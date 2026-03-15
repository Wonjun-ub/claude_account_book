# Sprint 3 — 월 시작일 버그 수정 & 거래 유형 3분류 탭 UI (목업)

## 개요

| 항목 | 내용 |
|------|------|
| 스프린트 번호 | Sprint 3 |
| 유형 | 버그 수정 + 목업 |
| 브랜치 | `sprint3` |
| 기간 | 2026-03-15 ~ 2026-03-28 (2주) |
| 상태 | ✅ 완료 (2026-03-15) |
| 대상 브랜치 (PR) | `develop` |
| DB/백엔드 변경 | T20만 백엔드 1파일 수정 (DB 스키마 변경 없음) |

## 스프린트 목표

두 가지 작업을 수행한다.

1. **버그 수정 (T20)**: 거래 목록 API가 `monthStartDay`를 무시하고 표준 달력 기준으로 필터링하는 문제를 수정한다. 백엔드 1파일 수정으로 완료 가능한 범위이므로 목업 단계 없이 바로 수정한다.

2. **목업 UI (T21)**: 거래 등록 모달에서 일반/반복/할부 3가지 유형을 탭으로 명확히 선택할 수 있도록 UI를 개편한다. 이 스프린트에서 **할부 탭은 mock 데이터로만 동작**하며, 실제 `installment-transactions` API 연동은 Sprint 5에서 수행한다.

스프린트 종료 시점에 DB 스키마 변경 없이 배포 가능한 상태여야 한다.

---

## 구현 범위

### 포함
- 거래 목록 API `monthStartDay` 미적용 버그 수정 (T20) — 백엔드 수정, DB 변경 없음
- 거래 입력 모달 — 일반/반복/할부 유형 선택 탭 UI 개편 (T21) — 프론트엔드 목업

### 제외 (이후 스프린트)
- 할부 `installment-transactions` API 실제 연동 — Sprint 5에서 구현
- 할부 DB 마이그레이션 (`InstallmentTransactions` 테이블) — Sprint 5에서 수행
- 카드 결제 현황 탭 — Sprint 4(목업) → Sprint 6(구현) 순서로 진행
- 사용자 인증 (JWT) — Backlog

---

## 목업 전략 (T21)

### 왜 목업인가

할부 기능은 DB 스키마 변경(`InstallmentTransactions` 테이블 신규)과 복잡한 카드사 방식 계산 로직이 필요하다. 이 작업 전에 UI 흐름과 입력 항목이 사용자 입장에서 직관적인지 먼저 확인해야 한다.

이 스프린트에서는 할부 탭의 **입력 폼 UI와 저장 버튼 동작만** 구현한다. 저장 시 실제 API 대신 mock 처리(로컬 상태 업데이트만)로 흐름을 시뮬레이션한다.

### 목업 데이터 파일

```
frontend/src/mocks/
└── installment.mock.ts   # 할부 mock 응답 데이터 및 저장 시뮬레이션 함수
```

```typescript
// frontend/src/mocks/installment.mock.ts
// Sprint 3 목업용 — Sprint 5에서 실제 API로 교체

export interface MockInstallmentInput {
  totalAmount: number      // 총 금액
  totalInstallments: number // 총 개월수
  startDate: string        // 시작일
  categoryId: number
  paymentMethodId: number
  memo?: string
}

// 카드사 방식 계산 미리보기 (UI 확인용)
export function calcMockInstallment(total: number, months: number) {
  const monthly = Math.floor(total / months)
  const firstExtra = total - monthly * months
  return {
    monthlyAmount: monthly,
    firstMonthAmount: monthly + firstExtra,
    totalInstallments: months,
  }
}

// mock 저장 — 실제 API 없이 콘솔 출력 + Promise 반환
export async function mockCreateInstallment(input: MockInstallmentInput) {
  console.log('[Mock] 할부 등록 (Sprint 5에서 실제 API로 교체):', input)
  return { id: -1, ...input }
}
```

---

## 사전 파악 사항

### 현재 버그 동작 방식 (T20)

`GET /api/transactions?year=&month=` 호출 시 백엔드가 `UserSettings.MonthStartDay`를 로드하지 않고 표준 달력(1일~말일)로 날짜 범위를 계산한다.

반면 `GET /api/summary/monthly?year=&month=`는 `DateRangeHelper.GetMonthRange()`를 사용하여 커스텀 시작일 기준 날짜 범위를 올바르게 계산한다.

**영향**: `monthStartDay=25`, 3월 조회 시:
- 요약: `2/25~3/24` 기준으로 집계 (올바름)
- 거래 목록: `3/1~3/31` 기준 반환 → `2/25~2/28` 구간 거래가 목록에 미표시 (버그)

### 현재 거래 유형 UI 현황 (T21)

`TransactionModal.vue`에서 반복 설정이 `recurring.enabled` 체크박스로 접혀 있어:
- 사용자가 할부와 반복을 구분하기 어려움
- 3가지 유형(일반/반복/할부)을 탭으로 명확히 선택하도록 개편 필요

---

## 태스크 상세

### T20 — 거래 목록 API monthStartDay 버그 수정

**유형**: 백엔드 버그 수정 | **우선순위**: 최상 | **예상 소요**: 0.5일

**수정 대상 파일**
- `backend/BudgetTracker.Api/Services/TransactionService.cs` (또는 Repository — 기존 아키텍처 확인 후 적절한 레이어 수정)

**수정 방향**

```csharp
// 수정 전 (추정): 표준 달력 기준
var from = new DateTime(year, month, 1);
var to = from.AddMonths(1).AddDays(-1);

// 수정 후: DateRangeHelper 활용
var settings = await _settingsRepository.GetAsync();
var (from, to) = DateRangeHelper.GetMonthRange(year, month, settings.MonthStartDay);
```

**영향 범위 파악 필수**
- `GET /api/transactions` 쿼리 파라미터 처리 경로 전체
- `from/to` 직접 지정 시에는 기존 동작 유지 (monthStartDay 미적용)
- 반복 거래 on-demand 자동 반영 로직에서 날짜 범위를 별도로 계산하는 경우 동일 패턴 적용 필요 여부 확인

**검증 시나리오**
1. `monthStartDay=25` 설정 후 2월 25일~3월 24일 사이 거래 여러 건 입력
2. `GET /api/transactions?year=2026&month=3` 호출
3. 반환 결과에 2월 25일~28일 거래 포함 여부 확인
4. `GET /api/summary/monthly?year=2026&month=3` 집계 금액과 거래 목록 합계 일치 확인

---

### T21 — 거래 입력 모달 유형 선택 탭 UI (목업)

**유형**: 프론트엔드 목업 | **우선순위**: 상 | **예상 소요**: 1일

**수정 대상 파일**
- `frontend/src/components/TransactionModal.vue`
- `frontend/src/mocks/installment.mock.ts` (신규 생성)

**현재 구조 → 목표 구조**

```
[현재]
금액/날짜/카테고리/결제수단 입력
└── "반복 설정" 체크박스 (collapsed)
    └── 반복 유형: Fixed | Installment 선택
        └── (할부 시) 총 횟수 입력

[목표]
거래 유형 선택 탭: [일반] [반복] [할부]
금액/날짜/카테고리/결제수단 입력
└── (반복 탭 선택 시) 반복 일자 입력
└── (할부 탭 선택 시) 총 개월수 입력 + 월 할부금 미리보기
```

**세부 작업**

- ⬜ `transactionType` ref 추가: `'OneTime' | 'Fixed' | 'Installment'` (기본값: `'OneTime'`)
- ⬜ 모달 상단에 유형 선택 탭 UI 추가 (3개 버튼, 활성 탭 강조)
- ⬜ 기존 `recurring.enabled` 체크박스 제거, `transactionType` 기반으로 조건부 섹션 표시
- ⬜ 반복(Fixed) 탭: 반복 일자(DayOfMonth) 입력 필드 표시
- ⬜ 할부(Installment) 탭: 총 개월수 입력 + `calcMockInstallment()`로 월 할부금 미리보기 표시
  - 예: "총 100,000원 / 3개월 → 1회차 33,334원, 이후 33,333원"
- ⬜ 수정 모드(`isEdit=true`)에서는 유형 선택 탭 비활성화 (기존 유형 표시만)
- ⬜ 저장 로직:
  - `OneTime`: 기존 `transactionsApi.create()` 호출 (변경 없음)
  - `Fixed`: 기존 `transactionsApi.create()` + `recurringApi.create({ type: 'Fixed', ... })`
  - `Installment` (목업): `mockCreateInstallment()` 호출 → 실제 거래 생성 없이 토스트 메시지만 표시
    - 토스트: "할부 등록 기능은 준비 중입니다 (Sprint 5에서 구현 예정)"

**영향 범위**
- 신규 등록 / 수정 모드 / 반복(Fixed) 저장 세 가지 케이스 모두 동작 확인 필수
- 할부 탭은 mock이므로 실제 데이터 변경 없음 — 기존 데이터에 영향 없음

**검증 시나리오**
1. 일반 탭 → 거래 등록 → 거래 목록에 단건 표시, 반복 거래 미생성 확인
2. 반복(Fixed) 탭 → 거래 등록 → 거래 목록 표시 + `GET /api/recurring-transactions`에 Fixed 항목 추가 확인
3. 할부(Installment) 탭 → 총 개월수 입력 → 월 할부금 미리보기 표시 확인
4. 할부 탭 → 저장 버튼 → "준비 중" 토스트 표시, 실제 거래 미생성 확인
5. 기존 거래 수정 → 유형 탭 비활성화(표시만), 금액/메모 등 수정 정상 동작 확인

---

## 기술적 의존성 및 리스크

| 리스크 | 가능성 | 영향 | 대응 방안 |
|--------|--------|------|-----------|
| T20 수정 시 기존 `from/to` 직접 지정 파라미터와 충돌 | 낮음 | 중간 | `year/month` 파라미터 사용 시에만 DateRangeHelper 적용, `from/to` 직접 지정 시 기존 로직 유지 |
| T21 UI 변경으로 기존 수정 모드 동작 깨짐 | 보통 | 높음 | 수정 모드에서는 유형 탭 비활성화 처리, 저장 경로는 기존 PUT API만 호출 |
| 할부 탭 mock 처리가 사용자에게 혼란을 줌 | 낮음 | 낮음 | "준비 중" 토스트 메시지로 명확하게 안내 |

---

## 완료 기준 (Definition of Done)

### 기능 완료 기준

- ✅ T20: `monthStartDay=25` 설정 시 거래 목록과 요약 API가 동일한 날짜 범위(예: 2/25~3/24) 기준으로 동작
- ✅ T21: 모달에서 일반/반복/할부 탭 전환이 정상 동작
- ✅ T21: 일반/반복(Fixed) 유형 저장이 올바르게 처리됨
- ✅ T21: 수정 모드에서 유형 탭이 비활성화(표시만)되고 기존 수정 기능이 정상 동작
- ✅ T21: 할부 탭 선택 시 월 할부금 미리보기 표시, 저장 시 "준비 중" 토스트 표시

### 코드 품질 기준

- ✅ `npm run build` 성공 (TypeScript 타입 에러 0건)
- ✅ `dotnet build` 컴파일 에러 0건 (프로세스 잠금 경고만 발생, 코드 품질 기준 통과)

### 배포 기준

- ✅ DB 스키마 변경 없음 (마이그레이션 불필요)
- ✅ `sprint3` → `develop` PR 생성 완료

---

## 예상 산출물

| 산출물 | 설명 |
|--------|------|
| `backend/.../Services/TransactionService.cs` (또는 Repository) | monthStartDay 기반 날짜 범위 적용 |
| `frontend/src/components/TransactionModal.vue` | 유형 선택 탭(일반/반복/할부) 추가, 기존 체크박스 방식 제거 |
| `frontend/src/mocks/installment.mock.ts` | 할부 mock 계산 + 저장 시뮬레이션 함수 |
| `docs/sprint/sprint3.md` | 본 문서 |

---

## 다음 스프린트 연결

Sprint 3 완료 후 Sprint 4에서 할부 + 카드 결제 현황의 목업을 동시에 확인한다.

```
Sprint 3 완료 (목업 확인)
  └─ Sprint 4: 할부 + 카드 결제 현황 목업 (요구사항 확정)
       └─ Sprint 5: 할부 실제 구현 (DB + 백엔드 + 프론트 연동)
       └─ Sprint 6: 카드 결제 현황 실제 구현 (DB + 백엔드 + 프론트 연동)
```

---

## 작업 순서 (권장)

```
T20 (백엔드 버그 수정 — 독립적, 먼저 처리 권장)
T21 (프론트엔드 UI 개편 — T20과 병행 가능)
```

T20은 백엔드 단독 수정으로 프론트엔드 변경 없이 즉시 배포 가능한 단위.
T21은 프론트엔드 단독 수정으로 백엔드 API 추가 없이 구현 가능.

---

## 스프린트 회고

**완료일**: 2026-03-15

### 달성 사항

- T20: `TransactionService.GetAllAsync()`에 `ISettingsRepository` 주입 추가. `year/month` 파라미터 사용 시 `DateRangeHelper.GetMonthRange()`로 변환하여 monthStartDay가 거래 목록에도 올바르게 적용됨. `from/to` 직접 지정 시에는 기존 로직 유지.
- T21: `TransactionModal.vue` 완전 개편. 기존 `recurring.enabled` 체크박스 방식을 제거하고 일반/반복/할부 3탭 UI로 교체. 수정 모드에서는 탭 read-only 표시. 할부 탭 저장 시 "준비 중" 토스트 안내. `installment.mock.ts` 신규 생성으로 목업 계산 함수 분리.

### 핵심 결정 사항

- 할부 탭은 `mockCreateInstallment()` 호출 없이 `showAlert()` 직접 호출로 단순화 — Sprint 5에서 실제 API 연동 시 교체 예정
- 수정 모드에서 `'Installment'` 유형 표시는 현재 미구현 (DB에 할부 원부 미존재하므로 영향 없음)
