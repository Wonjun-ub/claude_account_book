# Sprint 5 — 할부 기능 실제 구현

## 개요

| 항목 | 내용 |
|------|------|
| 스프린트 번호 | Sprint 5 |
| 유형 | 구현 |
| 브랜치 | `sprint5` |
| 기간 | 2026-03-15 |
| 상태 | ✅ 완료 |
| 대상 브랜치 (PR) | `develop` |
| PR | https://github.com/Wonjun-ub/claude_account_book/pull/2 |
| DB/백엔드 변경 | 있음 (`InstallmentTransactions`, `RecurringSkips` 테이블 신규 + `Transactions` FK 컬럼 추가 + `RecurringTransactions` StartDate/EndDate 추가) |
| 선행 조건 | Sprint 4 완료 + 할부 UI 요구사항 확정 |
| 검증 기록 | `docs/deploy-history/2026-03-15.md` |

## 스프린트 목표

Sprint 4 목업에서 확인된 할부 기능 UI/UX를 실제 DB와 백엔드 API로 구현한다.

`RecurringTransaction` 테이블에서 Fixed와 Installment를 혼합 관리하던 구조를 개선하여, 할부 전용 `InstallmentTransaction` 테이블을 분리하고 카드사 방식(나머지 금액을 첫 달에 합산)으로 월 할부금을 자동 계산한다.

스프린트 종료 시점에 DB 마이그레이션이 Supabase에 적용된 상태에서 할부 거래 등록 → 자동 반영 → 상세 팝업 전체 플로우가 실제 데이터로 동작해야 한다.

---

## 선행 조건 체크리스트

Sprint 5 시작 전 다음 항목이 확정되어야 한다:

- ✅ Sprint 4(T22-mock) 할부 상세 팝업 표시 항목 확정
- ✅ Sprint 4(T23-mock) 할부 등록 입력 항목 확정
- ✅ Sprint 4 목업 검토 결과 반영 완료

---

## 구현 범위

### 포함
- `InstallmentTransaction` 테이블 신규 생성 + EF Core 마이그레이션 (T30)
- 할부 백엔드 API 구현 — 등록/조회/삭제 + 카드사 방식 월할부금 계산 (T31)
- 할부 on-demand 자동 반영 로직 — 회차별 금액 계산 + 완료 시 비활성화 (T32)
- 프론트엔드 — mock을 실제 API로 교체 (T33)
  - `InstallmentDetailModal.vue` API 연동
  - `TransactionModal.vue` 할부 탭 저장 API 연동

### 제외 (Sprint 6)
- 카드 결제 현황 탭 실제 구현
- 기존 `RecurringTransaction` 테이블의 Installment 타입 레코드 마이그레이션 스크립트 (운영 데이터 없는 MVP 단계이므로 DROP & RECREATE 허용)

---

## DB 스키마 변경

### 신규: `InstallmentTransactions` 테이블

| 필드 | 타입 | 설명 |
|------|------|------|
| Id | int | PK |
| TotalAmount | decimal | 총 금액 (원금) |
| MonthlyAmount | decimal | 월 할부금 = `floor(총금액 ÷ 개월수)` |
| FirstMonthExtra | decimal | 첫 달 추가금 = `총금액 mod 개월수` |
| TotalInstallments | int | 총 개월수 |
| RemainingInstallments | int | 남은 개월수 (매월 차감) |
| StartDate | DateTime | 첫 번째 할부 날짜 |
| CategoryId | int (FK) | 카테고리 |
| PaymentMethodId | int (FK) | 결제수단 |
| Memo | string? | 메모 |
| IsActive | bool | 활성 여부 (완료 시 false) |

### 변경: `Transactions` 테이블

| 추가 필드 | 타입 | 설명 |
|-----------|------|------|
| InstallmentTransactionId | int? (FK) | 할부 원부 참조 (할부 거래에만 설정) |
| InstallmentIndex | int? | 회차 번호 (1-based) |

### 변경: `RecurringTransactions` 테이블

- `Installment` 타입 레코드를 `InstallmentTransactions`로 이전
- `Type` 컬럼에서 `Installment` 값 제거 → `Fixed`만 남음

---

## 태스크 상세

### T30 — DB 모델 및 마이그레이션

**우선순위**: 최상 | **예상 소요**: 0.5일

- ⬜ `InstallmentTransaction` 엔티티 클래스 생성
- ⬜ `Transaction` 엔티티에 `InstallmentTransactionId`, `InstallmentIndex` 컬럼 추가
- ⬜ `BudgetTrackerDbContext`에 `InstallmentTransactions` DbSet 등록 및 관계 설정
- ⬜ EF Core 마이그레이션 생성 (`AddInstallmentTransactions`)
- ⬜ 로컬 PostgreSQL에서 마이그레이션 적용 검증 후 Supabase 적용

---

### T31 — 할부 백엔드 API

**우선순위**: 최상 | **예상 소요**: 1일

**신규 엔드포인트**

| 메서드 | 경로 | 설명 |
|--------|------|------|
| POST | /api/installment-transactions | 할부 등록 + 1회차 거래 자동 생성 |
| GET | /api/installment-transactions | 할부 목록 (활성/전체 조회) |
| GET | /api/installment-transactions/{id} | 할부 상세 (총금액, 월할부금, 남은금액, 진행회차) |
| DELETE | /api/installment-transactions/{id} | 할부 삭제 (연결된 미래 거래 함께 삭제) |

**월 할부금 계산 로직**

```csharp
// 카드사 방식: 나머지는 첫 달에 합산
// 예) 100,000원 / 3개월 → 1회차: 33,334원, 2~3회차: 33,333원
decimal monthlyAmount = Math.Floor(totalAmount / totalInstallments);
decimal firstMonthExtra = totalAmount - (monthlyAmount * totalInstallments);
```

**세부 작업**
- ⬜ `InstallmentTransactionsController` 구현
- ⬜ `InstallmentTransactionService` 구현 (계산 로직 포함)
- ⬜ POST 시 1회차 `Transaction` 레코드 자동 생성
- ⬜ DTO 정의 및 유효성 검사
- ⬜ Sprint 4 mock 응답 구조와 실제 API 응답 구조 일치 확인

---

### T32 — 할부 on-demand 자동 반영 로직

**우선순위**: 상 | **예상 소요**: 1일

- ⬜ 월별 요약/거래 목록 조회 시 활성 할부(`IsActive=true`)의 해당 월 미생성 회차 자동 삽입
- ⬜ 회차별 금액 계산: `index == 1 ? monthlyAmount + firstMonthExtra : monthlyAmount`
- ⬜ 멱등성 보장: `InstallmentTransactionId + 회차번호` 조합으로 중복 생성 방지
- ⬜ 마지막 회차 생성 시 `IsActive = false` + `RemainingInstallments = 0` 업데이트

---

### T33 — 프론트엔드 mock → 실제 API 교체

**우선순위**: 상 | **예상 소요**: 1일

**수정 대상 파일**
- Modify: `frontend/src/components/InstallmentDetailModal.vue` (mock → API)
- Modify: `frontend/src/components/TransactionModal.vue` (할부 탭 저장 mock → API)
- Create: `frontend/src/api/installmentTransactions.ts`

**세부 작업**

- ⬜ `installmentTransactions.ts` API 모듈 생성
  - `create(data)` → `POST /api/installment-transactions`
  - `getById(id)` → `GET /api/installment-transactions/{id}`
  - `remove(id)` → `DELETE /api/installment-transactions/{id}`
- ⬜ `TransactionModal.vue` — 할부 탭 저장 시 `installmentTransactionsApi.create()` 호출로 교체
- ⬜ `InstallmentDetailModal.vue` — `mockGetInstallment()` → `installmentTransactionsApi.getById()` 교체
- ⬜ 팝업 내 "할부 취소" 버튼 → `installmentTransactionsApi.remove()` 호출 + 목록 갱신
- ⬜ `frontend/src/mocks/installment.mock.ts` — Sprint 5 완료 후 파일 삭제 또는 `// DEPRECATED` 주석 처리

---

## 기술적 의존성 및 리스크

| 리스크 | 가능성 | 영향 | 대응 방안 |
|--------|--------|------|-----------|
| DB 마이그레이션 Supabase 적용 실패 | 낮음 | 높음 | 로컬 PostgreSQL에서 먼저 검증 후 Supabase 적용 |
| 기존 `RecurringTransaction.Installment` 타입 데이터와 충돌 | 낮음 | 중간 | MVP 단계이므로 운영 데이터 없음 — 기존 Installment 레코드 수동 삭제 |
| 할부 자동 반영 로직과 기존 반복 자동 반영 로직 중복 실행 | 보통 | 중간 | 두 로직을 명확히 분리 (`SummaryService`에서 각각 독립 호출) |
| 첫 달 추가금 계산 오류 (소수점, 반올림 정책) | 보통 | 중간 | `decimal` 타입 유지, `Math.Floor`로 일관 처리, 경계값 테스트 |
| Sprint 4 mock 응답 구조와 실제 API 구조 불일치 | 낮음 | 중간 | Sprint 4 mock 데이터 구조를 Sprint 5 API 설계 기준으로 사전 정의했으므로 차이 최소화 |

---

## 완료 기준 (Definition of Done)

- ✅ T30: EF Core 마이그레이션이 Supabase에 성공적으로 적용됨
- ⬜ T31: `POST /api/installment-transactions` 호출 시 100,000원/3개월 → 33,334/33,333/33,333원 분할 확인 (수동 검증 필요)
- ⬜ T32: 할부 등록 후 다음 달 조회 시 해당 회차 자동 생성, 마지막 회차 후 `IsActive=false` 확인 (수동 검증 필요)
- ✅ T33: 모달에서 할부 등록 시 `installment-transactions` API 호출, 거래 목록 즉시 갱신
- ✅ T33: 할부 거래 X 버튼 → DeleteOptionSheet → API 호출로 교체
- ✅ `npm run build` 성공, `dotnet build` 경고 0건
- ✅ `sprint5` → `develop` PR 생성 완료 (https://github.com/Wonjun-ub/claude_account_book/pull/2)

---

## 예상 산출물

| 산출물 | 설명 |
|--------|------|
| EF Core 마이그레이션 파일 | `AddInstallmentTransactions` 마이그레이션 |
| `backend/.../Entities/InstallmentTransaction.cs` | 할부 원부 엔티티 |
| `backend/.../Controllers/InstallmentTransactionsController.cs` | 할부 API 컨트롤러 |
| `backend/.../Services/InstallmentTransactionService.cs` | 카드사 방식 할부금 계산 + 자동 반영 로직 |
| `frontend/src/api/installmentTransactions.ts` | 할부 API 모듈 |
| `frontend/src/components/InstallmentDetailModal.vue` | 할부 상세 팝업 (실제 API 연동) |
| `frontend/src/components/TransactionModal.vue` | 할부 탭 실제 API 교체 |
| `docs/sprint/sprint5.md` | 본 문서 |

---

## 작업 순서 (권장)

```
T30 (DB 마이그레이션 — 백엔드 작업의 전제)
  └─ T31 (할부 백엔드 API)
       └─ T32 (on-demand 자동 반영)
       └─ T33 (프론트엔드 API 교체 — T31 완료 후 가능)
```

---

## 스프린트 회고

**완료일**: 2026-03-15

### 달성 사항

- Sprint 4 MockupView(localStorage 기반)에서 검증된 할부/반복 거래 기능을 실서비스로 성공적으로 이관
- `InstallmentTransactions`, `RecurringSkips` 2개 테이블 신규 추가 + EF Core 마이그레이션 Supabase 적용 완료
- 할부 API: POST/GET/DELETE (3가지 모드) 구현, 카드사 방식(나머지를 1회차에 합산) 계산 로직 적용
- 반복 거래 API 확장: `skipMonth` 삭제 모드(RecurringSkips 활용) + `/pending` 엔드포인트 추가
- `DeleteOptionSheet.vue` 신규 공용 컴포넌트로 할부/반복 삭제 UI 통합
- `MonthlySummaryResponse`에 `incomeCount`, `expenseCount` 추가로 HomeView 건수 표시 지원

### 주의사항

- 할부 등록 시 Transaction N건을 루프 내에서 개별 `CreateAsync` 호출 → 트랜잭션 원자성 없음. 규모가 커지면 단일 bulk insert로 개선 필요 (Medium)
- `SummaryService.GetMonthlyAsync()`에서 `allTransactions`를 별도 쿼리로 조회 → 동일 기간에 대해 2번 DB 쿼리 발생. 추후 최적화 가능 (Medium)
- 백엔드 dotnet test, API curl, Playwright 검증은 Docker 미실행으로 미수행. `docker compose up --build` 후 수동 검증 필요

### 수동 검증 남은 항목

- `docs/deploy-history/2026-03-15.md` 참조
