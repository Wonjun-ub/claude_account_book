# Sprint 6 — 카드 결제 현황 실제 구현

## 개요

| 항목 | 내용 |
|------|------|
| 스프린트 번호 | Sprint 6 |
| 유형 | 구현 |
| 브랜치 | `sprint6` |
| 기간 | 미정 (Sprint 5 완료 후 시작) |
| 상태 | ⬜ 예정 |
| 대상 브랜치 (PR) | `develop` |
| DB/백엔드 변경 | 있음 (`PaymentMethods`에 `BillingCutoffDay`, `PaymentDueDay` 컬럼 추가) |
| 선행 조건 | Sprint 4 완료 + 카드 결제 현황 UI 요구사항 확정 |

## 스프린트 목표

Sprint 4 목업에서 확인된 카드 결제 현황 탭의 UI/UX를 실제 DB와 백엔드 API로 구현한다.

카드별 정산일/결제일을 DB에 저장하고, 오늘 날짜 기준으로 2슬롯(현재 청구 + 다음 청구)을 자동 계산하여 납부 예정 금액을 한눈에 파악할 수 있는 신규 탭을 제공한다.

스프린트 종료 시점에 DB 마이그레이션 적용 + 카드 청구 현황 탭 전체 플로우(카드 설정 → 청구 슬롯 표시 → 드릴다운)가 실제 데이터로 동작해야 한다.

---

## 선행 조건 체크리스트

Sprint 6 시작 전 다음 항목이 확정되어야 한다:

- ⬜ Sprint 4(T24-mock) 카드 결제 현황 탭 레이아웃 확정
- ⬜ Sprint 4(T24-mock) 드릴다운 방식 확정 (슬라이드업 패널 vs 별도 화면)
- ⬜ Sprint 4(T24-mock) 하단 탭바 5번째 탭 레이아웃 확정
- ⬜ Sprint 4(T25-mock) 카드 청구 설정 UI 방식 확정

---

## 구현 범위

### 포함
- `PaymentMethod` 테이블에 카드 전용 컬럼 추가 + EF Core 마이그레이션 (T34)
- 카드 청구 설정 API (정산일/결제일 저장/조회) (T35)
- 카드 결제 현황 백엔드 API — 슬롯 계산 + 청구 기간 내 이용금액 집계 (T36)
- 프론트엔드 — mock을 실제 API로 교체 (T37)
  - `CardBillingView.vue` API 연동
  - `SettingsView.vue` 카드 청구 설정 API 연동

### 제외 (Backlog)
- 카드 결제일 알림 (브라우저 Push Notification)
- 청구 금액 예측 (미래 할부 포함 예상 청구액)

---

## DB 스키마 변경

### 변경: `PaymentMethods` 테이블 (Card 타입 전용 컬럼 추가)

| 추가 필드 | 타입 | 설명 |
|-----------|------|------|
| BillingCutoffDay | int? | 정산일 (1~28, Card 타입에만 유효) |
| PaymentDueDay | int? | 결제일 (1~28, Card 타입에만 유효) |

> Card 타입이 아닌 결제수단은 두 필드 모두 `null` 유지.

---

## 카드 슬롯 계산 로직 (핵심)

PRD §4.7 슬롯 전환 기준을 백엔드 서비스로 구현한다.

**케이스 A — 결제일 > 정산일 (같은 달 결제)**
- 예: 정산일=15, 결제일=25
- 청구 주기: 전월 16일 ~ 당월 15일 → 당월 25일 결제
- 오늘 ≤ 결제일: 1번 슬롯 = 이번 청구 사이클
- 오늘 > 결제일: 슬롯 한 달 앞으로 이동

**케이스 B — 결제일 < 정산일 (다음 달 결제)**
- 예: 정산일=25, 결제일=10
- 청구 주기: 전월 26일 ~ 당월 25일 → 익월 10일 결제
- 결제월 = 정산 마감월 + 1

**결제월 레이블 계산 규칙**
- 결제일 > 정산일: 결제월 = 정산 마감월
- 결제일 < 정산일: 결제월 = 정산 마감월 + 1
- 2번 슬롯은 항상 1번 다음 청구 주기

---

## 태스크 상세

### T34 — DB 모델 및 마이그레이션

**우선순위**: 최상 | **예상 소요**: 0.5일

- ⬜ `PaymentMethod` 엔티티에 `BillingCutoffDay`, `PaymentDueDay` Nullable 컬럼 추가
- ⬜ EF Core 마이그레이션 생성 (`AddCardBillingColumns`)
- ⬜ 로컬 PostgreSQL에서 마이그레이션 적용 검증 후 Supabase 적용

---

### T35 — 카드 청구 설정 API

**우선순위**: 최상 | **예상 소요**: 0.5일

기존 `PUT /api/payment-methods/{id}` 엔드포인트 확장.

- ⬜ `PaymentMethodUpdateRequest` DTO에 `BillingCutoffDay`, `PaymentDueDay` 추가
- ⬜ Card 타입이 아닌 경우 두 필드 무시 (유효성 검사)
- ⬜ `GET /api/payment-methods` 응답에 두 필드 포함

---

### T36 — 카드 결제 현황 백엔드 API

**우선순위**: 최상 | **예상 소요**: 1.5일

**신규 엔드포인트**

| 메서드 | 경로 | 설명 |
|--------|------|------|
| GET | /api/card-billing/summary | 설정된 모든 카드의 2슬롯 요약 |
| GET | /api/card-billing/{paymentMethodId}/transactions | 특정 슬롯의 거래 목록 (드릴다운) |

**`GET /api/card-billing/summary` 응답 구조** (Sprint 4 mock 구조와 동일)

```json
[
  {
    "paymentMethodId": 1,
    "cardName": "신한카드",
    "billingCutoffDay": 15,
    "paymentDueDay": 25,
    "slots": [
      {
        "slotIndex": 1,
        "label": "3월 청구금액",
        "periodFrom": "2026-02-15",
        "periodTo": "2026-03-14",
        "paymentDueDate": "2026-03-25",
        "dDayLabel": "D-5",
        "totalAmount": 150000,
        "transactionCount": 8,
        "isConfirmed": false
      }
    ]
  }
]
```

**세부 작업**
- ⬜ `CardBillingController` 구현
- ⬜ `CardBillingService` 구현 — 슬롯 계산 로직 (케이스 A/B 처리)
- ⬜ D-day 계산: `paymentDueDate - today` (음수면 "D+N", 0이면 "D-day", 양수면 "D-N")
- ⬜ 확정 여부: `periodTo < today` → `isConfirmed = true`
- ⬜ 드릴다운 API: 청구 기간 내 해당 카드 거래 목록 반환 (카테고리별 소계 포함)
- ⬜ PRD §4.7 케이스 A/B 예시 테이블 기준으로 단위 테스트 작성 후 구현

---

### T37 — 프론트엔드 mock → 실제 API 교체

**우선순위**: 상 | **예상 소요**: 1일

**수정 대상 파일**
- Modify: `frontend/src/views/CardBillingView.vue` (mock → API)
- Modify: `frontend/src/views/SettingsView.vue` (카드 청구 설정 mock → API)
- Create: `frontend/src/api/cardBilling.ts`

**세부 작업**

- ⬜ `cardBilling.ts` API 모듈 생성
  - `getSummary()` → `GET /api/card-billing/summary`
  - `getTransactions(paymentMethodId, from, to)` → `GET /api/card-billing/{id}/transactions`
- ⬜ `CardBillingView.vue` — `mockCardBillingSummary` → `cardBillingApi.getSummary()` 교체
- ⬜ 드릴다운 패널 — `mockCardBillingTransactions` → `cardBillingApi.getTransactions()` 교체
  - Sprint 4에서 확정된 드릴다운 방식(슬라이드업 vs 별도 화면)에 따라 구현
- ⬜ `SettingsView.vue` — 카드 청구 설정 저장 mock → `PUT /api/payment-methods/{id}` 교체
- ⬜ `frontend/src/mocks/cardBilling.mock.ts` — Sprint 6 완료 후 파일 삭제 또는 `// DEPRECATED` 주석 처리

---

## 기술적 의존성 및 리스크

| 리스크 | 가능성 | 영향 | 대응 방안 |
|--------|--------|------|-----------|
| 슬롯 계산 로직 케이스 A/B 경계 조건 오류 | 높음 | 높음 | PRD §4.7 예시 테이블(3/20, 3/25, 3/26 케이스)을 단위 테스트로 먼저 작성 후 구현 |
| 월말 날짜 처리 (31일, 2월 28일 등) | 보통 | 중간 | `BillingCutoffDay`, `PaymentDueDay`를 1~28로 제한, DateTime 계산 시 월말 보정 로직 추가 |
| 하단 탭바 5번째 탭 레이아웃 — Sprint 4에서 미확정 시 지연 | 보통 | 중간 | Sprint 4 완료 후 요구사항 확정 필수 |
| Sprint 4 mock 응답 구조와 실제 API 응답 구조 불일치 | 낮음 | 중간 | Sprint 4에서 mock 구조를 실제 API 기준으로 정의했으므로 차이 최소화 |

---

## 완료 기준 (Definition of Done)

- ⬜ T34: EF Core 마이그레이션이 Supabase에 성공적으로 적용됨
- ⬜ T35: 카드 결제수단에 정산일/결제일 저장/조회 정상 동작
- ⬜ T36: PRD §4.7 케이스 A(정산일=15, 결제일=25) 기준 슬롯 계산 결과가 예시 테이블과 일치
- ⬜ T36: PRD §4.7 케이스 B(정산일=25, 결제일=10) 기준 슬롯 계산 결과가 예시 테이블과 일치
- ⬜ T37: 카드 결제 현황 탭에서 실제 API 데이터로 2슬롯 카드 표시, D-day, 이용금액 정상 표시
- ⬜ T37: 슬롯 클릭 시 실제 청구 기간 거래 목록 드릴다운 정상 표시
- ⬜ T37: 설정 화면에서 카드 정산일/결제일 설정 후 현황 탭에 즉시 반영
- ⬜ `npm run build` 성공, `dotnet build` 경고 0건
- ⬜ `sprint6` → `develop` PR 생성 완료

---

## 예상 산출물

| 산출물 | 설명 |
|--------|------|
| EF Core 마이그레이션 파일 | `AddCardBillingColumns` 마이그레이션 |
| `backend/.../Controllers/CardBillingController.cs` | 카드 청구 현황 API |
| `backend/.../Services/CardBillingService.cs` | 슬롯 계산 + 청구 집계 로직 |
| `frontend/src/api/cardBilling.ts` | 카드 결제 현황 API 모듈 |
| `frontend/src/views/CardBillingView.vue` | 카드 결제 현황 탭 (실제 API 연동) |
| `docs/sprint/sprint6.md` | 본 문서 |

---

## 작업 순서 (권장)

```
T34 (DB 마이그레이션 — 백엔드 작업의 전제)
  └─ T35 (카드 청구 설정 API — T34 완료 후 가능)
  └─ T36 (카드 결제 현황 API — T34 완료 후 가능, T35와 병행 가능)
       └─ T37 (프론트엔드 API 교체 — T35, T36 완료 후)
```

---

## 스프린트 회고 (완료 후 작성)

> 스프린트 완료 후 sprint-close 에이전트가 작성합니다.
