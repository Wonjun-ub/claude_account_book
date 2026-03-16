# Sprint 6 — 카드 결제 현황 (3단계 프로세스)

## 개요

| 항목 | 내용 |
|------|------|
| 스프린트 번호 | Sprint 6 |
| 유형 | 목업 (Step 3 실서비스 구현은 Sprint 8로 이관) |
| 브랜치 | `sprint6` |
| 기간 | 2026-03-16 시작 |
| 상태 | ✅ Step 1 v3 완료 / Step 2 승인 완료 (2026-03-16) |
| 대상 브랜치 (PR) | `develop` |
| DB/백엔드 변경 | **없음** — 전략 변경: 통계/설정 목업 완료 후 Sprint 8에서 전체 실서비스 구현 |
| 선행 조건 | Sprint 5 완료 ✅ |

---

## 스프린트 목표

카드별 정산일/결제일 기준으로 **2슬롯(현재 청구 + 다음 청구)** 을 자동 계산하여 납부 예정 금액을 한눈에 파악할 수 있는 카드 탭을 제공한다.

---

## Step 1 — 목업 ✅ 완료 (2026-03-16, v3)

> **v1 → v2 변경**: 별도 탭(CardBillingView) 방식 → 메인 화면 '예정된 지출' 통합 위젯으로 전면 재설계.
> **v2 → v3 변경**: 홈 탭 UI/UX 전면 개선 — 다크모드, 저축 수단, 필터 아코디언, 반복 예정 배너 개선 등 대규모 UX 정제.

### 구현 완료 파일

| 파일 | 설명 |
|------|------|
| `frontend/src/mocks/upcoming.mock.ts` | 신한카드(id=1) + 국민카드(id=4), 정산일=15/결제일=25 하드코딩 더미 데이터 |
| `frontend/src/components/UpcomingWidget.vue` | 카드 결제 예정 위젯 — D-day 배지, 카드별 청구 내역 아코디언, 다크모드 |
| `frontend/src/views/MockupView.vue` | 홈 탭 전체 — 아래 상세 참조 |
| `frontend/src/App.vue` | 4탭 → 3탭(가계부/통계/설정) 원복 |
| `frontend/src/router/index.ts` | `/card-billing` 라우트 제거 |
| `frontend/tests/e2e/sprint6-mockup-features.spec.ts` | Playwright E2E 테스트 22개 (저축/필터/카드모달/서머리/반복배너/레이아웃) |

### Step 1 v3 구현 상세

**다크모드 전환 (MockupView + UpcomingWidget)**
- 전체 배경: `bg-gray-900` (헤더/하단탭) / `bg-gray-800` (카드/서피스) / `bg-gray-700` (인풋/칩)
- 텍스트: 일반 `text-gray-100`, 보조 `text-gray-400`
- 하단 탭 아이콘·텍스트: `text-white`

**저축(Savings) 거래 유형**
- 색상: `text-emerald-500` (금액 및 배지 전체 통일)
- 서머리 4열: 수입(파란) / 지출(빨간) / 저축(초록) / 잔액(흰색)
- 잔액 공식: `수입 - 지출 - 저축`
- 반복 예정 배너·카드 청구 배너에 저축 금액 합산 표시

**저축 수단 (MOCK_SAVINGS_METHODS)**
- 기업은행(id=101) / 카카오뱅크(id=102) / 현금(id=103)
- 폼: 지출 → "결제수단" 드롭다운, 저축 → "저축 수단" 드롭다운 (분리), 수입 → 수단 없음
- ID 공간 분리: 결제수단 1~99, 저축 수단 101~

**검색/필터 아코디언 (UpcomingWidget 아래, 거래 목록 위)**
- 토글 버튼으로 패널 열기/닫기
- 메모 키워드 검색 (실시간 필터)
- 유형 칩: 전체 / 수입 / 지출 / 저축
- 카테고리 칩: 유형 선택 시 해당 유형 카테고리 표시
- 월 이동 시 필터 자동 초기화

**일별 서머리**
- 날짜 그룹 헤더에 `+수입 / -지출 / -저축` 형식
- 각 금액은 수입/지출/저축 색상으로 구분

**카드 결제 모달 카테고리 필터**
- 카드 청구 모달 상단에 해당 청구 기간의 카테고리 칩 자동 생성
- 칩 선택 시 해당 카테고리 거래만 표시
- 모달 닫기 시 필터 초기화

**반복 예정 배너 개선**
- 배너 아이템 **탭** → 폼 모달 pre-fill (날짜=당월 예정일, 마스터 데이터 자동 입력). 저장 시 `recurringMasterId` 연결 → 배너 자동 제거
- 배너 아이템 **[×]** → 삭제 옵션 바텀 시트 (`@click.stop` 버블링 차단)
- 배너 헤더: 수입/지출/저축 금액 색상 구분 표시

**mock 데이터 (`MockupView.vue`)**
- `MOCK_DATA_VERSION = 'v11'` (버전 변경 시 localStorage 자동 초기화 + 초기 데이터 즉시 저장)
- 저축 거래(id=20,21,22) 및 반복 마스터(id=2,3): `paymentMethodId` → 저축 수단 ID(103)로 교체

**UpcomingWidget.vue 인터랙션 (카드 결제 전용으로 분리)**
- 헤더 탭 → 아코디언 펼침/닫힘
- 카드 항목 탭 → `filter-card` emit → MockupView가 카드 필터 칩 표시 + 거래 목록 필터링
- D-day 배지: D-day(빨강) / D-7 이내(주황) / 지난 결제(회색)

---

## Step 2 — UI/UX 확정 ✅ 승인 완료 (2026-03-16)

> Step 1 목업을 로컬에서 `/mock-up` 접속하여 확인 후 승인해주세요.

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

## Step 3 — 실제 구현 ⬜ 보류 (Sprint 8에서 진행)

> **전략 변경**: 통계/설정 화면 목업(Sprint 7)까지 완료 후 Sprint 8에서 카드 결제 현황 실서비스를 포함한 전체 구현을 진행합니다.
> Step 3 세부 태스크(T34~T37)는 아래에 유지하되, Sprint 8 계획 시 참조합니다.

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
