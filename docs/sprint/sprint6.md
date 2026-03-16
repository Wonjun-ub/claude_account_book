# Sprint 6 — 카드 결제 현황 (3단계 프로세스)

## 개요

| 항목 | 내용 |
|------|------|
| 스프린트 번호 | Sprint 6 |
| 유형 | 3단계: 목업 → 승인 → 구현 |
| 브랜치 | `sprint6` (Step 3 시작 시 생성) |
| 기간 | 2026-03-16 시작 |
| 상태 | 🔄 Step 1 완료 / Step 2 승인 대기 중 |
| 대상 브랜치 (PR) | `develop` |
| DB/백엔드 변경 | 있음 — Step 3에서 진행 (`PaymentMethods`에 `BillingCutoffDay`, `PaymentDueDay` 컬럼 추가) |
| 선행 조건 | Sprint 5 완료 ✅ |

---

## 스프린트 목표

카드별 정산일/결제일 기준으로 **2슬롯(현재 청구 + 다음 청구)** 을 자동 계산하여 납부 예정 금액을 한눈에 파악할 수 있는 카드 탭을 제공한다.

---

## Step 1 — 목업 ✅ 완료 (2026-03-16)

### 구현 완료 파일

| 파일 | 설명 |
|------|------|
| `frontend/src/mocks/cardBilling.mock.ts` | 케이스 A(신한카드, 정산일=15/결제일=25) + 케이스 B(국민카드, 정산일=25/결제일=10) mock 데이터 |
| `frontend/src/components/CardBillingSlot.vue` | 슬롯 1개 UI: 결제월 레이블, 청구 기간, 결제 예정일, D-day 배지(D-day→빨강/D-7이내→주황/이외→회색), 이용금액, 이용 중/확정 배지 |
| `frontend/src/components/CardBillingCard.vue` | 카드 헤더(카드명, 정산일/결제일) + 2슬롯 나란히 + 슬롯 클릭 시 드릴다운 패널(카테고리 소계 칩 + 거래 목록) |
| `frontend/src/views/CardBillingView.vue` | 카드 목록 페이지, 카드 없음 안내 메시지 포함 |
| `frontend/src/router/index.ts` | `/card-billing` 라우트 추가 |
| `frontend/src/App.vue` | 하단 탭바 4탭(가계부/통계/카드/설정) 구성 |

### Step 1 구현 상세

**mock 데이터 구조:**
- `MockCard`: id, name, billingCutoffDay, paymentDueDay, slots(2개)
- `MockCardSlot`: slotIndex, label, periodFrom, periodTo, paymentDueDate, totalAmount, transactionCount, isConfirmed
- `calcDDay(paymentDueDate)`: D-N / D-day / D+N 문자열 반환

**드릴다운 패널:**
- 슬롯 클릭 → 슬라이드다운 패널 펼침
- 카테고리별 소계 칩 (금액 내림차순)
- 거래 목록 (날짜/카테고리/금액)

---

## Step 2 — UI/UX 확정 ⬜ 승인 대기

> Step 1 목업을 로컬에서 `/card-billing` 접속하여 확인 후 승인해주세요.

### 확인 항목

| # | 항목 | 확인 포인트 |
|---|------|------------|
| 1 | 4탭 하단 탭바 레이아웃 | iPhone SE(375px) 기준 4탭이 균등하게 들어오는가? 텍스트 잘림 없는가? |
| 2 | 2슬롯 가독성 | 나란히 배치 시 각 슬롯 정보(기간/금액/D-day)가 충분히 읽히는가? |
| 3 | D-day 색상 피드백 | D-day(빨강) / D-7 이내(주황) / 이외(회색) 구분이 직관적인가? |
| 4 | 드릴다운 방식 | 슬라이드다운 패널 방식이 자연스러운가? (별도 화면 전환 vs 인라인 펼침) |
| 5 | 카테고리 소계 칩 | 드릴다운 헤더의 카테고리 소계 칩 배치가 적절한가? |

### 승인 방법

> "진행해" 또는 "Step 2 완료, Step 3 진행해"라고 말씀해주세요.
> 수정 요청이 있으면 목업 파일을 수정 후 재확인합니다.

---

## Step 3 — 실제 구현 ⬜ 대기 중 (Step 2 승인 후 시작)

> **Step 2 승인 전에는 DB/백엔드 코드를 절대 건드리지 않습니다.**

### DB 스키마 변경

**변경: `PaymentMethods` 테이블 (Card 타입 전용 컬럼 추가)**

| 추가 필드 | 타입 | 설명 |
|-----------|------|------|
| BillingCutoffDay | int? | 정산일 (1~28, Card 타입에만 유효) |
| PaymentDueDay | int? | 결제일 (1~28, Card 타입에만 유효) |

> Card 타입이 아닌 결제수단은 두 필드 모두 `null` 유지.

### 카드 슬롯 계산 로직 (핵심)

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

### 태스크

#### T34 — DB 모델 및 마이그레이션

**우선순위**: 최상 | **예상 소요**: 0.5일

- ⬜ `PaymentMethod` 엔티티에 `BillingCutoffDay`, `PaymentDueDay` Nullable 컬럼 추가
- ⬜ EF Core 마이그레이션 생성 (`AddCardBillingColumns`)
- ⬜ 로컬 PostgreSQL에서 마이그레이션 적용 검증 후 Supabase 적용

---

#### T35 — 카드 청구 설정 API

**우선순위**: 최상 | **예상 소요**: 0.5일

기존 `PUT /api/payment-methods/{id}` 엔드포인트 확장.

- ⬜ `PaymentMethodUpdateRequest` DTO에 `BillingCutoffDay`, `PaymentDueDay` 추가
- ⬜ Card 타입이 아닌 경우 두 필드 무시 (유효성 검사)
- ⬜ `GET /api/payment-methods` 응답에 두 필드 포함

---

#### T36 — 카드 결제 현황 백엔드 API

**우선순위**: 최상 | **예상 소요**: 1.5일

**신규 엔드포인트**

| 메서드 | 경로 | 설명 |
|--------|------|------|
| GET | /api/card-billing/summary | 설정된 모든 카드의 2슬롯 요약 |
| GET | /api/card-billing/{paymentMethodId}/transactions | 특정 슬롯의 거래 목록 (드릴다운) |

**`GET /api/card-billing/summary` 응답 구조** (Step 1 mock 구조와 동일)

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
- ⬜ 확정 여부: `periodTo < today` → `isConfirmed = true`
- ⬜ 드릴다운 API: 청구 기간 내 해당 카드 거래 목록 반환 (카테고리별 소계 포함)
- ⬜ PRD §4.7 케이스 A/B 예시 테이블 기준으로 단위 테스트 작성 후 구현

---

#### T37 — 프론트엔드 mock → 실제 API 교체

**우선순위**: 상 | **예상 소요**: 1일

**수정 대상 파일**
- Modify: `frontend/src/views/CardBillingView.vue` (mock → API)
- Modify: `frontend/src/views/SettingsView.vue` (카드 청구 설정 mock → API)
- Create: `frontend/src/api/cardBilling.ts`

**세부 작업**

- ⬜ `cardBilling.ts` API 모듈 생성
  - `getSummary()` → `GET /api/card-billing/summary`
  - `getTransactions(paymentMethodId, from, to)` → `GET /api/card-billing/{id}/transactions`
- ⬜ `CardBillingView.vue` — `mockCards` → `cardBillingApi.getSummary()` 교체
- ⬜ 드릴다운 패널 — `mockSlotTransactions` → `cardBillingApi.getTransactions()` 교체
- ⬜ `SettingsView.vue` — 카드 청구 설정 저장 → `PUT /api/payment-methods/{id}` 연동
- ⬜ `frontend/src/mocks/cardBilling.mock.ts` — Step 3 완료 후 파일 삭제

---

## 기술적 의존성 및 리스크

| 리스크 | 가능성 | 영향 | 대응 방안 |
|--------|--------|------|-----------|
| 슬롯 계산 로직 케이스 A/B 경계 조건 오류 | 높음 | 높음 | PRD §4.7 예시 테이블(3/20, 3/25, 3/26 케이스)을 단위 테스트로 먼저 작성 후 구현 |
| 월말 날짜 처리 (31일, 2월 28일 등) | 보통 | 중간 | `BillingCutoffDay`, `PaymentDueDay`를 1~28로 제한, DateTime 계산 시 월말 보정 로직 추가 |

---

## 완료 기준 (Definition of Done — Step 3 기준)

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

## 예상 산출물 (Step 3)

| 산출물 | 설명 |
|--------|------|
| EF Core 마이그레이션 파일 | `AddCardBillingColumns` 마이그레이션 |
| `backend/.../Controllers/CardBillingController.cs` | 카드 청구 현황 API |
| `backend/.../Services/CardBillingService.cs` | 슬롯 계산 + 청구 집계 로직 |
| `frontend/src/api/cardBilling.ts` | 카드 결제 현황 API 모듈 |
| `frontend/src/views/CardBillingView.vue` | 카드 결제 현황 탭 (실제 API 연동) |
| `docs/sprint/sprint6.md` | 본 문서 |

---

## 작업 순서 (Step 3)

```
T34 (DB 마이그레이션 — 백엔드 작업의 전제)
  └─ T35 (카드 청구 설정 API — T34 완료 후 가능)
  └─ T36 (카드 결제 현황 API — T34 완료 후 가능, T35와 병행 가능)
       └─ T37 (프론트엔드 API 교체 — T35, T36 완료 후)
```

---

## 스프린트 회고 (완료 후 작성)

> 스프린트 완료 후 sprint-close 에이전트가 작성합니다.
