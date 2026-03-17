# Sprint 9 — Local-first 실서비스 구현

## 개요

| 항목 | 내용 |
|------|------|
| 스프린트 번호 | Sprint 9 |
| 유형 | 구현 (Local-first) |
| 브랜치 | `sprint9` (sprint8 기준 분기) |
| 기간 | 2026-03-17 ~ |
| 상태 | ⬜ 대기 |
| 대상 브랜치 (PR) | `develop` |
| DB/백엔드 변경 | **없음** — IndexedDB(Dexie.js) 기반, 백엔드 완전 제거 |
| 선행 조건 | Sprint 8 Step 2 승인 ✅ (2026-03-17) |

---

## 스프린트 목표

Sprint 1~8에서 완성한 MockupView(localStorage 기반)의 모든 기능을 **Dexie.js(IndexedDB) 기반 실서비스**로 이관한다.

- `HomeView.vue` — 가계부 탭 전체: Dexie 연동 거래 CRUD, 월 요약, 반복/할부, 카드 결제 위젯
- `StatsView.vue` — 통계 탭 전체: Dexie 기반 카테고리 집계, 전월 비교, 추이 차트
- `SettingsView.vue` — 설정 탭 전체: 카테고리/결제수단/저축 수단 CRUD, 월 시작일
- Pinia stores 교체: 기존 API 호출 → Dexie 직접 읽기/쓰기
- `frontend/src/types/index.ts` — Dexie 스키마와 정합성 맞춰 전면 교체
- PWA 아이콘 생성

---

## 구현 범위 (포함/제외)

### 포함

| ID | 항목 |
|----|------|
| T34 | Pinia stores 재구현 (`useTransactionStore`, `useCategoryStore`, `useSettingsStore`, `usePaymentMethodStore`) |
| T35 | 거래 CRUD — 단건/할부/반복 생성·수정·삭제 (`db.ts` 헬퍼 활용) |
| T36 | 카드 결제 현황 로컬 계산 (정산일/결제일 기준 청구 기간 계산) |
| T37 | 반복 거래 자동 적용 (`applyRecurringForMonth` 월 진입 시 호출) |
| T45 | 저축 수단 CRUD (`db.savingsMethods`) |
| T46 | 설정 CRUD — monthStartDay, 카테고리, 결제수단, 포인트 예산 |
| T47 | 포인트 잔액 자동 차감/복구 (Dexie transaction 보장) |
| T48 | HomeView — Dexie 연동 |
| T49 | StatsView — Dexie 연동 |
| T50 | SettingsView — Dexie 연동 |
| T51 | PWA 아이콘 생성 (`public/icons/icon-192.png`, `icon-512.png`) |

### 제외 (스코프 아웃)

- 기존 `.NET` 백엔드 코드 삭제 (별도 스프린트 또는 수동 처리)
- MockupView.vue 삭제 (DEV 전용 파일이므로 유지)
- 기기 간 동기화, CSV 내보내기
- 사용자 인증

---

## 기술적 접근 방법

### 핵심 아키텍처 결정

```
MockupView (localStorage)     →     실서비스 Views (Dexie.js)
─────────────────────────            ──────────────────────────
ref 기반 로컬 상태              →     Pinia store + Dexie reactive query
mock 함수 (CRUD)               →     db.ts 헬퍼 + store action
settingMonthStartDay ref       →     useSettingsStore.monthStartDay
settingCategories ref          →     useCategoryStore.categories
settingPaymentMethods ref      →     usePaymentMethodStore.paymentMethods
```

### 타입 시스템 전환

`frontend/src/types/index.ts`는 현재 구 백엔드 API DTO 기준이므로 `db.ts` 인터페이스를 단일 진실 소스로 삼아 전면 재작성한다.

- `db.ts`에서 export되는 `Transaction`, `Category`, `PaymentMethod` 등을 타입으로 직접 사용하거나 `types/index.ts`를 db.ts 타입의 re-export + 뷰 전용 타입(정제 후 화면에 표시할 composite 타입)으로 재정의

### Pinia Store 설계 원칙

- **단일 DB 접근 레이어**: 모든 Dexie 호출은 store action을 통해서만 수행 (View에서 `db.*` 직접 호출 금지)
- **반응형 재조회**: action 실행 후 해당 store의 상태를 항상 재조회하여 화면 자동 갱신
- **seedDatabase 호출**: `App.vue` `onMounted`에서 한 번만 실행

### 날짜 처리 원칙 (필수)

- 모든 월 범위 계산은 `getMonthPeriod(year, month, monthStartDay)` 사용
- 달력 기준(1일~말일) 직접 계산 절대 금지

---

## 태스크 분해

### Task 1: types/index.ts 재작성 + Pinia stores 골격 구현

**목표**: 타입 정의를 Dexie 스키마와 정합성 맞추고, 4개 store의 기본 골격과 `seedDatabase` 연동을 완성한다.

**파일**
- 수정: `frontend/src/types/index.ts`
- 수정: `frontend/src/stores/app.ts` (→ `useSettingsStore` + 월 네비게이션)
- 신규: `frontend/src/stores/category.ts` (`useCategoryStore`)
- 신규: `frontend/src/stores/paymentMethod.ts` (`usePaymentMethodStore`)
- 신규: `frontend/src/stores/transaction.ts` (`useTransactionStore`)
- 수정: `frontend/src/main.ts` or `App.vue` — `seedDatabase()` 호출

**구현 상세**

`types/index.ts` 재작성 방향:
- `db.ts`에서 이미 export되는 `Transaction`, `Category`, `PaymentMethod`, `SavingsMethod`, `RecurringTransaction`, `RecurringSkip`, `InstallmentTransaction`, `UserSettings` 타입을 그대로 re-export
- 뷰 전용 composite 타입 추가:
  - `TransactionView` — Transaction + 조인된 `categoryName`, `paymentMethodName`, `savingsMethodName`
  - `MonthlySummary` — 로컬 계산 결과 (totalIncome, totalExpense, totalSavings, balance)
  - `CategorySummary` — 카테고리별 집계 (name, amount, percentage)
  - `CardBillingSummary` — 카드별 청구 현황 (id, name, dueDate, periodFrom, periodTo, amount)
- 구 API DTO 타입(MonthlySummary 구버전, CreateTransactionRequest 등) 제거

`useSettingsStore` (`app.ts` 개편):
- 기존 `@/api` 호출 전부 제거
- `loadSettings()` — `db.userSettings.toArray()` 로드
- `saveMonthStartDay(day)` — `db.userSettings.update()` + 재로드
- 월 네비게이션 (`currentYear`, `currentMonth`, `prevMonth`, `nextMonth`) 유지
- `monthStartDay` computed 노출

`useCategoryStore`:
- `categories` ref (isDeleted=false인 항목만)
- `loadCategories()`, `addCategory(name, type)`, `deleteCategory(id)` (isDefault 검사, 연결 거래 존재 시 soft delete만 허용)
- `incomeCategories`, `expenseCategories`, `savingsCategories` computed

`usePaymentMethodStore`:
- `paymentMethods` ref
- `loadPaymentMethods()`, `addPaymentMethod(params)`, `updatePaymentMethod(id, params)`, `deletePaymentMethod(id)`
- `savingsMethods` ref + `loadSavingsMethods()`, `addSavingsMethod(name)`, `deleteSavingsMethod(id)`
- 카드: `updateBillingSettings(id, cutoffDay, dueDay)`
- 포인트: `updatePointBudget(id, totalBudget, remainingBudget)`

`useTransactionStore`:
- `transactions` ref (`TransactionView[]`)
- `loadTransactions(year, month)` — `getMonthPeriod` + Dexie 범위 쿼리 + 카테고리/결제수단 이름 조인
- `summary` computed — 현재 transactions 기반 MonthlySummary
- `applyRecurring()` — `applyRecurringForMonth()` 호출 후 재조회
- 거래 CRUD는 Task 2에서 구현

**검증 방법**
- `npm run build` TypeScript 오류 0건
- `npm run dev` 실행 후 콘솔 오류 없음
- 최초 실행 시 IndexedDB에 시드 데이터 생성 확인 (브라우저 DevTools > Application > IndexedDB)

---

### Task 2: 거래 CRUD — 단건/할부/반복 생성·수정·삭제

**목표**: MockupView의 거래 폼 로직(createTx, updateTx, deleteTx)을 `useTransactionStore` action으로 구현한다.

**파일**
- 수정: `frontend/src/stores/transaction.ts`
- 참조: `frontend/src/database/db.ts` (createInstallment, applyRecurringForMonth 헬퍼)
- 테스트: `frontend/src/tests/transaction.test.ts`

**구현 상세**

`useTransactionStore` action 추가:

```typescript
// 단건 생성
async createSingle(params: CreateTransactionParams): Promise<void>

// 할부 생성 — db.ts의 createInstallment() 래핑
async createInstallment(params: CreateInstallmentParams): Promise<void>

// 반복 원부 생성 — 즉시 거래 생성하지 않음 (on-demand)
async createRecurring(params: CreateRecurringParams): Promise<void>

// 거래 수정 (단건 / 반복 이번달만 / 할부 이번달만)
async updateTransaction(id: number, params: Partial<Transaction>): Promise<void>

// 할부 원부 수정 — 연결된 전체 회차 amount/category/method/memo 갱신
async updateInstallmentMaster(masterId: number, params: UpdateInstallmentParams): Promise<void>

// 반복 원부 수정 — 이미 생성된 거래 불변, 이후 미생성 회차에 반영
async updateRecurringMaster(masterId: number, params: UpdateRecurringParams): Promise<void>

// 단건 삭제 (포인트 잔액 복구 포함)
async deleteTransaction(id: number): Promise<void>

// 할부 삭제 (all / fromHere / single)
async deleteInstallment(masterId: number, mode: 'all' | 'fromHere' | 'single', sequence?: number): Promise<void>

// 반복 삭제 (all / fromHere / skipMonth)
async deleteRecurring(masterId: number, mode: 'all' | 'fromHere' | 'skipMonth', year?: number, month?: number): Promise<void>
```

포인트 잔액 처리 (`usePaymentMethodStore` 협력):
- 포인트 결제수단으로 Expense 생성 시 `db.transaction('rw', ...)` 내에서 `remainingBudget -= amount` 원자적 갱신
- 포인트 거래 삭제 시 `remainingBudget += amount` 복구
- 잔액 부족 시 애플리케이션 레벨 검증 후 오류 throw (useDialog로 표시)

**Vitest 테스트 작성 대상**:
- 단건 생성 후 조회 시 결과에 포함되는가
- 할부 생성 후 회차별 금액(1회차 나머지 보정)이 올바른가
- 반복 생성 후 `getRecurringPending`에 포함되는가
- 포인트 잔액 차감/복구가 원자적으로 동작하는가

**검증 방법**
- `npx vitest run --reporter=verbose`

---

### Task 3: T47 포인트 잔액 자동 차감/복구

**목표**: 거래 저장·삭제·수정 시 포인트 결제수단의 `remainingBudget`이 자동으로 갱신된다.

**파일**
- 수정: `frontend/src/stores/transaction.ts` (createSingle, deleteTransaction, updateTransaction 내 포인트 처리)
- 수정: `frontend/src/stores/paymentMethod.ts` (deductPoint, recoverPoint 헬퍼)

**구현 상세**

```typescript
// usePaymentMethodStore
async deductPoint(paymentMethodId: number, amount: number): Promise<void> {
  // db.transaction('rw', db.paymentMethods, async () => {
  //   const pm = await db.paymentMethods.get(paymentMethodId)
  //   if (!pm || pm.remainingBudget === undefined) return
  //   if (pm.remainingBudget < amount) throw new Error('잔액 부족')
  //   await db.paymentMethods.update(paymentMethodId, { remainingBudget: pm.remainingBudget - amount })
  // })
}

async recoverPoint(paymentMethodId: number, amount: number): Promise<void> {
  // ...잔액 복구
}
```

거래 수정 시 처리 순서:
1. 기존 포인트 거래면 `recoverPoint(oldAmount)`
2. `updateTransaction` 실행
3. 새 결제수단이 포인트이면 `deductPoint(newAmount)`

**검증 방법**
- DevTools > Application > IndexedDB에서 paymentMethods 테이블 잔액 확인
- Vitest 포인트 차감/복구 테스트 PASS

---

### Task 4: T36 카드 결제 현황 로컬 계산

**목표**: `usePaymentMethodStore`에 카드 청구 기간 계산 함수를 구현하고, `CardBillingSummary[]`를 제공한다.

**파일**
- 수정: `frontend/src/stores/paymentMethod.ts`
- 신규: `frontend/src/utils/cardBilling.ts`

**구현 상세**

`cardBilling.ts`:

```typescript
/**
 * 정산일 기준 이번 청구 기간 계산
 * - 정산일=15: 전월 16일 ~ 당월 15일
 * - 오늘이 정산일 이후이면 이번 달 청구 기간, 이전이면 전월 청구 기간
 */
export function getCurrentBillingPeriod(
  cutoffDay: number,
  today: string  // YYYY-MM-DD
): { periodFrom: string; periodTo: string }

/**
 * 결제일 D-day 계산
 */
export function calcDDay(dueDate: string, today: string): string
```

`usePaymentMethodStore`:
```typescript
// 현재 월 카드별 청구 현황 계산
function getCardBillingSummary(
  transactions: TransactionView[],
  today: string,
): CardBillingSummary[]
```

MockupView의 `cardBillingSummary computed` 로직을 그대로 이식하되, 하드코딩된 `CARD_CUTOFF_DAY=15`, `CARD_DUE_DAY=25` 대신 각 카드의 `billingCutoffDay`, `paymentDueDay`를 사용한다.

**정산일/결제일 미설정 카드**: `CardBillingSummary`에서 제외 (위젯에 표시 안 함)

**검증 방법**
- Vitest: `getCurrentBillingPeriod` 케이스별 단위 테스트
  - 오늘=3/16, cutoffDay=15 → periodFrom=2/16, periodTo=3/15
  - 오늘=3/10, cutoffDay=15 → periodFrom=1/16, periodTo=2/15

---

### Task 5: T37 반복 거래 자동 적용

**목표**: 화면 진입 또는 월 이동 시 `applyRecurringForMonth`를 호출하여 해당 월의 미생성 반복 거래를 자동 생성한다.

**파일**
- 수정: `frontend/src/stores/transaction.ts`
- 수정: `frontend/src/views/HomeView.vue` (watch currentMonth)

**구현 상세**

`useTransactionStore.loadTransactions(year, month)`:
```
1. applyRecurringForMonth(year, month, monthStartDay)  ← 먼저 자동 생성
2. Dexie 범위 쿼리로 거래 조회
3. 카테고리/결제수단 이름 조인
4. transactions ref 갱신
```

HomeView의 `watch`:
```typescript
watch([currentYear, currentMonth], async ([y, m]) => {
  await transactionStore.loadTransactions(y, m)
}, { immediate: true })
```

**중복 방지**: `applyRecurringForMonth` 내부에서 `getRecurringPending`으로 미생성 항목만 처리하므로 멱등성 보장됨.

**검증 방법**
- 반복 원부 등록 후 다음 달로 이동 시 거래가 자동 생성되는가
- 같은 달에 여러 번 이동해도 중복 생성되지 않는가

---

### Task 6: T48 HomeView — Dexie 연동

**목표**: 기존 API 호출 기반 HomeView.vue를 MockupView 홈 탭 UI를 기준으로 전면 재작성한다.

**파일**
- 수정: `frontend/src/views/HomeView.vue`
- 참조: `frontend/src/components/UpcomingWidget.vue` (재사용)
- 참조: `frontend/src/components/TransactionModal.vue` (수정 필요)
- 참조: `frontend/src/components/DeleteOptionSheet.vue` (재사용)

**구현 상세**

HomeView 구조 (MockupView 홈 탭과 동일):
```
HomeView
├── 월 헤더 (← 월 이동 →, 수입/지출/저축/잔액 요약)
├── 예정된 지출 위젯 (반복 예정 배너 + UpcomingWidget 카드 결제)
├── 검색/필터 아코디언 (키워드 + 유형 탭 + 카테고리 칩)
├── 거래 목록 (날짜별 그룹, 일별 서머리)
└── + 버튼 (거래 추가 모달)
```

store 연동:
```typescript
const settingStore   = useSettingsStore()
const txStore        = useTransactionStore()
const categoryStore  = useCategoryStore()
const pmStore        = usePaymentMethodStore()

// 로드
onMounted(async () => {
  await settingStore.loadSettings()
  await categoryStore.loadCategories()
  await pmStore.loadPaymentMethods()
  await pmStore.loadSavingsMethods()
  await txStore.loadTransactions(settingStore.currentYear, settingStore.currentMonth)
})
```

`TransactionModal.vue` 수정 필요 사항:
- props로 `categories`, `paymentMethods`, `savingsMethods` 받음
- emit `save` 이벤트 → HomeView에서 `txStore.createSingle / createInstallment / createRecurring` 호출
- 수정 모드: `emit('update', ...)` → `txStore.updateTransaction / updateInstallmentMaster / updateRecurringMaster` 호출

**다크모드 적용**:
- MockupView의 `bg-gray-900` 기반 다크 팔레트를 HomeView에 동일하게 적용
- 기존 라이트모드 Tailwind 클래스 전부 교체

**검증 방법**
- 거래 추가 → 목록에 즉시 표시
- 월 이동 → 해당 월 거래만 표시, 요약 값 갱신
- 검색/필터 동작

---

### Task 7: T46 + T45 SettingsView — Dexie 연동

**목표**: 기존 API 호출 기반 SettingsView.vue를 MockupView 설정 탭 UI를 기준으로 전면 재작성한다.

**파일**
- 수정: `frontend/src/views/SettingsView.vue`

**구현 상세**

SettingsView 구조 (MockupView 설정 탭과 동일):
```
SettingsView
├── 월 시작일 설정 (select 1~28일 → settingStore.saveMonthStartDay)
├── 카테고리 관리
│   ├── 수입/지출/저축 탭
│   ├── 카테고리 칩 목록 (기본값 삭제 불가)
│   └── 추가 폼
├── 결제수단 관리
│   ├── 결제수단 목록 (타입별 아이콘)
│   ├── 카드 행 클릭 → 인라인 청구 설정 (정산일/결제일)
│   ├── 포인트 행 클릭 → 인라인 예산 설정 (총액/잔액)
│   └── 추가 폼
└── 저축 수단 관리
    ├── 저축 수단 목록
    └── 추가 폼
```

store 연동:
- `useCategoryStore` — `addCategory`, `deleteCategory`
- `usePaymentMethodStore` — `addPaymentMethod`, `updatePaymentMethod`, `deletePaymentMethod`, `updateBillingSettings`, `updatePointBudget`
- `useSettingsStore` — `saveMonthStartDay`

**다크모드 적용**: MockupView 설정 탭과 동일한 `bg-gray-900` 기반 팔레트

**카테고리 삭제 제약**:
- `isDefault=true` 항목 삭제 시 `showAlert('기본 카테고리는 삭제할 수 없습니다.')`
- 연결 거래 존재 여부 확인: `db.transactions.where('categoryId').equals(id).count() > 0` → `showAlert('연결된 거래가 있어 삭제할 수 없습니다.')`

**결제수단 삭제 제약**:
- 연결 거래 존재 시 삭제 불가 (동일 패턴)
- 포인트 결제수단 삭제 시 연결된 포인트 예산도 함께 삭제

---

### Task 8: T49 StatsView — Dexie 연동

**목표**: 기존 API 호출 기반 StatsView.vue를 MockupView 통계 탭 UI를 기준으로 전면 재작성한다.

**파일**
- 수정: `frontend/src/views/StatsView.vue`

**구현 상세**

StatsView 구조 (MockupView 통계 탭과 동일):
```
StatsView
├── 월 헤더 (← 월 이동 →)
├── 수입/지출/저축 탭 (statsType)
├── 도넛 차트 (카테고리별 비율, 중앙 합계 텍스트)
├── 범례 목록 (카테고리명 + 금액 + %)
├── 전월 비교 4열 카드 (수입/지출/저축/잔액, 전월 대비 ±%)
├── 6개월 막대 추이 차트 (수입/지출/저축)
└── 카테고리별 6개월 추이 라인차트 (지출 전용, 점선)
```

로컬 계산 방식 (MockupView `statsCategoryData`, `statsTrendData`, `statsCatTrendData`와 동일):
- `getMonthPeriod`로 해당 월 범위 계산
- `db.transactions.where('date').between(start, end)` 조회
- 카테고리별 집계, 추이 계산

`useTransactionStore`에 통계 전용 메서드 추가:
```typescript
// 특정 월 모든 거래 조회 (통계 계산용 — 필터 없음)
async getTransactionsForMonth(year: number, month: number): Promise<TransactionView[]>

// 최근 N개월 월별 요약 조회 (추이 계산용)
async getMonthlyTrend(baseYear: number, baseMonth: number, months: number): Promise<MonthlySummary[]>
```

**차트 라이브러리**: Chart.js (이미 등록됨) — `BarController`, `BarElement`, `DoughnutController`, `ArcElement`, `LineController`, `LineElement` 필요

**검증 방법**
- 홈 탭에서 거래 추가 후 통계 탭 이동 시 해당 거래가 집계에 반영되는가
- 월 이동 시 차트가 해당 월 데이터로 재렌더링되는가

---

### Task 9: T51 PWA 아이콘 생성

**목표**: PWA manifest에 필요한 `icon-192.png`, `icon-512.png`를 생성하여 배포 시 설치 프롬프트가 동작하게 한다.

**파일**
- 신규: `frontend/public/icons/icon-192.png`
- 신규: `frontend/public/icons/icon-512.png`

**구현 상세**

단순 단색 배경(예: `#1F2937`, gray-800) + 중앙 텍스트("B" 또는 "가계부") PNG 파일을 생성한다.

생성 방법 (선택):
1. Canvas API로 직접 생성하는 스크립트 (`scripts/gen-icons.js`) 작성 후 `node scripts/gen-icons.js` 실행
2. 온라인 도구(favicon.io 등) 활용 후 파일 배치

`vite.config.ts`의 VitePWA 설정에서 `icons` 배열이 `public/icons/icon-192.png`, `public/icons/icon-512.png`를 참조하고 있는지 확인.

**검증 방법**
- `npm run build` 성공
- `dist/sw.js` 생성 확인
- Chrome DevTools > Application > Manifest에서 아이콘 표시 확인

---

### Task 10: App.vue — seedDatabase + store 초기화 통합

**목표**: App.vue에서 앱 최초 실행 시 시드 데이터 생성 및 마스터 데이터 초기 로드를 처리한다.

**파일**
- 수정: `frontend/src/App.vue`

**구현 상세**

```typescript
// App.vue
import { seedDatabase } from '@/database/db'
import { useSettingsStore } from '@/stores/app'

const settingStore = useSettingsStore()

onMounted(async () => {
  await seedDatabase()              // 최초 실행 시에만 시드 데이터 생성 (멱등)
  await settingStore.loadSettings() // monthStartDay 로드
})
```

기존 `store.loadMasterData()` 호출 제거 (구 API 호출 코드).

`frontend/src/api/` 디렉토리: Sprint 9 완료 후 사용되지 않으므로 별도 정리(스코프 아웃).

---

### Task 11: Vitest 테스트 + `npm run build` 최종 검증

**목표**: 핵심 로직에 대한 Vitest 단위 테스트를 작성하고 DoD를 충족한다.

**파일**
- 신규 또는 수정: `frontend/src/tests/` 하위 테스트 파일

**테스트 케이스 목록**

| 파일 | 테스트 대상 |
|------|------------|
| `transaction.test.ts` | 단건/할부/반복 CRUD, 포인트 차감/복구 |
| `cardBilling.test.ts` | `getCurrentBillingPeriod` 케이스별 (정산일 전/후, 월말 처리) |
| `monthPeriod.test.ts` | `getMonthPeriod` — 기존 테스트 유지 |
| `store.settings.test.ts` | monthStartDay 저장/조회 |

**실행 명령**:
```bash
cd C:/Project/claude/frontend
npx vitest run --reporter=verbose
npm run build
```

**합격 기준**: 모든 케이스 PASS, TypeScript 오류 0건

---

## 의존성 및 리스크

| 리스크 | 대응 방안 |
|--------|----------|
| `TransactionModal.vue`가 구 API 타입에 강하게 결합됨 | Task 6에서 props/emit 인터페이스를 Dexie 타입으로 전면 교체 |
| 기존 HomeView/StatsView/SettingsView에 API import가 다수 존재 | 각 View 파일을 신규 작성 수준으로 재작성 (기존 코드 참조 후 교체) |
| `PaymentMethodType`이 db.ts(`CreditCard/DebitCard`)와 types/index.ts(`Card`)가 불일치 | Task 1에서 types/index.ts 재작성 시 `CreditCard/DebitCard`로 통일 |
| Chart.js 컴포넌트 재등록 충돌 | StatsView에서 `Chart.register()`를 조건부로 처리하거나 전역 등록으로 이동 |

---

## 완료 기준 (Definition of Done)

- ⬜ T34: Pinia stores 4개 구현 완료 (useSettingsStore, useCategoryStore, usePaymentMethodStore, useTransactionStore)
- ⬜ T35: 거래 CRUD 완료 (단건/할부/반복 생성·수정·삭제)
- ⬜ T36: 카드 결제 현황 로컬 계산 완료
- ⬜ T37: 반복 거래 자동 적용 완료
- ⬜ T45: 저축 수단 CRUD 완료
- ⬜ T46: 설정 CRUD 완료 (monthStartDay, 카테고리, 결제수단)
- ⬜ T47: 포인트 잔액 자동 차감/복구 완료
- ⬜ T48: HomeView Dexie 연동 완료 (다크모드)
- ⬜ T49: StatsView Dexie 연동 완료 (다크모드)
- ⬜ T50: SettingsView Dexie 연동 완료 (다크모드)
- ⬜ T51: PWA 아이콘 생성 완료
- ⬜ Vitest 전체 케이스 PASS
- ⬜ `npm run build` TypeScript 오류 0건
- ⬜ `sprint9 → develop` PR 생성

---

## 예상 산출물

| 파일 | 변경 유형 | 설명 |
|------|----------|------|
| `frontend/src/types/index.ts` | 전면 재작성 | Dexie 스키마 기반 타입 통일 |
| `frontend/src/stores/app.ts` | 재작성 | useSettingsStore (API 호출 제거, Dexie 연동) |
| `frontend/src/stores/category.ts` | 신규 | useCategoryStore |
| `frontend/src/stores/paymentMethod.ts` | 신규 | usePaymentMethodStore (카드/포인트/저축 수단) |
| `frontend/src/stores/transaction.ts` | 신규 | useTransactionStore |
| `frontend/src/utils/cardBilling.ts` | 신규 | 카드 청구 기간 계산 유틸 |
| `frontend/src/views/HomeView.vue` | 전면 재작성 | 다크모드 + Dexie 연동 |
| `frontend/src/views/StatsView.vue` | 전면 재작성 | 다크모드 + Dexie 연동 |
| `frontend/src/views/SettingsView.vue` | 전면 재작성 | 다크모드 + Dexie 연동 |
| `frontend/src/components/TransactionModal.vue` | 수정 | Dexie 타입으로 props/emit 교체 |
| `frontend/public/icons/icon-192.png` | 신규 | PWA 아이콘 |
| `frontend/public/icons/icon-512.png` | 신규 | PWA 아이콘 |
| `frontend/src/tests/transaction.test.ts` | 신규 | CRUD + 포인트 Vitest |
| `frontend/src/tests/cardBilling.test.ts` | 신규 | 카드 청구 계산 Vitest |

---

## 참고: 구현 시 MockupView 참조 위치

| 구현 대상 | MockupView.vue 참조 줄 범위 |
|----------|--------------------------|
| 거래 폼 (createTx, updateTx) | 419~870 |
| 삭제 로직 (할부/반복 3가지 모드) | 800~870 |
| 반복 예정 배너 로직 | 273~414 |
| 카드 청구 계산 (cardBillingSummary) | 279~340 |
| 통계 계산 (category, trend, catTrend) | 1032~1282 |
| 설정 탭 CRUD (category, method, savings) | 949~1031 |
| 다크모드 UI (홈 탭 템플릿) | 1285~1700 (대략) |
| 다크모드 UI (통계/설정 탭 템플릿) | 1700~2238 (대략) |

---

## 스프린트 회고

> 완료 후 작성 예정
