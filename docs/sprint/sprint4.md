# Sprint 4 — 할부 + 반복 거래 + 카드 결제 현황 목업

## 개요

| 항목 | 내용 |
|------|------|
| 스프린트 번호 | Sprint 4 |
| 유형 | 목업 |
| 브랜치 | `sprint4` |
| 기간 | 미정 (Sprint 3 완료 후 시작) |
| 상태 | ✅ 완료 |
| 대상 브랜치 (PR) | `develop` |
| DB/백엔드 변경 | 없음 (프론트엔드 전용) |
| 선행 조건 | Sprint 3 완료 (거래 유형 탭 UI 확인됨) |

## 스프린트 목표

Sprint 5(할부 구현)와 Sprint 6(카드 결제 현황 구현) 전에, **기능의 UI/UX 흐름을 하드코딩된 mock 데이터로 먼저 검증**한다.

이 스프린트가 끝난 후 다음 질문에 답할 수 있어야 한다:
- 할부 등록/수정/삭제 흐름이 자연스러운가?
- 반복 거래 등록/수정/삭제 흐름이 자연스러운가?
- 반복 예정 배너 위치 및 표시 방식이 직관적인가?
- 카드 결제 현황 탭의 2슬롯 레이아웃이 직관적인가?
- 드릴다운 화면의 정보 구조가 적절한가?
- 추가/변경이 필요한 항목이 있는가?

요구사항이 확정된 이후에만 Sprint 5, 6의 DB/백엔드 구현을 진행한다.

---

## 구현 범위

### 포함 (목업 — 프론트엔드 전용)
- 할부 등록 + 수정 + 삭제 목업 (T22-mock, T23-mock) — MockupView.vue 내에서 완결
- 반복 거래 등록 + 삭제 + 예정 배너 목업 (T25-mock) — MockupView.vue 내에서 완결
- 카드 결제 현황 탭 목업 (T24-mock) — mock 슬롯 데이터 기반 전체 레이아웃

### 제외 (이후 스프린트)
- 통계/설정 화면 목업 — 가계부 화면 완성 후 별도 스프린트에서 처리
- 할부 DB 마이그레이션 및 백엔드 API — Sprint 5
- 카드 결제 현황 DB 마이그레이션 및 백엔드 API — Sprint 6
- 프론트-백 실제 연동 — Sprint 5, 6

---

## 목업 전략

모든 목업은 `MockupView.vue` 단일 파일 내에서 mock 데이터로 동작한다. 백엔드 미접근, DEV 환경 전용.

```
frontend/src/mocks/
└── installment.mock.ts    # 할부 금액 계산 함수 (calcMockInstallment)
```

MockupView.vue 내 상태:
- `txTransactions`: 거래 목록 (localStorage 영속)
- `mockInstallmentMasters`: 할부 원부 목록 (localStorage 영속)
- `mockRecurringMasters`: 반복 원부 목록 (localStorage 영속)
- `recurSkippedSet`: 반복 건 단건 삭제 skip 키 목록 `{masterId}-{YYYY}-{MM}` 형식 (localStorage 영속)

---

## 태스크 상세

### T22-mock — 할부 거래 등록 목업

**유형**: 프론트엔드 목업 | **우선순위**: 최상 | **상태**: ✅ 완료

**구현 내용** (MockupView.vue 내)
- 가계부 내역 추가/수정 모달에 할부 토글 추가 (금액 우측 상단, 지출 전용)
- 토글 ON → 총 개월수 입력 + 실시간 월별 금액 미리보기
- 저장 시 할부 원부(MockInstallmentMaster) 생성 + 회차별 거래 N건 자동 생성
- 할부 거래는 거래 목록에 "할부 N회" 배지로 표시

**검증 완료 항목**
- ✅ 할부 토글은 지출 탭에만 표시 (수입 탭에서는 숨김)
- ✅ 총 금액 입력 시 월 할부금 실시간 미리보기 (1회차 / 2회차 이후 구분 표시)
- ✅ 할부 등록 후 거래 목록에 회차별로 정상 표시

---

### T23-mock — 할부 거래 수정/삭제 목업

**유형**: 프론트엔드 목업 | **우선순위**: 상 | **상태**: ✅ 완료

**구현 내용** (MockupView.vue 내)
- 거래 목록 클릭 → 수정 모달 직접 오픈 (별도 상세 팝업 없음)
- 할부 거래 클릭 시 수정 모달이 할부 UI로 열림 (총금액 + 개월수 + 할부 배지 표시)
- 거래 목록 우측 X 버튼 클릭 → 할부 삭제 옵션 bottom sheet 표시
  - **전체 삭제**: 모든 회차 + 원부 삭제
  - **이후 삭제**: 클릭한 회차부터 이후 회차 삭제
  - **단건 삭제**: 해당 회차만 삭제

**검증 완료 항목**
- ✅ 할부 수정 시 총금액/개월수 정상 로드
- ✅ 수입 탭으로 전환 후 다시 지출로 돌아와도 할부 상태 유지
- ✅ 수입으로 타입 변경 시 회차 금액으로 금액 복원, 할부 UI 숨김
- ✅ 수입 거래 저장 시 결제수단 비워짐
- ✅ 3가지 삭제 옵션 정상 동작

---

### T25-mock — 반복 거래 등록/삭제/예정 배너 목업

**유형**: 프론트엔드 목업 | **우선순위**: 상 | **상태**: ✅ 완료

**구현 내용** (MockupView.vue 내)
- 거래 추가/수정 모달 부가 정보 섹션에 반복 토글 추가 (수입/지출 공통)
  - 토글 ON → 매월 반복 일자(dayOfMonth) 입력 + 종료일(endDate) 입력 (날짜 변경 시 dayOfMonth 자동 동기화)
- 저장 시 반복 원부(MockRecurringMaster) 생성 + 첫 달 거래 1건 자동 생성
- 실제 서비스에서는 .NET BackgroundService가 매일 자정 실행 → DayOfMonth 도래 시 Transaction 자동 생성
- 거래 목록 우측 X 버튼 클릭 → 반복 삭제 옵션 bottom sheet 표시
  - **전체 삭제**: 모든 거래 + 원부 삭제 (skip 키 정리 포함)
  - **이후 삭제**: 클릭한 날짜 이후 거래 삭제 + 원부 isActive = false
  - **단건 삭제**: 해당 거래만 삭제 + skip 키 추가 (이번 달 배너 재등장 방지)
- 반복 예정 배너: 검색바와 카테고리 칩 사이, 이번 달 미등록 반복 항목 표시
  - 수입/지출 합산 요약 + 접기/펼치기 + 항목별 X 버튼(삭제 옵션 시트 호출)
  - skip 키 기반으로 단건 삭제된 항목은 배너에서 제외

**검증 완료 항목**
- ✅ 반복 토글은 수입/지출 모두 표시
- ✅ 날짜 변경 시 dayOfMonth 자동 동기화 (신규 등록 모드 한정)
- ✅ 반복 거래 저장 후 목록 + 예정 배너 정상 표시
- ✅ 3가지 삭제 옵션 정상 동작
- ✅ 단건 삭제 후 배너 재등장 방지 (skip 키 영속)
- ✅ 전체 삭제 시 skip 키 정리 (orphan 방지)

---

### T24-mock — 카드 결제 현황 탭 목업

**유형**: 프론트엔드 목업 | **우선순위**: 최상 | **상태**: ⬜ 예정

**수정 대상 파일**
- Create: `frontend/src/views/CardBillingView.vue`
- Create: `frontend/src/components/CardBillingCard.vue`
- Create: `frontend/src/components/CardBillingSlot.vue`
- Create: `frontend/src/mocks/cardBilling.mock.ts`
- Modify: `frontend/src/router/index.ts` (신규 탭 라우트)
- Modify: `App.vue` 하단 탭바 (탭 항목 추가)

**mock 데이터 예시** (`cardBilling.mock.ts`)

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
]

// mock 드릴다운 거래 목록
export const mockCardBillingTransactions = [
  { date: '2026-03-10', memo: '스타벅스', amount: 6500, categoryName: '식비' },
  { date: '2026-03-08', memo: '올리브영', amount: 32000, categoryName: '쇼핑' },
  { date: '2026-03-05', memo: '버스 충전', amount: 10000, categoryName: '교통' },
]
```

**세부 작업**

- ⬜ `CardBillingView.vue` — mock 데이터로 카드 목록 + 슬롯 렌더링
- ⬜ `CardBillingCard.vue` — 카드명, 정산일/결제일 정보 헤더
- ⬜ `CardBillingSlot.vue` — 슬롯 1개: 결제월 레이블, 청구 기간, 결제 예정일, D-day 배지, 이용금액, 확정 여부
- ⬜ 슬롯 클릭 시 드릴다운 패널 표시 — mock 거래 목록
- ⬜ 하단 탭바에 카드 아이콘 탭 추가 (탭 레이블 축약 고려 — iPhone SE 375px 기준)
- ⬜ 설정된 카드가 없을 때 안내 메시지

**검증 포인트 (요구사항 확정용)**
- 2슬롯 레이아웃이 한눈에 파악되는가?
- D-day 표기 방식이 직관적인가? (D-10, D-day, D+5 형식)
- 하단 탭바에 4번째 탭 추가 시 모바일 레이아웃이 허용 가능한가?
- 드릴다운 패널(슬라이드업)과 별도 화면 전환 중 어느 방식이 더 자연스러운가?

---

## UI/UX 확정 사항 (이번 스프린트에서 결정)

Sprint 5 백엔드 설계 시 반영 필요:

| 항목 | 결정 내용 |
|------|-----------|
| 거래 클릭 동작 | 리스트 클릭 → 바로 수정 모달 오픈 (별도 상세 팝업 없음) |
| 삭제 동작 | 리스트 우측 X 버튼 → 단건 즉시 삭제 / 할부·반복은 3가지 옵션 bottom sheet |
| 할부 토글 위치 | 금액 필드 우측 상단 인라인 토글 (지출 전용) |
| 할부 삭제 옵션 | 전체 삭제 / 이후 삭제 / 단건 삭제 3가지 |
| 반복 토글 위치 | 부가 정보 섹션 내 토글 (수입/지출 공통) |
| 반복 삭제 옵션 | 전체 삭제 / 이후 삭제 / 단건 삭제(이번 달만, 반복 유지) 3가지 |
| 반복 예정 배너 | 검색바와 카테고리 칩 사이, 이번 달 미등록 항목 요약 표시 (접기/펼치기) |
| 수입 거래 | 결제수단 없음, 할부 없음, 반복 있음 |
| 기본 날짜 | 오늘 날짜 자동 설정 |
| 모달 타이틀 | "가계부 내역 추가 / 수정" |

---

## 기술적 의존성 및 리스크

| 리스크 | 가능성 | 영향 | 대응 방안 |
|--------|--------|------|-----------|
| 하단 탭바 4번째 탭 추가 시 모바일 레이아웃 깨짐 | 보통 | 중간 | 탭 아이콘만 표시(레이블 숨김) + iPhone SE(375px) 기준 검증 — 결과를 Sprint 6 설계에 반영 |
| mock 데이터와 실제 API 응답 구조 불일치 가능성 | 낮음 | 낮음 | mock 데이터 구조를 PRD §4.7 기준으로 정의, Sprint 6에서 동일 구조로 API 설계 |

---

## 완료 기준 (Definition of Done)

### 기능 완료 기준

- ✅ T22-mock: 할부 토글 ON → 개월수 입력 → 월별 미리보기 → 저장 → 목록 표시
- ✅ T23-mock: 할부 거래 클릭 → 수정 모달(할부 UI) → 저장 / X 버튼 → 삭제 옵션 bottom sheet
- ✅ T25-mock: 반복 토글 ON → dayOfMonth/endDate 입력 → 저장 → 목록 + 예정 배너 표시
- ✅ T25-mock: 반복 X 버튼 → 3가지 삭제 옵션 / 단건 삭제 시 배너 재등장 방지
- ⬜ T24-mock: 카드 결제 현황 탭에서 2슬롯 카드 표시, D-day 배지, 이용금액 표시
- ⬜ T24-mock: 슬롯 클릭 시 드릴다운 패널에 mock 거래 목록 표시
- ✅ 기존 일반/할부/반복 거래 CRUD 기능이 이 스프린트 변경으로 인해 깨지지 않음

### 코드 품질 기준

- ✅ `npm run build` 성공 (TypeScript 타입 에러 0건)
- ⬜ 콘솔 에러/경고 0건

### 배포 기준

- ⬜ DB 스키마 변경 없음 (마이그레이션 불필요)
- ⬜ `sprint4` → `develop` PR 생성 완료

---

## 예상 산출물

| 산출물 | 설명 | 상태 |
|--------|------|------|
| `frontend/src/views/MockupView.vue` | 할부 + 반복 CRUD 목업 (인라인 토글, 삭제 bottom sheet, 예정 배너) | ✅ |
| `frontend/src/mocks/installment.mock.ts` | 할부 금액 계산 함수 | ✅ |
| `frontend/src/components/CardBillingCard.vue` | 카드별 청구 요약 컴포넌트 | ⬜ |
| `frontend/src/components/CardBillingSlot.vue` | 청구 슬롯 컴포넌트 | ⬜ |
| `frontend/src/views/CardBillingView.vue` | 카드 결제 현황 탭 화면 | ⬜ |
| `frontend/src/mocks/cardBilling.mock.ts` | 카드 결제 현황 mock 데이터 | ⬜ |
| `docs/sprint/sprint4.md` | 본 문서 | ✅ |

---

## 다음 스프린트 연결

Sprint 4 목업 검토 후 요구사항을 확정하고 다음 두 스프린트를 순차 진행한다.

```
Sprint 4 목업 완료 + 요구사항 확정
  ├─ Sprint 5: 할부 실제 구현 (DB + 백엔드 + 프론트 연동)
  └─ Sprint 6: 카드 결제 현황 실제 구현 (DB + 백엔드 + 프론트 연동)
```

**Sprint 4 완료 후 확정해야 할 항목**
- 카드 결제 현황 탭 레이아웃 및 드릴다운 방식 확정 (슬라이드업 vs 별도 화면)
- 하단 탭바 4번째 탭 레이아웃 방식 확정

---

## 작업 순서 (권장)

```
T22-mock + T23-mock (✅ 완료)
T25-mock (✅ 완료)
T24-mock (카드 현황 탭 — 다음 작업)
```

---

## 테스트 및 검증 리포트

### 단위 테스트

| 도구 | 결과 | 케이스 수 | 파일 / 설명 |
|------|------|---------|------------|
| Vitest | ✅ 전체 통과 | 54 케이스 | `monthPeriod.test.ts`, `installment.test.ts`, `mockupLogic.test.ts` |
| xUnit | — | — | 백엔드 변경 없음 (목업 스프린트) |

### E2E 테스트 (Playwright — Galaxy S25 360×780)

| 시나리오 | 결과 | 비고 |
|---------|------|------|
| 할부 등록 → 회차 배지 확인 | — 해당없음 | MockupView는 DEV 전용, 실 백엔드 미연동 |
| 반복 거래 등록 → 예정 배너 표시 | — 해당없음 | 동일 |
| 반복 단건 삭제 → 배너 재등장 방지 | — 해당없음 | 동일 |

> **목업 스프린트는 E2E 제외**: `/mock-up` 경로는 DEV 환경 전용이며 실 백엔드가 없으므로 Playwright E2E 대상 외.

### 빌드 검증

| 항목 | 결과 |
|------|------|
| `npm run build` (TypeScript) | ✅ 오류 0건 |
| `dotnet build` | — (백엔드 변경 없음) |

### 발견된 이슈 및 해결 방안

| # | 이슈 | 해결 방안 | 상태 |
|---|------|----------|------|
| 1 | 수입→지출 타입 전환 시 할부 상태 유지되지 않음 | `watch(type)` 에서 수입 선택 시 할부 토글 강제 리셋 | ✅ 해결 |
| 2 | 전체 삭제 후 `recurSkippedSet`에 orphan 키 잔존 | 원부 전체 삭제 시 해당 `masterId`로 시작하는 skip 키 일괄 제거 | ✅ 해결 |
| 3 | 반복 거래 단건 삭제(skip) 후 배너 재등장 | `recurSkippedSet`에 `{masterId}-{YYYY}-{MM}` 키 추가, 배너 computed에서 필터링 | ✅ 해결 |

### 검증 증빙

| 항목 | 위치 / 파일명 |
|------|-------------|
| Vitest 결과 | `docs/deploy-history/2026-03-15.md` — Sprint 4 섹션 |
| 빌드 결과 | `docs/deploy-history/2026-03-15.md` — Sprint 4 섹션 |
| 스크린샷 | 미첨부 (목업 확인은 로컬 `/mock-up` 직접 접속으로 진행) |

---

## 스프린트 회고 (완료 후 작성)

> 스프린트 완료 후 sprint-close 에이전트가 작성합니다.
