# Sprint 1 — 백엔드 핵심 API

## 개요

| 항목 | 내용 |
|------|------|
| 스프린트 번호 | Sprint 1 |
| 브랜치 | `sprint1` |
| 기간 | 2026-03-13 ~ 2026-03-27 (2주) |
| 상태 | 🔄 진행 중 |
| 대상 브랜치 (PR) | `develop` |

## 스프린트 목표

.NET 9 Web API 기반의 핵심 백엔드를 완성한다.
Supabase PostgreSQL에 EF Core 마이그레이션을 적용하고, 거래·카테고리·결제수단·포인트 예산·반복 지출·통계·검색 등 프론트엔드가 소비할 모든 REST API를 구현한다.
스프린트 종료 시점에 Swagger(또는 .http 파일)로 모든 엔드포인트를 수동 검증 가능한 상태여야 한다.

---

## 구현 범위

### 포함
- .NET 솔루션 및 Vue3 프로젝트 초기 구조 셋업 (T1)
- EF Core 엔티티 5종 + 마이그레이션 + 시드 데이터 (T2)
- 거래 CRUD API (T3)
- 카테고리 CRUD API (T4)
- 결제수단 CRUD API (T5)
- 포인트 예산 API + 잔액 차감 로직 (T6)
- 반복 지출 API + 자동 반영 로직 (T7)
- 월별 요약 API — 커스텀 월 시작일 지원 (T8)
- 통계 API — 카테고리별 집계 + 월별 추이 (T9)
- 검색·필터 API (T10)

### 제외 (Sprint 2 이후)
- Vue3 UI 구현 전체 (T11~T19)
- 사용자 인증 (JWT) — Backlog 항목
- 예산 초과 알림, CSV 내보내기 — Backlog 항목

---

## 태스크 상세

### T1 — 프로젝트 구조 설정
**우선순위**: 최상 | **예상 소요**: 0.5일

- ✅ .NET 9 Web API 솔루션 생성 (`backend/BudgetTracker.Api/`)
- ✅ Vue3 + Vite + TypeScript 프론트 프로젝트 생성 (`frontend/`)
- ✅ `.env.example` 작성 (DB 연결 문자열, CORS 오리진 등)
- ✅ `appsettings.json` / `appsettings.Development.json` 구성 (ConnectionStrings, CORS)
- ✅ Swagger / OpenAPI 활성화 확인
- ✅ 프로젝트 루트 `README.md` 실행 방법 업데이트

---

### T2 — DB 모델 및 마이그레이션
**우선순위**: 최상 | **예상 소요**: 1일

- ✅ NuGet 패키지 추가: `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.Design`
- ✅ `Transaction` 엔티티 구현
- ✅ `Category` 엔티티 구현
- ✅ `PaymentMethod` 엔티티 구현
- ✅ `PointBudget` 엔티티 구현
- ✅ `RecurringTransaction` 엔티티 구현
- ✅ `UserSettings` 엔티티 구현 (MonthStartDay)
- ✅ `BudgetTrackerDbContext` 구현 (DbSet, 관계 설정, Fluent API)
- ✅ EF Core 초기 마이그레이션 생성 (`InitialCreate`)
- ⬜ Supabase 마이그레이션 적용 (`dotnet ef database update`)
- ✅ 기본 시드 데이터 작성 (카테고리 기본값, 결제수단 기본값, UserSettings 기본값)

---

### T3 — 거래 CRUD API
**우선순위**: 상 | **예상 소요**: 1일

- ✅ `TransactionsController` 구현
- ✅ `GET /api/transactions` — 목록 조회 (기본: 현재 월 기준)
- ✅ `POST /api/transactions` — 거래 생성 (합산포함여부, 결제수단 포함)
- ✅ `GET /api/transactions/{id}` — 거래 상세 조회
- ✅ `PUT /api/transactions/{id}` — 거래 수정
- ✅ `DELETE /api/transactions/{id}` — 거래 삭제
- ✅ 포인트 결제수단 선택 시 `PointBudget.RemainingAmount` 자동 차감/복구 처리
- ✅ DTO(Request/Response) 클래스 정의 및 유효성 검사 (`DataAnnotations`)

---

### T4 — 카테고리 API
**우선순위**: 상 | **예상 소요**: 0.5일

- ✅ `CategoriesController` 구현
- ✅ `GET /api/categories` — 전체 목록 (수입/지출 구분 쿼리 파라미터 지원)
- ✅ `POST /api/categories` — 카테고리 생성
- ✅ `PUT /api/categories/{id}` — 카테고리 수정
- ✅ `DELETE /api/categories/{id}` — 카테고리 삭제 (연결된 거래 존재 시 오류 반환)

---

### T5 — 결제수단 API
**우선순위**: 상 | **예상 소요**: 0.5일

- ✅ `PaymentMethodsController` 구현
- ✅ `GET /api/payment-methods` — 전체 목록 (Type 필터 지원: Cash/Card/Point)
- ✅ `POST /api/payment-methods` — 결제수단 생성
- ✅ `PUT /api/payment-methods/{id}` — 결제수단 수정
- ✅ `DELETE /api/payment-methods/{id}` — 결제수단 삭제 (연결된 거래 존재 시 오류 반환)

---

### T6 — 포인트 예산 API
**우선순위**: 중 | **예상 소요**: 0.5일

- ✅ `PointBudgetsController` 구현
- ✅ `GET /api/point-budgets` — 포인트 예산 목록 (잔액 포함)
- ✅ `POST /api/point-budgets` — 포인트 예산 생성 (TotalAmount 입력 시 RemainingAmount 동일하게 초기화)
- ✅ 잔액 차감 로직은 T3(거래 생성/수정/삭제) 내에서 처리 (별도 엔드포인트 불필요)

---

### T7 — 반복 지출 API
**우선순위**: 중 | **예상 소요**: 1일

- ✅ `RecurringTransactionsController` 구현
- ✅ `GET /api/recurring-transactions` — 반복 지출 목록
- ✅ `POST /api/recurring-transactions` — 반복 지출 등록 (Fixed/Installment 구분)
- ✅ `DELETE /api/recurring-transactions/{id}` — 반복 지출 삭제
- ✅ 자동 반영 로직: 월별 요약 API 호출 시 on-demand 생성 방식

---

### T8 — 월별 요약 API
**우선순위**: 상 | **예상 소요**: 0.5일

- ✅ `SummaryController` 구현
- ✅ `GET /api/summary/monthly?year=&month=` — 월별 수입/지출/잔액 집계
- ✅ `GET /api/settings` — 사용자 설정 조회
- ✅ `PUT /api/settings` — 사용자 설정 수정 (MonthStartDay 등)
- ✅ 커스텀 월 시작일 기준 날짜 범위 계산 로직 구현 (`DateRangeHelper`)

---

### T9 — 통계 API
**우선순위**: 중 | **예상 소요**: 0.5일

- ✅ `GET /api/summary/category?year=&month=` — 카테고리별 금액 집계 (파이차트용)
- ✅ `GET /api/summary/trend?months=6` — 최근 N개월 월별 수입/지출 추이 (막대그래프용)
- ✅ 전월 대비 지출 비교 수치 포함 (`monthOverMonthChange`)

---

### T10 — 검색·필터 API
**우선순위**: 중 | **예상 소요**: 0.5일

- ✅ `GET /api/transactions` 에 쿼리 파라미터 필터 추가 (category, paymentMethod, from, to, keyword, minAmount, maxAmount)
- ✅ 필터 조합 쿼리 최적화 (IQueryable 체이닝 방식)
- ✅ keyword 검색: `EF.Functions.ILike` (PostgreSQL 대소문자 무시)

---

## 기술적 의존성 및 리스크

| 리스크 | 가능성 | 영향 | 대응 방안 |
|--------|--------|------|-----------|
| Supabase 연결 지연/설정 오류 | 중 | 상 | 로컬 PostgreSQL Docker로 대체 개발 후 Supabase 전환 |
| EF Core 마이그레이션 충돌 | 저 | 중 | 개발 중 스키마 변경 시 마이그레이션 재생성 절차 문서화 |
| 커스텀 월 시작일 날짜 계산 버그 | 중 | 중 | 경계값(31일, 28일 등) 케이스 수동 검증 필수 |
| 반복 지출 자동 반영 중복 생성 | 중 | 상 | RecurringTransactionId + 년월 조합 멱등성 체크 로직 적용 |
| 로컬 Node.js 버전 부족 | 고 | 저 | CI (Node 20)에서 프론트 빌드 검증, 로컬은 Node 20으로 업그레이드 권장 |

---

## 완료 기준 (Definition of Done)

- ✅ T1~T10 모든 태스크의 체크리스트 항목이 완료 상태
- ✅ `dotnet build` 에러 없음 (경고 0개)
- ⬜ EF Core 마이그레이션이 Supabase에 성공적으로 적용됨
- ⬜ Swagger UI에서 모든 엔드포인트 목록이 노출됨
- ⬜ 다음 시나리오 수동 검증 통과:
  1. 카테고리 생성 → 결제수단 생성 → 거래 생성 → 거래 수정 → 거래 삭제
  2. 포인트 예산 생성 → 포인트 결제수단으로 거래 생성 → 잔액 차감 확인
  3. 반복 지출 등록 → 월별 요약 API 호출 → 거래 자동 생성 확인
  4. 커스텀 월 시작일(25일) 설정 → 월별 요약 날짜 범위 정확성 확인
  5. 검색 파라미터 조합 필터링 정상 동작 확인
- ⬜ `sprint1` 브랜치에서 `develop` 브랜치로 PR 생성 완료
- ⬜ PR 설명에 수동 검증 결과 스크린샷 첨부 (또는 .http 파일 응답 결과)

---

## 예상 산출물

| 산출물 | 설명 | 상태 |
|--------|------|------|
| `backend/BudgetTracker.Api/` | .NET 9 Web API 프로젝트 전체 | ✅ 완료 |
| `frontend/` | Vue3 + Vite + TS 초기 프로젝트 (빌드 가능 상태) | ✅ 완료 (Node 20 필요) |
| EF Core 마이그레이션 파일 | Supabase에 적용 예정 | ✅ 생성 완료 |
| Swagger 문서 | 전체 REST API 명세 자동 생성 | ✅ 설정 완료 |
| `.env.example` | 환경변수 템플릿 | ✅ 완료 |
| `docs/sprint/sprint1.md` | 본 문서 (계획 + 완료 체크리스트) | 🔄 진행 중 |

---

## 작업 순서 (권장)

```
T1 (구조 설정) ✅
  └→ T2 (DB 모델·마이그레이션) ✅
       └→ T4 (카테고리 API) ✅
       └→ T5 (결제수단 API) ✅
       └→ T6 (포인트 예산 API) ✅
       └→ T3 (거래 CRUD API) ✅
            └→ T7 (반복 지출 API) ✅
            └→ T8 (월별 요약 API) ✅
                 └→ T9 (통계 API) ✅
            └→ T10 (검색·필터 API) ✅
```

---

## 참고 문서

- `ROADMAP.md` — 전체 요구사항 및 DB 스키마 정의
- `docs/dev-process.md` — 개발 프로세스 및 검증 원칙
- `docs/ci-policy.md` — CI/CD 정책
- `CLAUDE.md` — 기술 스택 및 브랜치 전략
