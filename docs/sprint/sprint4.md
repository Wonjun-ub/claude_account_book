# Sprint 4 — 할부 + 카드 결제 현황 목업

## 개요

| 항목 | 내용 |
|------|------|
| 스프린트 번호 | Sprint 4 |
| 유형 | 목업 |
| 브랜치 | `sprint4` |
| 기간 | 미정 (Sprint 3 완료 후 시작) |
| 상태 | ⬜ 예정 |
| 대상 브랜치 (PR) | `develop` |
| DB/백엔드 변경 | 없음 (프론트엔드 전용) |
| 선행 조건 | Sprint 3 완료 (거래 유형 탭 UI 확인됨) |

## 스프린트 목표

Sprint 5(할부 구현)와 Sprint 6(카드 결제 현황 구현) 전에, **두 기능의 UI/UX 흐름을 하드코딩된 mock 데이터로 먼저 검증**한다.

이 스프린트가 끝난 후 다음 질문에 답할 수 있어야 한다:
- 할부 상세 팝업에서 필요한 정보가 모두 표시되는가?
- 카드 결제 현황 탭의 2슬롯 레이아웃이 직관적인가?
- 드릴다운 화면의 정보 구조가 적절한가?
- 추가/변경이 필요한 항목이 있는가?

요구사항이 확정된 이후에만 Sprint 5, 6의 DB/백엔드 구현을 진행한다.

---

## 구현 범위

### 포함 (목업 — 프론트엔드 전용)
- 할부 거래 상세 팝업 목업 (T22-mock) — mock 할부 데이터 기반
- 할부 등록 모달 mock 완성 (T23-mock) — Sprint 3에서 "준비 중" 처리한 할부 탭을 mock 저장으로 업그레이드
- 카드 결제 현황 탭 목업 (T24-mock) — mock 슬롯 데이터 기반 전체 레이아웃
- 카드 청구 설정 UI 목업 (T25-mock) — 설정 화면에서 정산일/결제일 입력 폼 (저장 mock)

### 제외 (이후 스프린트)
- 할부 DB 마이그레이션 및 백엔드 API — Sprint 5
- 카드 결제 현황 DB 마이그레이션 및 백엔드 API — Sprint 6
- 프론트-백 실제 연동 — Sprint 5, 6

---

## 목업 전략

### mock 데이터 파일 구조

```
frontend/src/mocks/
├── installment.mock.ts    # 할부 mock 데이터 및 함수 (Sprint 3에서 생성 → 이 스프린트에서 확장)
└── cardBilling.mock.ts    # 카드 결제 현황 mock 데이터
```

**installment.mock.ts 확장 내용**

```typescript
// Sprint 4에서 추가: mock 할부 원부 목록 (상세 팝업용)
export const mockInstallmentList = [
  {
    id: 1,
    totalAmount: 1200000,
    monthlyAmount: 100000,
    firstMonthExtra: 0,
    totalInstallments: 12,
    remainingInstallments: 9,
    startDate: '2026-01-15',
    categoryId: 3,
    paymentMethodId: 2,
    memo: '노트북 할부',
    isActive: true,
  },
  {
    id: 2,
    totalAmount: 300000,
    monthlyAmount: 100000,
    firstMonthExtra: 0,
    totalInstallments: 3,
    remainingInstallments: 1,
    startDate: '2026-01-20',
    categoryId: 5,
    paymentMethodId: 2,
    memo: '청소기',
    isActive: true,
  },
]

// mock 할부 상세 조회
export async function mockGetInstallment(id: number) {
  return mockInstallmentList.find(i => i.id === id) ?? null
}
```

**cardBilling.mock.ts**

```typescript
// 카드 결제 현황 mock 데이터 — PRD §4.7 케이스 A 기준
// 정산일=15, 결제일=25 (같은 달 결제)
export const mockCardBillingSummary = [
  {
    paymentMethodId: 2,
    cardName: '신한카드',
    billingCutoffDay: 15,
    paymentDueDay: 25,
    slots: [
      {
        slotIndex: 1,
        label: '3월 청구금액',
        periodFrom: '2026-02-15',
        periodTo: '2026-03-14',
        paymentDueDate: '2026-03-25',
        dDayLabel: 'D-10',
        totalAmount: 287000,
        transactionCount: 12,
        isConfirmed: false,
      },
      {
        slotIndex: 2,
        label: '4월 청구금액',
        periodFrom: '2026-03-15',
        periodTo: '2026-04-14',
        paymentDueDate: '2026-04-25',
        dDayLabel: 'D+41',
        totalAmount: 43000,
        transactionCount: 3,
        isConfirmed: false,
      },
    ],
  },
  {
    paymentMethodId: 3,
    cardName: '국민카드',
    billingCutoffDay: 25,
    paymentDueDay: 10,
    slots: [
      {
        slotIndex: 1,
        label: '4월 청구금액',
        periodFrom: '2026-02-26',
        periodTo: '2026-03-25',
        paymentDueDate: '2026-04-10',
        dDayLabel: 'D+26',
        totalAmount: 156000,
        transactionCount: 7,
        isConfirmed: false,
      },
      {
        slotIndex: 2,
        label: '5월 청구금액',
        periodFrom: '2026-03-26',
        periodTo: '2026-04-25',
        paymentDueDate: '2026-05-10',
        dDayLabel: 'D+56',
        totalAmount: 12000,
        transactionCount: 1,
        isConfirmed: false,
      },
    ],
  },
]

// mock 드릴다운 거래 목록
export const mockCardBillingTransactions = [
  { date: '2026-03-10', memo: '스타벅스', amount: 6500, categoryName: '식비' },
  { date: '2026-03-08', memo: '올리브영', amount: 32000, categoryName: '쇼핑' },
  { date: '2026-03-05', memo: '버스 충전', amount: 10000, categoryName: '교통' },
]
```

---

## 태스크 상세

### T22-mock — 할부 거래 상세 팝업 목업

**유형**: 프론트엔드 목업 | **우선순위**: 최상 | **예상 소요**: 1일

**수정 대상 파일**
- Create: `frontend/src/components/InstallmentDetailModal.vue`
- Modify: `frontend/src/views/HomeView.vue` (거래 클릭 이벤트 분기)
- Modify: `frontend/src/mocks/installment.mock.ts` (mock 원부 목록 추가)

**표시 항목**
- 총 금액 (원금)
- 월 할부금 (이번 달 나가는 금액)
- 남은 금액 (남은 개월수 × 월 할부금)
- 진행 현황 (예: 3/12회차)
- 결제수단, 카테고리, 메모
- "할부 취소" 버튼 (mock — 실제 삭제 없이 모달 닫기만)

**세부 작업**

- ⬜ `InstallmentDetailModal.vue` 컴포넌트 생성 (props: installmentId)
- ⬜ `HomeView.vue`에서 거래 클릭 시 분기: `InstallmentTransactionId` 존재 여부에 따라
  - 일반/반복 거래 → 기존 `TransactionModal.vue`
  - 할부 거래 → `InstallmentDetailModal.vue` (mock 데이터로 표시)
- ⬜ mock 데이터에서 상세 로드 (`mockGetInstallment(id)`)
- ⬜ 팝업 내 정보 레이아웃 확인 — 항목 배치, 금액 포맷, 진행 현황 시각화(프로그레스 바 또는 텍스트)
- ⬜ "할부 취소" 버튼 클릭 시 "실제 기능은 Sprint 5에서 구현됩니다" 알림

**검증 포인트 (요구사항 확정용)**
- 표시 항목이 PRD §4.6에 명시된 내용과 일치하는가?
- 남은 금액 계산 방식이 직관적인가? (남은 개월수 × 월 할부금 vs 총금액 - 납부금액)
- 추가로 표시할 항목이 있는가?

---

### T23-mock — 할부 등록 모달 목업 완성

**유형**: 프론트엔드 목업 | **우선순위**: 상 | **예상 소요**: 0.5일

**수정 대상 파일**
- Modify: `frontend/src/components/TransactionModal.vue`
- Modify: `frontend/src/mocks/installment.mock.ts`

Sprint 3(T21)에서 할부 탭 저장 시 "준비 중" 토스트만 표시했다. 이 스프린트에서는 mock 저장으로 업그레이드하여 UI 흐름 전체를 시뮬레이션한다.

**세부 작업**

- ⬜ 할부 탭 저장 시 `mockCreateInstallment()` 호출 → mock 거래 목록에 임시 항목 추가
- ⬜ 저장 후 모달 닫힘 + 거래 목록 갱신 (mock 데이터 기반)
- ⬜ 등록된 mock 할부 항목 클릭 시 `InstallmentDetailModal.vue` 표시 연계 확인
- ⬜ 총 금액 입력 시 실시간으로 월 할부금 미리보기 업데이트

**검증 포인트**
- 할부 탭 입력 → 저장 → 목록 표시 → 클릭 → 상세 팝업의 전체 흐름이 자연스러운가?
- 입력 항목(총 금액, 총 개월수, 시작일, 카테고리, 결제수단)이 충분한가?

---

### T24-mock — 카드 결제 현황 탭 목업

**유형**: 프론트엔드 목업 | **우선순위**: 최상 | **예상 소요**: 2일

**수정 대상 파일**
- Create: `frontend/src/views/CardBillingView.vue`
- Create: `frontend/src/components/CardBillingCard.vue`
- Create: `frontend/src/components/CardBillingSlot.vue`
- Create: `frontend/src/mocks/cardBilling.mock.ts`
- Modify: `frontend/src/router/index.ts` (신규 탭 라우트)
- Modify: 하단 탭바 컴포넌트 (탭 항목 추가)

**세부 작업**

- ⬜ `CardBillingView.vue` — mock 데이터로 카드 목록 + 슬롯 렌더링
- ⬜ `CardBillingCard.vue` — 카드명, 정산일/결제일 정보 헤더
- ⬜ `CardBillingSlot.vue` — 슬롯 1개 컴포넌트: 결제월 레이블, 청구 기간, 결제 예정일, D-day 배지, 이용금액, 확정 여부
- ⬜ 슬롯 클릭 시 드릴다운 패널 표시 — mock 거래 목록 (`mockCardBillingTransactions`)
- ⬜ 하단 탭바에 카드 아이콘 탭 추가 (탭 레이블 축약 고려 — iPhone SE 375px 기준)
- ⬜ 설정된 카드가 없을 때 안내 메시지 + 설정 화면 이동 링크

**검증 포인트 (요구사항 확정용)**
- 2슬롯 레이아웃이 한눈에 파악되는가?
- D-day 표기 방식이 직관적인가? (D-10, D-day, D+5 형식)
- 하단 탭바에 5번째 탭 추가 시 모바일 레이아웃이 허용 가능한가?
- 드릴다운 패널(슬라이드업)과 별도 화면 전환 중 어느 방식이 더 자연스러운가?
- 카테고리별 소계를 드릴다운에서 보여줄 필요가 있는가?

---

### T25-mock — 카드 청구 설정 UI 목업

**유형**: 프론트엔드 목업 | **우선순위**: 상 | **예상 소요**: 0.5일

**수정 대상 파일**
- Modify: `frontend/src/views/SettingsView.vue`

**세부 작업**

- ⬜ Card 타입 결제수단 항목에 "청구 설정" 버튼/링크 추가
- ⬜ 청구 설정 인라인 폼 또는 모달 — 정산일(1~28), 결제일(1~28) 입력
- ⬜ 저장 버튼 클릭 시 mock 저장 (로컬 상태 업데이트) + "Sprint 6에서 실제 저장됩니다" 안내
- ⬜ 설정 완료 후 카드 결제 현황 탭 바로가기 링크 표시

**검증 포인트**
- 설정 화면에서 정산일/결제일 입력이 자연스러운가?
- 1~28 범위 제한에 대한 안내가 필요한가?

---

## 기술적 의존성 및 리스크

| 리스크 | 가능성 | 영향 | 대응 방안 |
|--------|--------|------|-----------|
| 하단 탭바 5번째 탭 추가 시 모바일 레이아웃 깨짐 | 보통 | 중간 | 탭 아이콘만 표시(레이블 숨김) + iPhone SE(375px) 기준 검증 — 결과를 Sprint 6 설계에 반영 |
| mock 데이터와 실제 API 응답 구조 불일치 가능성 | 낮음 | 낮음 | mock 데이터 구조를 PRD §4.7 기준으로 정의, Sprint 6에서 동일 구조로 API 설계 |
| T22-mock에서 HomeView 분기 로직이 기존 거래 수정 흐름에 영향 | 보통 | 높음 | `InstallmentTransactionId` 필드가 mock 데이터에만 존재하므로 실제 거래에는 영향 없음 확인 필수 |

---

## 완료 기준 (Definition of Done)

### 기능 완료 기준

- ⬜ T22-mock: 할부 거래 클릭 시 상세 팝업 표시 (총금액/월할부금/남은금액/진행회차)
- ⬜ T23-mock: 할부 탭 입력 → mock 저장 → 목록 표시 → 상세 팝업 연계 전체 흐름 동작
- ⬜ T24-mock: 카드 결제 현황 탭에서 2슬롯 카드 표시, D-day 배지, 이용금액 표시
- ⬜ T24-mock: 슬롯 클릭 시 드릴다운 패널에 mock 거래 목록 표시
- ⬜ T25-mock: 설정 화면에서 카드 정산일/결제일 입력 폼 동작 (mock 저장)
- ⬜ 기존 일반/반복 거래 CRUD 기능이 이 스프린트 변경으로 인해 깨지지 않음

### 코드 품질 기준

- ⬜ `npm run build` 성공 (TypeScript 타입 에러 0건)
- ⬜ 콘솔 에러/경고 0건

### 배포 기준

- ⬜ DB 스키마 변경 없음 (마이그레이션 불필요)
- ⬜ `sprint4` → `develop` PR 생성 완료

---

## 예상 산출물

| 산출물 | 설명 |
|--------|------|
| `frontend/src/components/InstallmentDetailModal.vue` | 할부 상세 팝업 (mock 데이터 기반) |
| `frontend/src/components/CardBillingCard.vue` | 카드별 청구 요약 컴포넌트 |
| `frontend/src/components/CardBillingSlot.vue` | 청구 슬롯 컴포넌트 |
| `frontend/src/views/CardBillingView.vue` | 카드 결제 현황 탭 화면 |
| `frontend/src/mocks/installment.mock.ts` | 할부 mock 데이터 확장 |
| `frontend/src/mocks/cardBilling.mock.ts` | 카드 결제 현황 mock 데이터 |
| `docs/sprint/sprint4.md` | 본 문서 |

---

## 다음 스프린트 연결

Sprint 4 목업 검토 후 요구사항을 확정하고 다음 두 스프린트를 순차 진행한다.

```
Sprint 4 목업 완료 + 요구사항 확정
  ├─ Sprint 5: 할부 실제 구현 (DB + 백엔드 + 프론트 연동)
  └─ Sprint 6: 카드 결제 현황 실제 구현 (DB + 백엔드 + 프론트 연동)
```

**Sprint 4 완료 후 확정해야 할 항목**
- 할부 상세 팝업 표시 항목 최종 확정
- 카드 결제 현황 탭 레이아웃 및 드릴다운 방식 확정 (슬라이드업 vs 별도 화면)
- 하단 탭바 5번째 탭 레이아웃 방식 확정

---

## 작업 순서 (권장)

```
T22-mock + T23-mock (할부 흐름 완성 — T22가 T23 상세 팝업 연계에 필요)
T24-mock (카드 현황 탭 — 독립적으로 병행 가능)
T25-mock (설정 화면 — 독립적)
```

T22-mock과 T23-mock은 할부 등록 → 목록 → 상세 팝업 흐름을 연계하므로 순서대로 진행 권장.
T24-mock, T25-mock은 독립적이므로 병행 개발 가능.

---

## 스프린트 회고 (완료 후 작성)

> 스프린트 완료 후 sprint-close 에이전트가 작성합니다.
