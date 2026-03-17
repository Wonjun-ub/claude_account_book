<script setup lang="ts">
// DEV-only 목업 페이지 — 백엔드 미접근, mock 데이터로만 동작
// 실제 앱과 동일한 UI 구조로 신규 기능 UI를 확인한 후 실서비스에 이관합니다.

import { ref, computed, watch, onMounted, onUnmounted, nextTick } from 'vue'
import {
  Chart, ArcElement, DoughnutController, Tooltip, Legend,
  CategoryScale, LinearScale, BarElement, BarController,
  LineElement, PointElement, LineController,
} from 'chart.js'

Chart.register(
  ArcElement, DoughnutController, Tooltip, Legend,
  CategoryScale, LinearScale, BarElement, BarController,
  LineElement, PointElement, LineController,
)
import dayjs from 'dayjs'
import { calcMockInstallment } from '@/mocks/installment.mock'
import { getMonthPeriod } from '@/utils/monthPeriod'
import UpcomingWidget from '@/components/UpcomingWidget.vue'

// UpcomingWidget과 공유하는 타입 — SFC export 호환성 문제로 여기서 직접 정의
interface CardFilter {
  id: number
  name: string
  periodFrom: string
  periodTo: string
}
interface CardBillingSummary extends CardFilter {
  dueDay: number
  dueDate: string
  amount: number
}

const activePage = ref('home')

// ── 인터페이스 ──────────────────────────────────────────────────────────────

interface MockTxRecord {
  id: number
  amount: number
  date: string
  type: 'Income' | 'Expense' | 'Savings'
  categoryId: number
  categoryName: string
  paymentMethodId: number
  paymentMethodName: string
  memo: string | undefined
  isIncludedInTotal: boolean
  installmentMasterId: number | undefined   // 할부 원부 FK
  installmentSequence: number | undefined   // 할부 회차 (1-based)
  recurringMasterId: number | undefined     // 반복 원부 FK
}

interface MockInstallmentMaster {
  id: number
  totalAmount: number
  monthlyAmount: number
  firstMonthAmount: number
  totalInstallments: number
  startDate: string
  categoryId: number
  categoryName: string
  paymentMethodId: number
  paymentMethodName: string
  memo: string | undefined
}

interface MockRecurringMaster {
  id: number
  amount: number
  type: 'Income' | 'Expense' | 'Savings'
  dayOfMonth: number        // 1~28
  startDate: string
  endDate: string | undefined
  isActive: boolean
  categoryId: number
  categoryName: string
  paymentMethodId: number
  paymentMethodName: string
  memo: string | undefined
}

// ── 거래 유형 판별 ────────────────────────────────────────────────────────────

function txKind(tx: MockTxRecord): 'Single' | 'Installment' | 'Recurring' {
  if (tx.installmentMasterId !== undefined) return 'Installment'
  if (tx.recurringMasterId !== undefined) return 'Recurring'
  return 'Single'
}

// ── Mock 초기 데이터 ─────────────────────────────────────────────────────────

const MOCK_CATEGORIES = [
  { id: 1, name: '식비',   type: 'Expense'  as const, isDefault: true },
  { id: 2, name: '교통',   type: 'Expense'  as const, isDefault: true },
  { id: 3, name: '쇼핑',   type: 'Expense'  as const, isDefault: false },
  { id: 4, name: '의료',   type: 'Expense'  as const, isDefault: false },
  { id: 5, name: '급여',   type: 'Income'   as const, isDefault: true },
  { id: 6, name: '부업',   type: 'Income'   as const, isDefault: false },
  { id: 7, name: '청약',   type: 'Savings'  as const, isDefault: true },
  { id: 8, name: '적금',   type: 'Savings'  as const, isDefault: true },
  { id: 9, name: '비상금', type: 'Savings'  as const, isDefault: true },
]

const MOCK_PAYMENT_METHODS: { id: number; name: string; type: 'Cash' | 'CreditCard' | 'DebitCard' | 'Point'; remainingAmount: number | undefined }[] = [
  { id: 1, name: '신한카드',   type: 'CreditCard', remainingAmount: undefined },
  { id: 2, name: '현금',       type: 'Cash',       remainingAmount: undefined },
  { id: 3, name: '네이버페이', type: 'Point',      remainingAmount: 45000    },
  { id: 4, name: '국민카드',   type: 'CreditCard', remainingAmount: undefined },
]

const MOCK_SAVINGS_METHODS = [
  { id: 101, name: '기업은행' },
  { id: 102, name: '카카오뱅크' },
  { id: 103, name: '현금' },
]

// recurringMasterId:1 = 급여 (매월 10일)
// installmentMasterId:1 = 의류 할부 3개월 (300,000원, 3~5월)
const MOCK_TRANSACTIONS_INITIAL: MockTxRecord[] = [
  // 2025-12
  { id: 101, amount: 2800000, date: '2025-12-10', type: 'Income',  categoryId: 5, categoryName: '급여',  paymentMethodId: 2, paymentMethodName: '현금',       memo: '12월 급여',  isIncludedInTotal: true,  installmentMasterId: undefined, installmentSequence: undefined, recurringMasterId: 1 },
  { id: 102, amount: 120000,  date: '2025-12-20', type: 'Expense', categoryId: 3, categoryName: '쇼핑',  paymentMethodId: 1, paymentMethodName: '신한카드',   memo: '크리스마스', isIncludedInTotal: true,  installmentMasterId: undefined, installmentSequence: undefined, recurringMasterId: undefined },
  { id: 103, amount: 45000,   date: '2025-12-25', type: 'Expense', categoryId: 1, categoryName: '식비',  paymentMethodId: 2, paymentMethodName: '현금',       memo: '연말 모임',  isIncludedInTotal: true,  installmentMasterId: undefined, installmentSequence: undefined, recurringMasterId: undefined },
  { id: 104, amount: 15000,   date: '2025-12-28', type: 'Expense', categoryId: 2, categoryName: '교통',  paymentMethodId: 1, paymentMethodName: '신한카드',   memo: undefined,    isIncludedInTotal: true,  installmentMasterId: undefined, installmentSequence: undefined, recurringMasterId: undefined },
  // 2026-01
  { id: 201, amount: 3000000, date: '2026-01-10', type: 'Income',  categoryId: 5, categoryName: '급여',  paymentMethodId: 2, paymentMethodName: '현금',       memo: '1월 급여',   isIncludedInTotal: true,  installmentMasterId: undefined, installmentSequence: undefined, recurringMasterId: 1 },
  { id: 202, amount: 55000,   date: '2026-01-12', type: 'Expense', categoryId: 1, categoryName: '식비',  paymentMethodId: 1, paymentMethodName: '신한카드',   memo: '회식',       isIncludedInTotal: true,  installmentMasterId: undefined, installmentSequence: undefined, recurringMasterId: undefined },
  { id: 203, amount: 30000,   date: '2026-01-18', type: 'Expense', categoryId: 4, categoryName: '의료',  paymentMethodId: 2, paymentMethodName: '현금',       memo: '병원',       isIncludedInTotal: true,  installmentMasterId: undefined, installmentSequence: undefined, recurringMasterId: undefined },
  { id: 204, amount: 150000,  date: '2026-01-22', type: 'Income',  categoryId: 6, categoryName: '부업',  paymentMethodId: 2, paymentMethodName: '현금',       memo: '프리랜서',   isIncludedInTotal: false, installmentMasterId: undefined, installmentSequence: undefined, recurringMasterId: undefined },
  // 2026-02
  { id: 301, amount: 3000000, date: '2026-02-10', type: 'Income',  categoryId: 5, categoryName: '급여',  paymentMethodId: 2, paymentMethodName: '현금',       memo: '2월 급여',   isIncludedInTotal: true,  installmentMasterId: undefined, installmentSequence: undefined, recurringMasterId: 1 },
  { id: 302, amount: 68000,   date: '2026-02-14', type: 'Expense', categoryId: 1, categoryName: '식비',  paymentMethodId: 1, paymentMethodName: '신한카드',   memo: '발렌타인',   isIncludedInTotal: true,  installmentMasterId: undefined, installmentSequence: undefined, recurringMasterId: undefined },
  { id: 303, amount: 200000,  date: '2026-02-20', type: 'Expense', categoryId: 3, categoryName: '쇼핑',  paymentMethodId: 3, paymentMethodName: '네이버페이', memo: '봄옷',       isIncludedInTotal: true,  installmentMasterId: undefined, installmentSequence: undefined, recurringMasterId: undefined },
  // 2026-03
  { id: 1,   amount: 50000,   date: '2026-03-15', type: 'Expense', categoryId: 1, categoryName: '식비',  paymentMethodId: 1, paymentMethodName: '신한카드',   memo: '회식',       isIncludedInTotal: true,  installmentMasterId: undefined, installmentSequence: undefined, recurringMasterId: undefined },
  { id: 2,   amount: 3000000, date: '2026-03-10', type: 'Income',  categoryId: 5, categoryName: '급여',  paymentMethodId: 2, paymentMethodName: '현금',       memo: '3월 급여',   isIncludedInTotal: true,  installmentMasterId: undefined, installmentSequence: undefined, recurringMasterId: 1 },
  { id: 3,   amount: 12500,   date: '2026-03-10', type: 'Expense', categoryId: 2, categoryName: '교통',  paymentMethodId: 1, paymentMethodName: '신한카드',   memo: undefined,    isIncludedInTotal: true,  installmentMasterId: undefined, installmentSequence: undefined, recurringMasterId: undefined },
  { id: 4,   amount: 100000,  date: '2026-03-08', type: 'Expense', categoryId: 3, categoryName: '쇼핑',  paymentMethodId: 1, paymentMethodName: '신한카드',   memo: '의류 1/3',   isIncludedInTotal: true,  installmentMasterId: 1,         installmentSequence: 1,        recurringMasterId: undefined },
  { id: 5,   amount: 200000,  date: '2026-03-05', type: 'Income',  categoryId: 6, categoryName: '부업',  paymentMethodId: 2, paymentMethodName: '현금',       memo: '프리랜서',   isIncludedInTotal: false, installmentMasterId: undefined, installmentSequence: undefined, recurringMasterId: undefined },
  { id: 6,   amount: 35000,   date: '2026-03-03', type: 'Expense', categoryId: 1, categoryName: '식비',  paymentMethodId: 2, paymentMethodName: '현금',       memo: '장보기',     isIncludedInTotal: true,  installmentMasterId: undefined, installmentSequence: undefined, recurringMasterId: undefined },
  // 신한카드 청구 기간 02/16~03/15 내 추가 거래 (기존 162,500원 + 124,500원 = 287,000원 합산)
  { id: 10,  amount: 45000,   date: '2026-02-18', type: 'Expense', categoryId: 1, categoryName: '식비',  paymentMethodId: 1, paymentMethodName: '신한카드',   memo: '외식',       isIncludedInTotal: true,  installmentMasterId: undefined, installmentSequence: undefined, recurringMasterId: undefined },
  { id: 11,  amount: 38500,   date: '2026-02-22', type: 'Expense', categoryId: 2, categoryName: '교통',  paymentMethodId: 1, paymentMethodName: '신한카드',   memo: '주유',       isIncludedInTotal: true,  installmentMasterId: undefined, installmentSequence: undefined, recurringMasterId: undefined },
  { id: 12,  amount: 41000,   date: '2026-03-02', type: 'Expense', categoryId: 4, categoryName: '의료',  paymentMethodId: 1, paymentMethodName: '신한카드',   memo: '약국',       isIncludedInTotal: true,  installmentMasterId: undefined, installmentSequence: undefined, recurringMasterId: undefined },
  // 국민카드 청구 기간 02/16~03/15 내 거래 (합산 142,000원)
  { id: 7,   amount: 89000,   date: '2026-03-12', type: 'Expense', categoryId: 3, categoryName: '쇼핑',  paymentMethodId: 4, paymentMethodName: '국민카드',   memo: '쿠팡',       isIncludedInTotal: true,  installmentMasterId: undefined, installmentSequence: undefined, recurringMasterId: undefined },
  { id: 8,   amount: 32000,   date: '2026-03-07', type: 'Expense', categoryId: 1, categoryName: '식비',  paymentMethodId: 4, paymentMethodName: '국민카드',   memo: '편의점',     isIncludedInTotal: true,  installmentMasterId: undefined, installmentSequence: undefined, recurringMasterId: undefined },
  { id: 9,   amount: 21000,   date: '2026-02-25', type: 'Expense', categoryId: 2, categoryName: '교통',  paymentMethodId: 4, paymentMethodName: '국민카드',   memo: 'KTX',        isIncludedInTotal: true,  installmentMasterId: undefined, installmentSequence: undefined, recurringMasterId: undefined },
  // 2026-03 저축 거래 (청약/적금/비상금)
  { id: 20, amount: 500000, date: '2026-03-10', type: 'Savings', categoryId: 8, categoryName: '적금',   paymentMethodId: 103, paymentMethodName: '현금', memo: '정기적금',  isIncludedInTotal: true, installmentMasterId: undefined, installmentSequence: undefined, recurringMasterId: 3 },
  { id: 21, amount: 200000, date: '2026-03-10', type: 'Savings', categoryId: 7, categoryName: '청약',   paymentMethodId: 103, paymentMethodName: '현금', memo: '주택청약',  isIncludedInTotal: true, installmentMasterId: undefined, installmentSequence: undefined, recurringMasterId: 2 },
  { id: 22, amount: 100000, date: '2026-03-05', type: 'Savings', categoryId: 9, categoryName: '비상금', paymentMethodId: 103, paymentMethodName: '현금', memo: '비상금통장', isIncludedInTotal: true, installmentMasterId: undefined, installmentSequence: undefined, recurringMasterId: undefined },
  // 2026-04 (급여 반복 거래는 4/10 미도래 — pending 배너로 노출)
  { id: 402, amount: 42000,   date: '2026-04-05', type: 'Expense', categoryId: 1, categoryName: '식비',  paymentMethodId: 1, paymentMethodName: '신한카드',   memo: undefined,    isIncludedInTotal: true,  installmentMasterId: undefined, installmentSequence: undefined, recurringMasterId: undefined },
  { id: 403, amount: 100000,  date: '2026-04-08', type: 'Expense', categoryId: 3, categoryName: '쇼핑',  paymentMethodId: 1, paymentMethodName: '신한카드',   memo: '의류 2/3',   isIncludedInTotal: true,  installmentMasterId: 1,         installmentSequence: 2,        recurringMasterId: undefined },
  // 2026-05 (급여 반복 거래는 5/10 미도래 — pending 배너로 노출)
  { id: 502, amount: 300000,  date: '2026-05-05', type: 'Expense', categoryId: 4, categoryName: '의료',  paymentMethodId: 1, paymentMethodName: '신한카드',   memo: '건강검진',   isIncludedInTotal: true,  installmentMasterId: undefined, installmentSequence: undefined, recurringMasterId: undefined },
  { id: 503, amount: 100000,  date: '2026-05-08', type: 'Expense', categoryId: 3, categoryName: '쇼핑',  paymentMethodId: 1, paymentMethodName: '신한카드',   memo: '의류 3/3',   isIncludedInTotal: true,  installmentMasterId: 1,         installmentSequence: 3,        recurringMasterId: undefined },
]

const MOCK_INSTALLMENT_MASTERS_INITIAL: MockInstallmentMaster[] = [
  { id: 1, totalAmount: 300000, monthlyAmount: 100000, firstMonthAmount: 100000, totalInstallments: 3, startDate: '2026-03-08', categoryId: 3, categoryName: '쇼핑', paymentMethodId: 1, paymentMethodName: '신한카드', memo: '의류 할부' },
]

const MOCK_RECURRING_MASTERS_INITIAL: MockRecurringMaster[] = [
  { id: 1, amount: 3000000, type: 'Income',  dayOfMonth: 10, startDate: '2025-12-10', endDate: undefined, isActive: true, categoryId: 5, categoryName: '급여', paymentMethodId: 2,   paymentMethodName: '현금', memo: '월급' },
  { id: 2, amount: 200000,  type: 'Savings', dayOfMonth: 10, startDate: '2025-12-10', endDate: undefined, isActive: true, categoryId: 7, categoryName: '청약', paymentMethodId: 103, paymentMethodName: '현금', memo: '주택청약' },
  { id: 3, amount: 500000,  type: 'Savings', dayOfMonth: 10, startDate: '2025-12-10', endDate: undefined, isActive: true, categoryId: 8, categoryName: '적금', paymentMethodId: 103, paymentMethodName: '현금', memo: '정기적금' },
]

// ── 월 탐색 ─────────────────────────────────────────────────────────────────

const MOCK_NAV_MIN = { year: 2025, month: 12 }
const MOCK_NAV_MAX = { year: 2026, month: 5 }
const mockYear  = ref(2026)
const mockMonth = ref(3)

const mockMonthLabel = computed(() => `${mockYear.value}년 ${mockMonth.value}월`)

const mockMonthRange = computed(() => {
  if (settingMonthStartDay.value === 1) return null
  const { start, end } = getMonthPeriod(mockYear.value, mockMonth.value, settingMonthStartDay.value)
  return `${dayjs(start).format('M/D')} ~ ${dayjs(end).format('M/D')}`
})

function prevMonth() {
  if (mockYear.value === MOCK_NAV_MIN.year && mockMonth.value === MOCK_NAV_MIN.month) return
  if (mockMonth.value === 1) { mockYear.value--; mockMonth.value = 12 }
  else mockMonth.value--
  txSelectedType.value = null
  txSelectedCategory.value = null
}
function nextMonth() {
  if (mockYear.value === MOCK_NAV_MAX.year && mockMonth.value === MOCK_NAV_MAX.month) return
  if (mockMonth.value === 12) { mockYear.value++; mockMonth.value = 1 }
  else mockMonth.value++
  txSelectedType.value = null
  txSelectedCategory.value = null
}
const canGoPrev = computed(() => !(mockYear.value === MOCK_NAV_MIN.year && mockMonth.value === MOCK_NAV_MIN.month))
const canGoNext = computed(() => !(mockYear.value === MOCK_NAV_MAX.year && mockMonth.value === MOCK_NAV_MAX.month))

// ── localStorage 영속성 ───────────────────────────────────────────────────────

// 초기 데이터 변경 시 이 버전을 올리면 localStorage가 자동 초기화됩니다
const MOCK_DATA_VERSION = 'v11'

const LS_VER  = 'mockup_version'
const LS_TX   = 'mockup_transactions'
const LS_INST = 'mockup_installment_masters'
const LS_RECR = 'mockup_recurring_masters'
const LS_SKIP = 'mockup_recurring_skipped'  // 반복 월별 스킵 키: `{masterId}-{YYYY}-{MM}`

function saveAll() {
  localStorage.setItem(LS_VER,  MOCK_DATA_VERSION)
  localStorage.setItem(LS_TX,   JSON.stringify(txTransactions.value))
  localStorage.setItem(LS_INST, JSON.stringify(mockInstallmentMasters.value))
  localStorage.setItem(LS_RECR, JSON.stringify(mockRecurringMasters.value))
  localStorage.setItem(LS_SKIP, JSON.stringify(recurSkippedSet.value))
}

// ── 상태 ─────────────────────────────────────────────────────────────────────

const txTransactions        = ref<MockTxRecord[]>([...MOCK_TRANSACTIONS_INITIAL])
const mockInstallmentMasters = ref<MockInstallmentMaster[]>([...MOCK_INSTALLMENT_MASTERS_INITIAL])
const mockRecurringMasters   = ref<MockRecurringMaster[]>([...MOCK_RECURRING_MASTERS_INITIAL])
const recurSkippedSet        = ref<string[]>([])  // 단건 삭제된 반복의 월별 스킵 키 목록

onMounted(() => {
  // 버전 불일치 시 초기 데이터로 리셋 후 즉시 저장 (ref 초기값 = MOCK_*_INITIAL)
  if (localStorage.getItem(LS_VER) !== MOCK_DATA_VERSION) {
    saveAll()
    return
  }
  try {
    const t = localStorage.getItem(LS_TX);   if (t) txTransactions.value        = JSON.parse(t)
    const i = localStorage.getItem(LS_INST); if (i) mockInstallmentMasters.value = JSON.parse(i)
    const r = localStorage.getItem(LS_RECR); if (r) mockRecurringMasters.value   = JSON.parse(r)
    const s = localStorage.getItem(LS_SKIP); if (s) recurSkippedSet.value        = JSON.parse(s)
  } catch { /* 손상 시 기본값 유지 */ }
})

// ── 홈: 거래 목록 ─────────────────────────────────────────────────────────────

const txSearch           = ref('')
const txSelectedType     = ref<'Income' | 'Expense' | 'Savings' | null>(null)
const txSelectedCategory = ref<number | null>(null)
const showModal          = ref(false)


const txMonthFiltered = computed(() => {
  const { start, end } = getMonthPeriod(mockYear.value, mockMonth.value, settingMonthStartDay.value)
  return txTransactions.value.filter(t => t.date >= start && t.date <= end)
})

const mockSummary = computed(() => {
  const included      = txMonthFiltered.value.filter(t => t.isIncludedInTotal)
  const incomeList    = included.filter(t => t.type === 'Income')
  const expenseList   = included.filter(t => t.type === 'Expense')
  const savingsList   = included.filter(t => t.type === 'Savings')
  const totalIncome   = incomeList.reduce((s, t) => s + t.amount, 0)
  const totalExpense  = expenseList.reduce((s, t) => s + t.amount, 0)
  const totalSavings  = savingsList.reduce((s, t) => s + t.amount, 0)
  return {
    totalIncome, totalExpense, totalSavings,
    balance: totalIncome - totalExpense - totalSavings,
    incomeCount: incomeList.length, expenseCount: expenseList.length,
  }
})

// ── 반복 예정 배너 ────────────────────────────────────────────────────────────

const showPendingBanner = ref(false)
const showFilterPanel   = ref(false)
const cardBillingModal = ref<CardFilter | null>(null)

// 카드 결제 예정 동적 계산 — 정산일=15, 결제일=25 하드코딩 (Step 3에서 API로 교체)
// 조회 월에 결제일이 있는 슬롯: 청구 기간 = 전월 16일 ~ 당월 15일
const CARD_CUTOFF_DAY = 15
const CARD_DUE_DAY = 25
const cardPaymentMethods = MOCK_PAYMENT_METHODS.filter(m => m.type === 'CreditCard' || m.type === 'DebitCard')

const cardBillingSummary = computed((): CardBillingSummary[] => {
  const periodTo   = dayjs(`${mockYear.value}-${String(mockMonth.value).padStart(2, '0')}-${String(CARD_CUTOFF_DAY).padStart(2, '0')}`)
  const periodFrom = periodTo.subtract(1, 'month').add(1, 'day')
  const dueDate    = dayjs(`${mockYear.value}-${String(mockMonth.value).padStart(2, '0')}-${String(CARD_DUE_DAY).padStart(2, '0')}`)
  const fromStr = periodFrom.format('YYYY-MM-DD')
  const toStr   = periodTo.format('YYYY-MM-DD')
  const dueStr  = dueDate.format('YYYY-MM-DD')

  return cardPaymentMethods
    .map(card => {
      const txs = txTransactions.value.filter(t =>
        t.paymentMethodId === card.id &&
        t.type === 'Expense' &&
        t.date >= fromStr &&
        t.date <= toStr
      )
      return {
        id: card.id,
        name: card.name,
        dueDay: CARD_DUE_DAY,
        dueDate: dueStr,
        periodFrom: fromStr,
        periodTo: toStr,
        amount: txs.reduce((s, t) => s + t.amount, 0),
      }
    })
    .filter(c => c.amount > 0)  // 거래 없는 카드는 숨김
})

const cardBillingTxList = computed(() => {
  if (!cardBillingModal.value) return []
  const { id, periodFrom, periodTo } = cardBillingModal.value
  return txTransactions.value
    .filter(t => t.paymentMethodId === id && t.date >= periodFrom && t.date <= periodTo)
})

const cardModalSelectedCategory = ref<number | null>(null)

const cardModalCategoryChips = computed(() => {
  const seen = new Set<number>()
  const result: { id: number; name: string }[] = []
  for (const tx of cardBillingTxList.value) {
    if (!seen.has(tx.categoryId)) {
      seen.add(tx.categoryId)
      result.push({ id: tx.categoryId, name: tx.categoryName })
    }
  }
  return result
})

const cardBillingTxListFiltered = computed(() => {
  if (cardModalSelectedCategory.value === null) return cardBillingTxList.value
  return cardBillingTxList.value.filter(t => t.categoryId === cardModalSelectedCategory.value)
})

watch(cardBillingModal, () => { cardModalSelectedCategory.value = null })

// 이번 월에 아직 등록되지 않은 반복 마스터 목록
const recurringPending = computed(() => {
  const { start, end } = getMonthPeriod(mockYear.value, mockMonth.value, settingMonthStartDay.value)
  const monthKey = (id: number) => `${id}-${mockYear.value}-${String(mockMonth.value).padStart(2, '0')}`
  return mockRecurringMasters.value.filter(master => {
    if (!master.isActive) return false
    if (master.startDate > end) return false
    if (master.endDate && master.endDate < start) return false
    if (recurSkippedSet.value.includes(monthKey(master.id))) return false
    // startDate가 이번 기간 내부(start 초과 ~ end 이하)에 있으면 등록 당기간 → pending 제외
    // start와 같으면(=period 첫날 시작) 이번 기간부터 pending으로 표시
    if (master.startDate > start && master.startDate <= end) return false
    return !txTransactions.value.some(t =>
      t.recurringMasterId === master.id && t.date >= start && t.date <= end
    )
  })
})

const pendingSummary = computed(() => {
  const income  = recurringPending.value.filter(m => m.type === 'Income').reduce((s, m) => s + m.amount, 0)
  const expense = recurringPending.value.filter(m => m.type === 'Expense').reduce((s, m) => s + m.amount, 0)
  const savings = recurringPending.value.filter(m => m.type === 'Savings').reduce((s, m) => s + m.amount, 0)
  return { income, expense, savings }
})

const txCategoryChips = computed(() => {
  const seen = new Set<number>()
  const result: { id: number; name: string; type: string }[] = []
  for (const tx of txMonthFiltered.value) {
    if (!seen.has(tx.categoryId)) {
      seen.add(tx.categoryId)
      result.push({ id: tx.categoryId, name: tx.categoryName, type: tx.type })
    }
  }
  return result
})

const txFiltered = computed(() => {
  let list = txMonthFiltered.value
  if (txSearch.value) list = list.filter(t => t.memo?.includes(txSearch.value) || t.categoryName.includes(txSearch.value))
  if (txSelectedType.value !== null) list = list.filter(t => t.type === txSelectedType.value)
  if (txSelectedCategory.value !== null) list = list.filter(t => t.categoryId === txSelectedCategory.value)
  return list
})

// 타입 선택 시 해당 타입의 카테고리 칩만 표시
const txTypeCategoryChips = computed(() => {
  if (txSelectedType.value === null) return []
  const seen = new Set<number>()
  const result: { id: number; name: string }[] = []
  for (const tx of txMonthFiltered.value) {
    if (tx.type !== txSelectedType.value) continue
    if (!seen.has(tx.categoryId)) {
      seen.add(tx.categoryId)
      result.push({ id: tx.categoryId, name: tx.categoryName })
    }
  }
  return result
})

// 타입 변경 시 카테고리 초기화
watch(txSelectedType, () => { txSelectedCategory.value = null })

function txGroupByDate(txs: MockTxRecord[]) {
  const map = new Map<string, MockTxRecord[]>()
  for (const tx of txs) {
    const key = tx.date
    if (!map.has(key)) map.set(key, [])
    map.get(key)!.push(tx)
  }
  return Array.from(map.entries()).sort((a, b) => b[0].localeCompare(a[0]))
}

function txFormatAmount(amount: number, type: string) {
  return `${type === 'Income' ? '+' : '-'}${amount.toLocaleString()}원`
}

// ── 거래 폼 ──────────────────────────────────────────────────────────────────

const editingTxId           = ref<number | null>(null)
const pendingMasterId       = ref<number | null>(null)
const formType              = ref<'Expense' | 'Income' | 'Savings'>('Expense')
const formAmount            = ref('')
const formDate              = ref(dayjs().format('YYYY-MM-DD'))
const formCategoryId        = ref(0)
const formPaymentMethodId   = ref(0)
const formSavingsMethodId   = ref(0)
const formMemo              = ref('')
const formIsIncluded        = ref(true)
const formSaved             = ref(false)
const formIsInstallment     = ref(false)
const formInstallmentMonths = ref<number | undefined>(undefined)
const formIsRecurring       = ref(false)
const formRecurringDay      = ref<number>(dayjs().date())
const formRecurringEndDate  = ref('')

const formCategories = computed(() => MOCK_CATEGORIES.filter(c => c.type === formType.value))

const formIsValid = computed(() => {
  const amount = parseInt(formAmount.value.replace(/,/g, ''), 10) || 0
  if (amount <= 0) return false
  if (formCategoryId.value === 0) return false
  if (formType.value === 'Expense' && formPaymentMethodId.value === 0) return false
  if (formType.value === 'Savings' && formSavingsMethodId.value === 0) return false
  return true
})

const installmentPreview = computed(() => {
  const total  = parseInt(formAmount.value.replace(/,/g, ''), 10) || 0
  const months = formInstallmentMonths.value
  if (!total || !months || months < 2) return null
  return calcMockInstallment(total, months)
})

// 탭 전환 시 타입별 카테고리 선택값 저장/복원
const savedCatByType: { Expense: number; Income: number; Savings: number } = { Expense: 0, Income: 0, Savings: 0 }
watch(formType, (newType, oldType) => {
  savedCatByType[oldType as 'Expense' | 'Income' | 'Savings'] = formCategoryId.value
  formCategoryId.value = savedCatByType[newType as 'Expense' | 'Income' | 'Savings']

  if (editingTxId.value === null) {
    // 신규 추가 모드: 수입/저축 전환 시 할부 상태 리셋
    if (newType === 'Income' || newType === 'Savings') {
      formIsInstallment.value = false
      formInstallmentMonths.value = undefined
    }
  } else if (formIsInstallment.value) {
    // 수정 모드 + 할부 거래: 타입 전환 시 금액 복원
    const tx = txTransactions.value.find(t => t.id === editingTxId.value)
    if (newType === 'Income' && tx) {
      // 수입으로 전환 → 실제 회차 금액으로 복원
      formAmount.value = tx.amount.toLocaleString()
    } else if (newType === 'Expense') {
      // 지출로 복귀 → 마스터 총금액으로 복원
      const master = tx?.installmentMasterId
        ? mockInstallmentMasters.value.find(m => m.id === tx.installmentMasterId)
        : undefined
      if (master) formAmount.value = master.totalAmount.toLocaleString()
    }
  }
})

// 날짜 변경 시 반복 일자 자동 동기화 (신규 추가 모드만)
watch(formDate, (newDate) => {
  if (formIsRecurring.value && editingTxId.value === null) {
    formRecurringDay.value = dayjs(newDate).date()
  }
})

function openAdd() {
  editingTxId.value = null
  pendingMasterId.value = null
  formType.value = 'Expense'
  formAmount.value = ''
  formDate.value = dayjs().format('YYYY-MM-DD')
  formCategoryId.value = 0
  formPaymentMethodId.value = 0
  formSavingsMethodId.value = 0
  formMemo.value = ''
  formIsIncluded.value = true
  formSaved.value = false
  formIsInstallment.value = false
  formInstallmentMonths.value = undefined
  formIsRecurring.value = false
  formRecurringDay.value = dayjs().date()
  formRecurringEndDate.value = ''
  savedCatByType.Expense = 0
  savedCatByType.Income = 0
  savedCatByType.Savings = 0
  showModal.value = true
}

function onFormAmountInput(e: Event) {
  const input = e.target as HTMLInputElement
  const digits = input.value.replace(/[^0-9]/g, '').slice(0, 9)
  const formatted = digits ? digits.replace(/\B(?=(\d{3})+(?!\d))/g, ',') : ''
  formAmount.value = formatted
  input.value = formatted
}

function formSaveMock() {
  const amount     = parseInt(formAmount.value.replace(/,/g, ''), 10) || 0
  const catName    = MOCK_CATEGORIES.find(c => c.id === formCategoryId.value)?.name ?? '기타'
  // 수입: 결제수단 없음 / 지출: 결제수단 / 저축: 저축 수단
  const methodId =
    formType.value === 'Expense' ? formPaymentMethodId.value :
    formType.value === 'Savings' ? formSavingsMethodId.value : 0
  const methodName =
    formType.value === 'Expense'
      ? (MOCK_PAYMENT_METHODS.find(m => m.id === formPaymentMethodId.value)?.name ?? '기타')
      : formType.value === 'Savings'
        ? (MOCK_SAVINGS_METHODS.find(m => m.id === formSavingsMethodId.value)?.name ?? '기타')
        : ''

  if (formIsRecurring.value && editingTxId.value === null) {
    // ── 반복 신규: 원부 생성 + 오늘 이전(또는 당일)인 경우에만 거래 1건 즉시 등록
    // 미래 날짜(startDate > today)는 거래를 생성하지 않고 pending에서 처리
    const masterId = Date.now()
    mockRecurringMasters.value.push({
      id: masterId,
      amount,
      type: formType.value,
      dayOfMonth: formRecurringDay.value,
      startDate: formDate.value,
      endDate: formRecurringEndDate.value || undefined,
      isActive: true,
      categoryId: formCategoryId.value,
      categoryName: catName,
      paymentMethodId: methodId,
      paymentMethodName: methodName,
      memo: formMemo.value || undefined,
    })
    const today = dayjs().format('YYYY-MM-DD')
    if (formDate.value <= today) {
      txTransactions.value.push({
        id: masterId + 1,
        amount,
        date: formDate.value,
        type: formType.value,
        categoryId: formCategoryId.value,
        categoryName: catName,
        paymentMethodId: methodId,
        paymentMethodName: methodName,
        memo: formMemo.value || undefined,
        isIncludedInTotal: formIsIncluded.value,
        installmentMasterId: undefined,
        installmentSequence: undefined,
        recurringMasterId: masterId,
      })
    }
  } else if (formIsInstallment.value && editingTxId.value === null) {
    // ── 할부 신규: 원부 생성 + N개 Transactions 즉시 생성
    const months   = formInstallmentMonths.value ?? 1
    const preview  = calcMockInstallment(amount, months)
    const masterId = Date.now()
    mockInstallmentMasters.value.push({
      id: masterId,
      totalAmount: amount,
      monthlyAmount: preview.monthlyAmount,
      firstMonthAmount: preview.firstMonthAmount,
      totalInstallments: months,
      startDate: formDate.value,
      categoryId: formCategoryId.value,
      categoryName: catName,
      paymentMethodId: formPaymentMethodId.value,
      paymentMethodName: methodName,
      memo: formMemo.value || undefined,
    })
    for (let seq = 1; seq <= months; seq++) {
      const d = dayjs(formDate.value).add(seq - 1, 'month')
      const day = Math.min(dayjs(formDate.value).date(), d.daysInMonth())
      txTransactions.value.push({
        id: masterId + seq,
        amount: seq === 1 ? preview.firstMonthAmount : preview.monthlyAmount,
        date: d.date(day).format('YYYY-MM-DD'),
        type: formType.value,
        categoryId: formCategoryId.value,
        categoryName: catName,
        paymentMethodId: formPaymentMethodId.value,
        paymentMethodName: methodName,
        memo: formMemo.value || undefined,
        isIncludedInTotal: formIsIncluded.value,
        installmentMasterId: masterId,
        installmentSequence: seq,
        recurringMasterId: undefined,
      })
    }
  } else if (editingTxId.value !== null) {
    // ── 수정
    const idx = txTransactions.value.findIndex(t => t.id === editingTxId.value)
    if (idx !== -1) {
      const existing = txTransactions.value[idx]!
      txTransactions.value[idx] = {
        ...existing,
        amount,
        date: formDate.value,
        type: formType.value,
        categoryId: formCategoryId.value,
        categoryName: catName,
        paymentMethodId: methodId,
        paymentMethodName: methodName,
        memo: formMemo.value || undefined,
        isIncludedInTotal: formIsIncluded.value,
      }
    }
  } else {
    // ── 단건 신규
    txTransactions.value.push({
      id: Date.now(),
      amount,
      date: formDate.value,
      type: formType.value,
      categoryId: formCategoryId.value,
      categoryName: catName,
      paymentMethodId: methodId,
      paymentMethodName: methodName,
      memo: formMemo.value || undefined,
      isIncludedInTotal: formIsIncluded.value,
      installmentMasterId: undefined,
      installmentSequence: undefined,
      recurringMasterId: pendingMasterId.value ?? undefined,
    })
  }

  pendingMasterId.value = null
  saveAll()
  formSaved.value = true
  setTimeout(() => { showModal.value = false; formSaved.value = false; editingTxId.value = null }, 1000)
}

// ── 거래 수정/삭제 ────────────────────────────────────────────────────────────

function openEdit(tx: MockTxRecord) {
  editingTxId.value = tx.id
  // formType 설정 전에 method를 먼저 세팅 — watch(formType) 발화 시 덮어쓰기 방지
  if (tx.type === 'Savings') {
    formSavingsMethodId.value = tx.paymentMethodId
    formPaymentMethodId.value = 0
  } else {
    formPaymentMethodId.value = tx.paymentMethodId
    formSavingsMethodId.value = 0
  }
  formType.value = tx.type
  formDate.value = tx.date
  formCategoryId.value = tx.categoryId
  formMemo.value = tx.memo ?? ''
  formIsIncluded.value = tx.isIncludedInTotal
  formSaved.value = false
  savedCatByType.Expense = tx.type === 'Expense' ? tx.categoryId : 0
  savedCatByType.Income  = tx.type === 'Income'  ? tx.categoryId : 0
  savedCatByType.Savings = tx.type === 'Savings' ? tx.categoryId : 0

  // 할부 거래 → 마스터에서 총금액/개월수 로드
  const instMaster = tx.installmentMasterId
    ? mockInstallmentMasters.value.find(m => m.id === tx.installmentMasterId)
    : undefined
  if (instMaster) {
    formIsInstallment.value = true
    formAmount.value = instMaster.totalAmount.toLocaleString()
    formInstallmentMonths.value = instMaster.totalInstallments
  } else {
    formIsInstallment.value = false
    formAmount.value = tx.amount.toLocaleString()
    formInstallmentMonths.value = undefined
  }

  // 반복 거래 → 마스터에서 반복 일자/종료일 로드
  const recMaster = tx.recurringMasterId
    ? mockRecurringMasters.value.find(m => m.id === tx.recurringMasterId)
    : undefined
  if (recMaster) {
    formIsRecurring.value = true
    formRecurringDay.value = recMaster.dayOfMonth
    formRecurringEndDate.value = recMaster.endDate ?? ''
  } else {
    formIsRecurring.value = false
    formRecurringDay.value = dayjs(tx.date).date()
    formRecurringEndDate.value = ''
  }

  showModal.value = true
}

function txDelete(id: number) {
  txTransactions.value = txTransactions.value.filter(t => t.id !== id)
  saveAll()
}

// ── 할부 삭제 옵션 시트 ───────────────────────────────────────────────────────

const showInstDeleteSheet = ref(false)
const instDeleteTx        = ref<MockTxRecord | null>(null)

function onDeleteClick(tx: MockTxRecord) {
  if (tx.installmentMasterId !== undefined) {
    instDeleteTx.value = tx
    showInstDeleteSheet.value = true
  } else if (tx.recurringMasterId !== undefined) {
    recurDeleteTx.value = tx
    showRecurDeleteSheet.value = true
  } else {
    txDelete(tx.id)
  }
}

function deleteInstAll() {
  const masterId = instDeleteTx.value?.installmentMasterId
  if (masterId === undefined) return
  txTransactions.value = txTransactions.value.filter(t => t.installmentMasterId !== masterId)
  mockInstallmentMasters.value = mockInstallmentMasters.value.filter(m => m.id !== masterId)
  saveAll()
  showInstDeleteSheet.value = false
}

function deleteInstFromHere() {
  const tx = instDeleteTx.value
  if (!tx?.installmentMasterId) return
  txTransactions.value = txTransactions.value.filter(t =>
    t.installmentMasterId !== tx.installmentMasterId ||
    (t.installmentSequence ?? 0) < (tx.installmentSequence ?? 0)
  )
  saveAll()
  showInstDeleteSheet.value = false
}

function deleteInstSingle() {
  if (!instDeleteTx.value) return
  txDelete(instDeleteTx.value.id)
  showInstDeleteSheet.value = false
}

// ── 반복 삭제 옵션 시트 ───────────────────────────────────────────────────────

const showRecurDeleteSheet = ref(false)
const recurDeleteTx        = ref<MockTxRecord | null>(null)

function deleteRecurAll() {
  const masterId = recurDeleteTx.value?.recurringMasterId
  if (masterId === undefined) return
  txTransactions.value = txTransactions.value.filter(t => t.recurringMasterId !== masterId)
  mockRecurringMasters.value = mockRecurringMasters.value.filter(m => m.id !== masterId)
  // 해당 마스터의 스킵 항목도 정리
  recurSkippedSet.value = recurSkippedSet.value.filter(k => !k.startsWith(`${masterId}-`))
  saveAll()
  showRecurDeleteSheet.value = false
}

function deleteRecurFromHere() {
  const tx = recurDeleteTx.value
  if (!tx?.recurringMasterId) return
  // 이 거래 날짜 이후 거래 삭제 + 마스터 비활성화
  txTransactions.value = txTransactions.value.filter(t =>
    t.recurringMasterId !== tx.recurringMasterId || t.date < tx.date
  )
  const master = mockRecurringMasters.value.find(m => m.id === tx.recurringMasterId)
  if (master) master.isActive = false
  saveAll()
  showRecurDeleteSheet.value = false
}

function deleteRecurSingle() {
  if (!recurDeleteTx.value) return
  const tx = recurDeleteTx.value
  // skip 키는 표시 월(mockYear/mockMonth) 기준으로 생성 — getMonthPeriod 범위와 일치
  if (tx.recurringMasterId !== undefined) {
    const key = `${tx.recurringMasterId}-${mockYear.value}-${String(mockMonth.value).padStart(2, '0')}`
    if (!recurSkippedSet.value.includes(key)) recurSkippedSet.value.push(key)
  }
  txDelete(tx.id)  // id=-1(pending 컨텍스트)이면 실제 삭제 없이 saveAll만 실행
  showRecurDeleteSheet.value = false
}

// 배너 항목 클릭 → 해당 마스터 데이터로 폼 pre-fill 후 모달 열기
function onPendingEditClick(master: MockRecurringMaster) {
  const { start: pStart } = getMonthPeriod(mockYear.value, mockMonth.value, settingMonthStartDay.value)
  const dueDate = dayjs(pStart).date(master.dayOfMonth).format('YYYY-MM-DD')

  editingTxId.value = null
  pendingMasterId.value = master.id
  // formType 설정 전에 method를 먼저 세팅 — watch(formType) 발화 시 덮어쓰기 방지
  if (master.type === 'Savings') {
    formSavingsMethodId.value = master.paymentMethodId
    formPaymentMethodId.value = 0
  } else {
    formPaymentMethodId.value = master.paymentMethodId
    formSavingsMethodId.value = 0
  }
  formType.value = master.type
  formAmount.value = master.amount.toLocaleString()
  formDate.value = dueDate
  formCategoryId.value = master.categoryId
  formMemo.value = master.memo ?? ''
  formIsIncluded.value = true
  formSaved.value = false
  formIsInstallment.value = false
  formIsRecurring.value = false
  formRecurringDay.value = master.dayOfMonth
  formRecurringEndDate.value = ''
  savedCatByType.Expense = master.type === 'Expense' ? master.categoryId : 0
  savedCatByType.Income  = master.type === 'Income'  ? master.categoryId : 0
  savedCatByType.Savings = master.type === 'Savings' ? master.categoryId : 0
  showModal.value = true
}

// 배너 항목 X 클릭 → 기존 삭제 시트 재사용 (synthetic tx 생성)
function onPendingDeleteClick(master: MockRecurringMaster) {
  // 이번 기간 내 실제 결제 예정일: period.start 기준 해당 dayOfMonth 날짜 계산
  const { start: pStart } = getMonthPeriod(mockYear.value, mockMonth.value, settingMonthStartDay.value)
  const dueDate = dayjs(pStart).date(master.dayOfMonth).format('YYYY-MM-DD')
  recurDeleteTx.value = {
    id: -1,  // 실제 거래 없음을 표시
    amount: master.amount,
    date: dueDate,
    type: master.type,
    categoryId: master.categoryId,
    categoryName: master.categoryName,
    paymentMethodId: master.paymentMethodId,
    paymentMethodName: master.paymentMethodName,
    memo: master.memo,
    isIncludedInTotal: true,
    installmentMasterId: undefined,
    installmentSequence: undefined,
    recurringMasterId: master.id,
  }
  showRecurDeleteSheet.value = true
}

// ── 설정 (홈/폼에서 사용하는 데이터만 유지) ──────────────────────────────────

const settingMonthStartDay  = ref(25)
const settingCategories     = ref(MOCK_CATEGORIES.map(c => ({ ...c })))
const settingPaymentMethods = ref<{ id: number; name: string; type: 'Cash' | 'CreditCard' | 'DebitCard' | 'Point'; remainingAmount: number | undefined }[]>(
  MOCK_PAYMENT_METHODS.map(m => ({ ...m }))
)

// ── 설정 탭 ───────────────────────────────────────────────────────────────────

// 카테고리
const settingCatFilter = ref<'Expense' | 'Income' | 'Savings'>('Expense')
const newCatName       = ref('')
let   _nextCatId       = 100
function settingAddCat() {
  if (!newCatName.value.trim()) return
  settingCategories.value.push({ id: _nextCatId++, name: newCatName.value.trim(), type: settingCatFilter.value, isDefault: false })
  newCatName.value = ''
}
function settingDeleteCat(id: number) {
  settingCategories.value = settingCategories.value.filter(c => c.id !== id)
}

// 결제수단
const newMethodName = ref('')
const newMethodType = ref<'Cash' | 'CreditCard' | 'DebitCard' | 'Point'>('Cash')
let   _nextMethodId = 100
function settingAddMethod() {
  if (!newMethodName.value.trim()) return
  const id = _nextMethodId++
  const isCard = newMethodType.value === 'CreditCard' || newMethodType.value === 'DebitCard'
  settingPaymentMethods.value.push({ id, name: newMethodName.value.trim(), type: newMethodType.value, remainingAmount: newMethodType.value === 'Point' ? 0 : undefined })
  if (isCard) settingCardBilling.value.push({ paymentMethodId: id, name: newMethodName.value.trim(), cutoffDay: 15, dueDay: 25 })
  newMethodName.value = ''
}
function settingDeleteMethod(id: number) {
  settingPaymentMethods.value = settingPaymentMethods.value.filter(m => m.id !== id)
  settingCardBilling.value    = settingCardBilling.value.filter(c => c.paymentMethodId !== id)
}

// 카드 청구 설정
const settingCardBilling = ref(
  MOCK_PAYMENT_METHODS.filter(m => m.type === 'CreditCard' || m.type === 'DebitCard').map(m => ({ paymentMethodId: m.id, name: m.name, cutoffDay: 15, dueDay: 25 }))
)

// 저축 수단
const settingSavingsMethods = ref([
  { id: 1, name: '청약통장',      isDefault: true  },
  { id: 2, name: '국민은행 적금', isDefault: false },
])
const newSavingsName = ref('')
let   _nextSavingsId = 10
function settingAddSavings() {
  if (!newSavingsName.value.trim()) return
  settingSavingsMethods.value.push({ id: _nextSavingsId++, name: newSavingsName.value.trim(), isDefault: false })
  newSavingsName.value = ''
}
function settingDeleteSavings(id: number) {
  settingSavingsMethods.value = settingSavingsMethods.value.filter(s => s.id !== id)
}

// 포인트 예산
const settingPointBudgets = ref([
  { id: 1, name: '네이버페이',  totalAmount: 100000, remainingAmount: 45000  },
  { id: 2, name: '복지포인트', totalAmount: 600000, remainingAmount: 600000 },
])
const newPointName   = ref('')
const newPointAmount = ref('')
let   _nextPointId   = 10
function settingAddPoint() {
  const amt = parseInt(newPointAmount.value.replace(/,/g, ''))
  if (!newPointName.value.trim() || isNaN(amt) || amt <= 0) return
  settingPointBudgets.value.push({ id: _nextPointId++, name: newPointName.value.trim(), totalAmount: amt, remainingAmount: amt })
  newPointName.value = newPointAmount.value = ''
}
function settingDeletePoint(id: number) {
  settingPointBudgets.value = settingPointBudgets.value.filter(p => p.id !== id)
}

// 설정 아코디언
const settingOpenSections = ref<string[]>([])
function toggleSection(key: string) {
  const idx = settingOpenSections.value.indexOf(key)
  if (idx === -1) settingOpenSections.value.push(key)
  else settingOpenSections.value.splice(idx, 1)
}
function isSectionOpen(key: string) {
  return settingOpenSections.value.includes(key)
}

// ── 통계 탭 ──────────────────────────────────────────────────────────────────

const statsType = ref<'Expense' | 'Income' | 'Savings'>('Expense')
const donutCanvas = ref<HTMLCanvasElement | null>(null)
const trendCanvas = ref<HTMLCanvasElement | null>(null)
let donutChart: Chart | null = null
let trendChart: Chart | null = null

const CHART_COLORS = [
  '#3B82F6', '#EF4444', '#10B981', '#F59E0B', '#8B5CF6',
  '#EC4899', '#14B8A6', '#F97316', '#6366F1', '#84CC16',
]

// 전월 요약 (비교용)
const prevMonthSummary = computed(() => {
  const prevYear  = mockMonth.value === 1 ? mockYear.value - 1 : mockYear.value
  const prevMonth = mockMonth.value === 1 ? 12 : mockMonth.value - 1
  const { start, end } = getMonthPeriod(prevYear, prevMonth, settingMonthStartDay.value)
  const included = txTransactions.value.filter(t => t.date >= start && t.date <= end && t.isIncludedInTotal)
  return {
    totalIncome:  included.filter(t => t.type === 'Income').reduce((s, t) => s + t.amount, 0),
    totalExpense: included.filter(t => t.type === 'Expense').reduce((s, t) => s + t.amount, 0),
    totalSavings: included.filter(t => t.type === 'Savings').reduce((s, t) => s + t.amount, 0),
  }
})

// 현재 월 카테고리별 집계 (statsType 기준)
const statsCategoryData = computed(() => {
  const map = new Map<string, number>()
  for (const tx of txMonthFiltered.value) {
    if (!tx.isIncludedInTotal || tx.type !== statsType.value) continue
    map.set(tx.categoryName, (map.get(tx.categoryName) ?? 0) + tx.amount)
  }
  return Array.from(map.entries())
    .map(([name, amount]) => ({ name, amount }))
    .sort((a, b) => b.amount - a.amount)
})

// 최근 6개월 추이 (현재 월 기준)
const statsTrendData = computed(() => {
  const result: { label: string; income: number; expense: number; savings: number }[] = []
  for (let i = 5; i >= 0; i--) {
    let y = mockYear.value
    let m = mockMonth.value - i
    while (m <= 0) { m += 12; y-- }
    const { start, end } = getMonthPeriod(y, m, settingMonthStartDay.value)
    const included = txTransactions.value.filter(t => t.date >= start && t.date <= end && t.isIncludedInTotal)
    result.push({
      label:   `${m}월`,
      income:  included.filter(t => t.type === 'Income').reduce((s, t) => s + t.amount, 0),
      expense: included.filter(t => t.type === 'Expense').reduce((s, t) => s + t.amount, 0),
      savings: included.filter(t => t.type === 'Savings').reduce((s, t) => s + t.amount, 0),
    })
  }
  return result
})

// 카테고리별 지출 6개월 추이 (지출 전용 점선 라인차트)
const statsCatTrendData = computed(() => {
  const months: { year: number; month: number; label: string }[] = []
  for (let i = 5; i >= 0; i--) {
    let y = mockYear.value
    let m = mockMonth.value - i
    while (m <= 0) { m += 12; y-- }
    months.push({ year: y, month: m, label: `${m}월` })
  }
  const catMap = new Map<string, number[]>()
  months.forEach(({ year, month }, idx) => {
    const { start, end } = getMonthPeriod(year, month, settingMonthStartDay.value)
    const txs = txTransactions.value.filter(
      t => t.type === 'Expense' && t.isIncludedInTotal && t.date >= start && t.date <= end
    )
    for (const tx of txs) {
      if (!catMap.has(tx.categoryName)) catMap.set(tx.categoryName, Array(6).fill(0))
      catMap.get(tx.categoryName)![idx] += tx.amount
    }
  })
  return {
    labels: months.map(m => m.label),
    categories: Array.from(catMap.entries())
      .map(([name, data]) => ({ name, data }))
      .sort((a, b) => b.data.reduce((s, v) => s + v, 0) - a.data.reduce((s, v) => s + v, 0)),
  }
})

let catTrendChart: Chart | null = null
const catTrendCanvas = ref<HTMLCanvasElement | null>(null)
const catTrendHiddenCats = ref<string[]>([])

function toggleCatTrend(name: string) {
  const idx = catTrendHiddenCats.value.indexOf(name)
  if (idx >= 0) catTrendHiddenCats.value.splice(idx, 1)
  else catTrendHiddenCats.value.push(name)
  renderCatTrend()
}

function renderCatTrend() {
  if (!catTrendCanvas.value) return
  catTrendChart?.destroy()
  const { labels, categories } = statsCatTrendData.value
  if (categories.length === 0) { catTrendChart = null; return }
  // 숨김 제외, 색상 인덱스는 전체 목록 기준으로 고정
  const visible = categories.filter(cat => !catTrendHiddenCats.value.includes(cat.name))
  catTrendChart = new Chart(catTrendCanvas.value, {
    type: 'line',
    data: {
      labels,
      datasets: visible.map((cat) => {
        const colorIdx = categories.findIndex(c => c.name === cat.name)
        return {
          label: cat.name,
          data: cat.data,
          borderColor: CHART_COLORS[colorIdx % CHART_COLORS.length],
          backgroundColor: 'transparent',
          borderDash: [5, 5],
          borderWidth: 2,
          pointRadius: 4,
          pointHoverRadius: 6,
          pointBackgroundColor: CHART_COLORS[colorIdx % CHART_COLORS.length],
          tension: 0.3,
        }
      }),
    },
    options: {
      responsive: true,
      interaction: { mode: 'index', intersect: false },
      plugins: {
        legend: { display: false },
        tooltip: { callbacks: { label: ctx => ` ${ctx.dataset.label}: ${(ctx.raw as number).toLocaleString()}원` } },
      },
      scales: {
        y: {
          beginAtZero: true,
          ticks: { callback: val => `${((val as number) / 10000).toFixed(0)}만`, font: { size: 10 }, color: '#9CA3AF' },
          grid: { color: 'rgba(255,255,255,0.05)' },
        },
        x: { ticks: { font: { size: 11 }, color: '#9CA3AF' }, grid: { display: false } },
      },
    },
  })
}

// 도넛 중앙 텍스트 플러그인 — absolute div 대신 캔버스에 직접 그려 툴팁 충돌 방지
const donutCenterPlugin = {
  id: 'donutCenter',
  afterDatasetsDraw(chart: Chart) {
    const { ctx, chartArea } = chart
    if (!chartArea || !chart.data.datasets[0]) return
    const total = (chart.data.datasets[0].data as number[]).reduce((s, v) => s + (Number(v) || 0), 0)
    const cx = (chartArea.left + chartArea.right) / 2
    const cy = (chartArea.top + chartArea.bottom) / 2
    ctx.save()
    ctx.textAlign = 'center'
    ctx.textBaseline = 'middle'
    ctx.fillStyle = '#9CA3AF'
    ctx.font = '10px sans-serif'
    ctx.fillText('합계', cx, cy - 9)
    ctx.fillStyle = '#F3F4F6'
    ctx.font = 'bold 12px sans-serif'
    ctx.fillText(`${(total / 10000).toFixed(0)}만원`, cx, cy + 9)
    ctx.restore()
  },
}

function renderDonut() {
  if (!donutCanvas.value) return
  donutChart?.destroy()
  const data = statsCategoryData.value
  if (data.length === 0) { donutChart = null; return }
  donutChart = new Chart(donutCanvas.value, {
    type: 'doughnut',
    data: {
      labels: data.map(d => d.name),
      datasets: [{ data: data.map(d => d.amount), backgroundColor: CHART_COLORS.slice(0, data.length), borderWidth: 0 }],
    },
    options: {
      responsive: true,
      plugins: {
        legend: { display: false },
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        tooltip: { callbacks: { label: (ctx: any) => ` ${ctx.label}: ${(ctx.raw as number).toLocaleString()}원` } },
      },
      cutout: '65%',
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    } as any,
    plugins: [donutCenterPlugin],
  })
}

function renderTrend() {
  if (!trendCanvas.value) return
  trendChart?.destroy()
  const data = statsTrendData.value
  trendChart = new Chart(trendCanvas.value, {
    type: 'bar',
    data: {
      labels: data.map(d => d.label),
      datasets: [
        { label: '수입', data: data.map(d => d.income),  backgroundColor: 'rgba(96,165,250,0.85)',  borderRadius: 3 },
        { label: '지출', data: data.map(d => d.expense), backgroundColor: 'rgba(248,113,113,0.85)', borderRadius: 3 },
        { label: '저축', data: data.map(d => d.savings), backgroundColor: 'rgba(52,211,153,0.85)',  borderRadius: 3 },
      ],
    },
    options: {
      responsive: true,
      interaction: { mode: 'index', intersect: false },
      plugins: {
        legend: { position: 'top', labels: { font: { size: 11 }, boxWidth: 10, color: '#9CA3AF' } },
        tooltip: {
          callbacks: {
            label: (ctx: any) => {
              const income = ctx.chart.data.datasets[0].data[ctx.dataIndex] as number
              const val = ctx.raw as number
              const pct = income > 0 ? ` (${Math.round(val / income * 100)}%)` : ''
              return ` ${ctx.dataset.label}: ${val.toLocaleString()}원${pct}`
            },
          },
        },
      },
      scales: {
        y: {
          beginAtZero: true,
          ticks: { callback: val => `${((val as number) / 10000).toFixed(0)}만`, font: { size: 10 }, color: '#9CA3AF' },
          grid: { color: 'rgba(255,255,255,0.05)' },
        },
        x: { ticks: { font: { size: 11 }, color: '#9CA3AF' }, grid: { display: false } },
      },
    },
  })
}

async function renderStatsCharts() {
  await nextTick()
  renderDonut()
  renderTrend()
  renderCatTrend()
}

watch(activePage, (page) => { if (page === 'stats') renderStatsCharts() })
watch([mockYear, mockMonth], () => {
  catTrendHiddenCats.value = []  // 월 변경 시 카테고리 필터 초기화
  if (activePage.value === 'stats') renderStatsCharts()
})
watch(statsType, () => { if (activePage.value === 'stats') renderStatsCharts() })

onUnmounted(() => {
  donutChart?.destroy()
  trendChart?.destroy()
  catTrendChart?.destroy()
})
</script>

<template>
  <div class="h-full flex flex-col bg-gray-900">

    <!-- DEV 배너 -->
    <div class="flex-shrink-0 bg-orange-500 text-white text-xs text-center py-1 font-medium tracking-wide">
      DEV MOCKUP — 로컬 개발 전용 · 백엔드 미접근
    </div>

    <div class="flex-1 overflow-hidden flex flex-col">

      <!-- ══ 홈 ══ -->
      <div v-show="activePage === 'home'" class="flex-1 overflow-hidden flex flex-col max-w-lg mx-auto w-full relative">

        <!-- 월 헤더 -->
        <div class="flex-shrink-0 bg-gray-900 px-4 pt-5 pb-4">
          <div class="flex items-center justify-between mb-3">
            <button data-testid="mock-btn-prev-month" @click="prevMonth" :disabled="!canGoPrev" class="p-1 rounded-full hover:bg-gray-700 disabled:opacity-30 disabled:cursor-not-allowed">
              <svg class="w-6 h-6 text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"/></svg>
            </button>
            <div class="text-center">
              <div data-testid="mock-month-label" class="text-lg font-semibold text-gray-100">{{ mockMonthLabel }}</div>
              <div v-if="mockMonthRange" class="text-xs text-gray-400 mt-0.5">{{ mockMonthRange }}</div>
            </div>
            <button data-testid="mock-btn-next-month" @click="nextMonth" :disabled="!canGoNext" class="p-1 rounded-full hover:bg-gray-700 disabled:opacity-30 disabled:cursor-not-allowed">
              <svg class="w-6 h-6 text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"/></svg>
            </button>
          </div>
          <div class="flex items-center justify-between">
            <div class="text-center flex-1">
              <div class="text-gray-400 text-[10px] mb-0.5">수입</div>
              <div data-testid="mock-summary-income" class="text-blue-400 font-semibold text-xs truncate">+{{ mockSummary.totalIncome.toLocaleString() }}</div>
            </div>
            <div class="w-px h-7 bg-gray-700 flex-shrink-0" />
            <div class="text-center flex-1">
              <div class="text-gray-400 text-[10px] mb-0.5">지출</div>
              <div data-testid="mock-summary-expense" class="text-red-500 font-semibold text-xs truncate">-{{ mockSummary.totalExpense.toLocaleString() }}</div>
            </div>
            <div class="w-px h-7 bg-gray-700 flex-shrink-0" />
            <div class="text-center flex-1">
              <div class="text-gray-400 text-[10px] mb-0.5">저축</div>
              <div data-testid="mock-summary-savings" class="text-emerald-500 font-semibold text-xs truncate">-{{ mockSummary.totalSavings.toLocaleString() }}</div>
            </div>
            <div class="w-px h-7 bg-gray-700 flex-shrink-0" />
            <div class="text-center flex-1">
              <div class="text-gray-400 text-[10px] mb-0.5">잔액</div>
              <div data-testid="mock-summary-balance" class="text-white font-bold text-xs truncate">{{ mockSummary.balance.toLocaleString() }}</div>
            </div>
          </div>
        </div>


        <!-- 반복 예정 배너 -->
        <div v-if="recurringPending.length > 0" data-testid="mock-pending-banner" class="flex-shrink-0 bg-gray-800 border-b border-gray-700">
          <button data-testid="mock-pending-toggle" @click="showPendingBanner = !showPendingBanner" class="w-full px-4 py-2.5 flex items-center justify-between">
            <div class="flex items-center gap-2 min-w-0">
              <svg class="w-3.5 h-3.5 text-teal-400 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"/></svg>
              <span class="text-xs font-semibold text-teal-400">반복 예정 {{ recurringPending.length }}건</span>
              <span class="text-xs truncate flex items-center gap-1">
                <template v-if="pendingSummary.income > 0"><span class="text-blue-400">+{{ pendingSummary.income.toLocaleString() }}원</span></template>
                <template v-if="pendingSummary.income > 0 && pendingSummary.expense > 0"><span class="text-gray-500">/</span></template>
                <template v-if="pendingSummary.expense > 0"><span class="text-red-500">-{{ pendingSummary.expense.toLocaleString() }}원</span></template>
                <template v-if="(pendingSummary.income > 0 || pendingSummary.expense > 0) && pendingSummary.savings > 0"><span class="text-gray-500">/</span></template>
                <template v-if="pendingSummary.savings > 0"><span class="text-emerald-500">-{{ pendingSummary.savings.toLocaleString() }}원</span></template>
              </span>
            </div>
            <svg :class="showPendingBanner ? 'rotate-180' : ''" class="w-4 h-4 text-teal-400 flex-shrink-0 transition-transform" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/></svg>
          </button>
          <div v-if="showPendingBanner" data-testid="mock-pending-list" class="px-4 pb-3 space-y-1">
            <div v-for="master in recurringPending" :key="master.id"
              data-testid="mock-pending-item" :data-pending-type="master.type"
              class="flex items-center justify-between bg-gray-800 rounded-lg px-3 py-2 border border-gray-700 cursor-pointer active:bg-gray-700"
              @click="onPendingEditClick(master)">
              <div class="flex items-center gap-2 min-w-0">
                <div class="w-1.5 h-1.5 rounded-full flex-shrink-0" :class="master.type === 'Income' ? 'bg-blue-400' : master.type === 'Savings' ? 'bg-emerald-400' : 'bg-red-400'"/>
                <span class="text-xs font-medium text-gray-100 truncate">{{ master.categoryName }}</span>
                <span class="text-[11px] text-gray-400 flex-shrink-0">매월 {{ master.dayOfMonth }}일</span>
                <span v-if="master.memo" class="text-[11px] text-gray-400 truncate">· {{ master.memo }}</span>
              </div>
              <div class="flex items-center gap-1 ml-2 flex-shrink-0">
                <span :class="master.type === 'Income' ? 'text-blue-400' : master.type === 'Savings' ? 'text-emerald-500' : 'text-red-500'" class="text-xs font-semibold">
                  {{ master.type === 'Income' ? '+' : '-' }}{{ master.amount.toLocaleString() }}원
                </span>
                <button @click.stop="onPendingDeleteClick(master)" class="p-0.5 text-gray-300 hover:text-red-400">
                  <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- 카드 결제 예정 배너 (Sprint 6 Step 1) — 해당 월 청구 카드가 있을 때만 표시 -->
        <UpcomingWidget
          :cards="cardBillingSummary"
          @filter-card="cardBillingModal = $event"
        />

        <!-- 검색/필터 아코디언 -->
        <div data-testid="mock-filter-accordion" class="flex-shrink-0 bg-gray-800 border-b border-gray-700">
          <!-- 헤더 -->
          <button
            data-testid="mock-filter-toggle"
            class="w-full px-4 py-2.5 flex items-center justify-between"
            @click="showFilterPanel = !showFilterPanel"
          >
            <div class="flex items-center gap-2 min-w-0">
              <svg class="w-3.5 h-3.5 text-gray-400 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
              </svg>
              <span class="text-xs font-semibold text-gray-300">검색/필터</span>
              <span v-if="txSearch" class="text-xs bg-gray-700 text-gray-300 px-2 py-0.5 rounded-full truncate max-w-[90px]">"{{ txSearch }}"</span>
              <span v-if="txSelectedType !== null" class="text-xs px-2 py-0.5 rounded-full flex-shrink-0 font-medium"
                :class="txSelectedType === 'Income' ? 'bg-blue-100 text-blue-700' : txSelectedType === 'Savings' ? 'bg-emerald-100 text-emerald-700' : 'bg-red-100 text-red-700'"
              >{{ txSelectedType === 'Income' ? '수입' : txSelectedType === 'Savings' ? '저축' : '지출' }}</span>
              <span v-if="txSelectedCategory !== null" class="text-xs bg-gray-700 text-gray-300 px-2 py-0.5 rounded-full flex-shrink-0">{{ MOCK_CATEGORIES.find(c => c.id === txSelectedCategory)?.name }}</span>
            </div>
            <svg
              class="w-4 h-4 text-gray-400 flex-shrink-0 transition-transform duration-200"
              :class="showFilterPanel ? 'rotate-180' : ''"
              fill="none" stroke="currentColor" viewBox="0 0 24 24"
            >
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
            </svg>
          </button>

          <!-- 펼쳐진 패널 -->
          <div v-if="showFilterPanel" data-testid="mock-filter-panel" class="px-4 pb-3 space-y-2.5">
            <!-- 메모 검색 -->
            <input
              data-testid="mock-filter-memo"
              v-model="txSearch"
              type="text"
              placeholder="메모 검색..."
              class="w-full text-sm border border-gray-700 rounded-xl px-3 py-2 focus:outline-none focus:border-blue-400 bg-gray-700 text-gray-100"
            />
            <!-- 1차: 거래 유형 필터 -->
            <div class="flex gap-2">
              <button
                @click="txSelectedType = null"
                :class="txSelectedType === null ? 'bg-gray-700 text-white' : 'bg-gray-700 text-gray-300'"
                class="flex-shrink-0 text-xs px-3 py-1.5 rounded-full font-medium"
              >전체</button>
              <button
                data-testid="mock-filter-type-income"
                @click="txSelectedType = txSelectedType === 'Income' ? null : 'Income'"
                :class="txSelectedType === 'Income' ? 'bg-blue-600 text-white' : 'bg-gray-700 text-gray-300'"
                class="flex-shrink-0 text-xs px-3 py-1.5 rounded-full font-medium"
              >수입</button>
              <button
                data-testid="mock-filter-type-expense"
                @click="txSelectedType = txSelectedType === 'Expense' ? null : 'Expense'"
                :class="txSelectedType === 'Expense' ? 'bg-red-500 text-white' : 'bg-gray-700 text-gray-300'"
                class="flex-shrink-0 text-xs px-3 py-1.5 rounded-full font-medium"
              >지출</button>
              <button
                data-testid="mock-filter-type-savings"
                @click="txSelectedType = txSelectedType === 'Savings' ? null : 'Savings'"
                :class="txSelectedType === 'Savings' ? 'bg-emerald-500 text-white' : 'bg-gray-700 text-gray-300'"
                class="flex-shrink-0 text-xs px-3 py-1.5 rounded-full font-medium"
              >저축</button>
            </div>
            <!-- 2차: 카테고리 필터 (유형 선택 시에만 표시) -->
            <div v-if="txTypeCategoryChips.length > 0" data-testid="mock-filter-category-chips" class="flex gap-2 overflow-x-auto pb-0.5">
              <button
                @click="txSelectedCategory = null"
                :class="txSelectedCategory === null ? 'bg-gray-700 text-white' : 'bg-gray-700 text-gray-300'"
                class="flex-shrink-0 text-xs px-3 py-1.5 rounded-full font-medium"
              >전체</button>
              <button
                v-for="cat in txTypeCategoryChips"
                :key="cat.id"
                @click="txSelectedCategory = txSelectedCategory === cat.id ? null : cat.id"
                :class="txSelectedCategory === cat.id
                  ? (txSelectedType === 'Income' ? 'bg-blue-600 text-white' : txSelectedType === 'Savings' ? 'bg-emerald-500 text-white' : 'bg-red-500 text-white')
                  : 'bg-gray-700 text-gray-300'"
                class="flex-shrink-0 text-xs px-3 py-1.5 rounded-full font-medium"
              >{{ cat.name }}</button>
            </div>
          </div>
        </div>

        <!-- 거래 목록 — FAB 영역 확보를 위해 하단 패딩 추가 -->
        <div class="flex-1 overflow-y-auto px-4 pt-2 pb-20">
          <div v-if="txFiltered.length === 0" class="py-10 text-center text-gray-400 text-sm">거래 내역이 없습니다</div>
          <template v-else>
            <div v-for="[date, txs] in txGroupByDate(txFiltered)" :key="date" class="mb-3">
              <div class="flex items-center justify-between mb-0.5">
                <span class="text-[11px] font-semibold text-gray-400">{{ dayjs(date).format('MM/DD (ddd)') }}</span>
                <span class="text-[11px] flex items-center gap-1">
                  <template v-if="txs.filter(t=>t.type==='Income').reduce((s,t)=>s+t.amount,0) > 0"><span class="text-blue-400">+{{ txs.filter(t=>t.type==='Income').reduce((s,t)=>s+t.amount,0).toLocaleString() }}</span></template>
                  <template v-if="txs.filter(t=>t.type==='Income').reduce((s,t)=>s+t.amount,0) > 0 && txs.filter(t=>t.type==='Expense').reduce((s,t)=>s+t.amount,0) > 0"><span class="text-gray-500">/</span></template>
                  <template v-if="txs.filter(t=>t.type==='Expense').reduce((s,t)=>s+t.amount,0) > 0"><span class="text-red-500">-{{ txs.filter(t=>t.type==='Expense').reduce((s,t)=>s+t.amount,0).toLocaleString() }}</span></template>
                  <template v-if="(txs.filter(t=>t.type==='Income').reduce((s,t)=>s+t.amount,0) > 0 || txs.filter(t=>t.type==='Expense').reduce((s,t)=>s+t.amount,0) > 0) && txs.filter(t=>t.type==='Savings').reduce((s,t)=>s+t.amount,0) > 0"><span class="text-gray-500">/</span></template>
                  <template v-if="txs.filter(t=>t.type==='Savings').reduce((s,t)=>s+t.amount,0) > 0"><span class="text-emerald-500">-{{ txs.filter(t=>t.type==='Savings').reduce((s,t)=>s+t.amount,0).toLocaleString() }}</span></template>
                </span>
              </div>
              <div v-for="tx in txs" :key="tx.id" data-testid="mock-tx-item" @click="openEdit(tx)" class="bg-gray-800 rounded-lg px-3 py-2 mb-1 flex items-center shadow-sm cursor-pointer active:bg-gray-700" :class="!tx.isIncludedInTotal ? 'opacity-50' : ''">
                <div class="flex-1 min-w-0">
                  <div class="flex items-center gap-1 flex-wrap">
                    <span class="text-xs font-medium truncate text-gray-100">{{ tx.categoryName }}</span>
                    <span v-if="!tx.isIncludedInTotal" class="text-[10px] bg-gray-700 text-gray-400 px-1 py-0.5 rounded">제외</span>
                    <span v-if="tx.installmentMasterId" class="text-[10px] bg-orange-900/20 text-orange-500 px-1 py-0.5 rounded">할부 {{ tx.installmentSequence }}/{{ mockInstallmentMasters.find(m => m.id === tx.installmentMasterId)?.totalInstallments }}회</span>
                    <span v-else-if="tx.recurringMasterId" class="text-[10px] bg-gray-800 text-blue-500 px-1 py-0.5 rounded">반복</span>
                  </div>
                  <div class="text-[11px] text-gray-400 mt-0.5">
                    <template v-if="tx.type === 'Expense' || tx.type === 'Savings'">{{ tx.paymentMethodName }}<span v-if="tx.memo"> · {{ tx.memo }}</span></template>
                    <span v-else-if="tx.memo">{{ tx.memo }}</span>
                  </div>
                </div>
                <span :class="tx.type === 'Income' ? 'text-blue-400' : tx.type === 'Savings' ? 'text-emerald-500' : 'text-red-500'" class="font-semibold text-xs ml-2 mr-1 flex-shrink-0">{{ txFormatAmount(tx.amount, tx.type) }}</span>
                <button @click.stop="onDeleteClick(tx)" class="flex-shrink-0 p-0.5 text-gray-300 hover:text-red-400">
                  <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>
                </button>
              </div>
            </div>
          </template>
        </div>

        <!-- FAB — 홈 컨테이너(relative) 기준 absolute, 스크롤 영역 밖 -->
        <button @click="openAdd" class="absolute bottom-4 right-4 w-14 h-14 bg-blue-600 text-white rounded-full shadow-lg flex items-center justify-center z-40">
          <svg class="w-7 h-7" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/></svg>
        </button>

      </div>

      <!-- ══ 통계 ══ -->
      <div v-show="activePage === 'stats'" class="flex-1 overflow-hidden flex flex-col max-w-lg mx-auto w-full">

        <!-- 헤더 (고정) -->
        <div class="flex-shrink-0 bg-gray-900 px-4 pt-5 pb-4">
          <div class="flex items-center justify-between">
            <button @click="prevMonth" :disabled="!canGoPrev" class="p-1 rounded-full hover:bg-gray-700 disabled:opacity-30 disabled:cursor-not-allowed">
              <svg class="w-6 h-6 text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"/></svg>
            </button>
            <div class="text-center">
              <div class="text-lg font-semibold text-gray-100">{{ mockMonthLabel }} 통계</div>
              <div v-if="mockMonthRange" class="text-xs text-gray-400 mt-0.5">{{ mockMonthRange }}</div>
            </div>
            <button @click="nextMonth" :disabled="!canGoNext" class="p-1 rounded-full hover:bg-gray-700 disabled:opacity-30 disabled:cursor-not-allowed">
              <svg class="w-6 h-6 text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"/></svg>
            </button>
          </div>
        </div>

        <!-- 스크롤 영역 (헤더 제외) -->
        <div class="flex-1 overflow-y-auto">

        <!-- 이번 달 요약 카드 (전월 비교) -->
        <div class="px-4 pb-4">
          <div class="bg-gray-800 rounded-2xl p-4 grid grid-cols-2 gap-3">
            <!-- 수입 -->
            <div class="bg-gray-700/60 rounded-xl p-3">
              <div class="text-[10px] text-gray-400 mb-1">수입</div>
              <div class="text-blue-400 font-bold text-sm">+{{ mockSummary.totalIncome.toLocaleString() }}원 <span class="text-[10px] text-blue-300/60 font-normal">(100%)</span></div>
              <div class="text-[10px] text-gray-400 mt-1 flex items-center gap-0.5">
                <template v-if="prevMonthSummary.totalIncome > 0">
                  전월 대비
                  <span :class="mockSummary.totalIncome >= prevMonthSummary.totalIncome ? 'text-blue-400' : 'text-red-400'" class="ml-0.5">
                    {{ mockSummary.totalIncome >= prevMonthSummary.totalIncome ? '▲' : '▼' }}{{ Math.abs(Math.round((mockSummary.totalIncome - prevMonthSummary.totalIncome) / prevMonthSummary.totalIncome * 100)) }}%
                  </span>
                </template>
                <template v-else>-</template>
              </div>
            </div>
            <!-- 지출 -->
            <div class="bg-gray-700/60 rounded-xl p-3">
              <div class="text-[10px] text-gray-400 mb-1">지출</div>
              <div class="text-red-400 font-bold text-sm">-{{ mockSummary.totalExpense.toLocaleString() }}원 <span class="text-[10px] text-red-300/60 font-normal">({{ mockSummary.totalIncome > 0 ? Math.round(mockSummary.totalExpense / mockSummary.totalIncome * 100) : 0 }}%)</span></div>
              <div class="text-[10px] text-gray-400 mt-1 flex items-center gap-0.5">
                <template v-if="prevMonthSummary.totalExpense > 0">
                  전월 대비
                  <span :class="mockSummary.totalExpense <= prevMonthSummary.totalExpense ? 'text-blue-400' : 'text-red-400'" class="ml-0.5">
                    {{ mockSummary.totalExpense <= prevMonthSummary.totalExpense ? '▼' : '▲' }}{{ Math.abs(Math.round((mockSummary.totalExpense - prevMonthSummary.totalExpense) / prevMonthSummary.totalExpense * 100)) }}%
                  </span>
                </template>
                <template v-else>-</template>
              </div>
            </div>
            <!-- 저축 -->
            <div class="bg-gray-700/60 rounded-xl p-3">
              <div class="text-[10px] text-gray-400 mb-1">저축</div>
              <div class="text-emerald-400 font-bold text-sm">-{{ mockSummary.totalSavings.toLocaleString() }}원 <span class="text-[10px] text-emerald-300/60 font-normal">({{ mockSummary.totalIncome > 0 ? Math.round(mockSummary.totalSavings / mockSummary.totalIncome * 100) : 0 }}%)</span></div>
              <div class="text-[10px] text-gray-400 mt-1 flex items-center gap-0.5">
                <template v-if="prevMonthSummary.totalSavings > 0">
                  전월 대비
                  <span class="text-emerald-400 ml-0.5">
                    {{ mockSummary.totalSavings >= prevMonthSummary.totalSavings ? '▲' : '▼' }}{{ Math.abs(Math.round((mockSummary.totalSavings - prevMonthSummary.totalSavings) / prevMonthSummary.totalSavings * 100)) }}%
                  </span>
                </template>
                <template v-else>-</template>
              </div>
            </div>
            <!-- 잔액 -->
            <div class="bg-gray-700/60 rounded-xl p-3">
              <div class="text-[10px] text-gray-400 mb-1">잔액</div>
              <div class="text-white font-bold text-sm">{{ mockSummary.balance.toLocaleString() }}원 <span class="text-[10px] text-gray-400/60 font-normal">({{ mockSummary.totalIncome > 0 ? Math.round(mockSummary.balance / mockSummary.totalIncome * 100) : 0 }}%)</span></div>
            </div>
          </div>
        </div>

        <!-- 카테고리별 도넛 차트 -->
        <div class="px-4 pb-4">
          <div class="bg-gray-800 rounded-2xl p-4">
            <h3 class="text-sm font-semibold text-gray-100 mb-3">카테고리별 분석</h3>
            <!-- 유형 탭 -->
            <div class="flex rounded-xl bg-gray-700 p-1 mb-4">
              <button @click="statsType = 'Expense'" :class="statsType === 'Expense' ? 'bg-red-500 text-white shadow-sm' : 'text-gray-400'" class="flex-1 py-1.5 text-xs font-medium rounded-lg transition-all">지출</button>
              <button @click="statsType = 'Income'"  :class="statsType === 'Income'  ? 'bg-blue-500 text-white shadow-sm' : 'text-gray-400'" class="flex-1 py-1.5 text-xs font-medium rounded-lg transition-all">수입</button>
              <button @click="statsType = 'Savings'" :class="statsType === 'Savings' ? 'bg-emerald-500 text-white shadow-sm' : 'text-gray-400'" class="flex-1 py-1.5 text-xs font-medium rounded-lg transition-all">저축</button>
            </div>
            <!-- 차트 없을 때 -->
            <div v-if="statsCategoryData.length === 0" class="py-8 text-center text-gray-400 text-sm">
              해당 유형의 거래가 없습니다
            </div>
            <!-- 도넛 + 범례 -->
            <div v-else class="flex gap-4 items-center">
              <div class="flex-shrink-0" style="width:120px;height:120px">
                <canvas ref="donutCanvas"></canvas>
              </div>
              <div class="flex-1 space-y-2 min-w-0">
                <div v-for="(item, i) in statsCategoryData" :key="item.name" class="flex items-center gap-2">
                  <div class="w-2.5 h-2.5 rounded-full flex-shrink-0" :style="`background-color:${CHART_COLORS[i % CHART_COLORS.length]}`"></div>
                  <span class="text-xs text-gray-300 truncate flex-1">{{ item.name }}</span>
                  <span class="text-xs text-gray-100 font-medium flex-shrink-0">{{ item.amount.toLocaleString() }}</span>
                  <span class="text-[10px] text-gray-400 flex-shrink-0">{{ Math.round(item.amount / statsCategoryData.reduce((s, d) => s + d.amount, 0) * 100) }}%</span>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- 최근 6개월 추이 -->
        <div class="px-4 pb-4">
          <div class="bg-gray-800 rounded-2xl p-4">
            <h3 class="text-sm font-semibold text-gray-100 mb-4">최근 6개월 추이</h3>
            <canvas ref="trendCanvas" style="max-height:200px"></canvas>
          </div>
        </div>

        <!-- 카테고리별 지출 추이 (점선) -->
        <div class="px-4 pb-8">
          <div class="bg-gray-800 rounded-2xl p-4">
            <div class="flex items-center gap-2 mb-1">
              <h3 class="text-sm font-semibold text-gray-100">카테고리별 지출 추이</h3>
              <span class="text-[10px] text-gray-500 bg-gray-700 px-1.5 py-0.5 rounded">지출 전용</span>
            </div>
            <p class="text-[11px] text-gray-400 mb-4">최근 6개월간 카테고리별 지출 흐름</p>
            <div v-if="statsCatTrendData.categories.length === 0" class="py-8 text-center text-gray-400 text-sm">
              지출 데이터가 없습니다
            </div>
            <template v-else>
              <canvas ref="catTrendCanvas" style="max-height:220px"></canvas>
              <!-- 커스텀 범례 — 클릭으로 카테고리 토글 -->
              <div class="flex flex-wrap gap-x-2 gap-y-2 mt-3">
                <button
                  v-for="(cat, i) in statsCatTrendData.categories"
                  :key="cat.name"
                  @click="toggleCatTrend(cat.name)"
                  class="flex items-center gap-1.5 px-2 py-1 rounded-lg transition-all"
                  :class="catTrendHiddenCats.includes(cat.name)
                    ? 'bg-gray-700/40 opacity-40'
                    : 'bg-gray-700/70'"
                >
                  <span class="w-4 h-0 border-t-2 border-dashed flex-shrink-0"
                    :style="`border-color:${CHART_COLORS[i % CHART_COLORS.length]}`"></span>
                  <span class="text-xs text-gray-200 whitespace-nowrap">{{ cat.name }}</span>
                </button>
              </div>
              <p class="text-[10px] text-gray-500 mt-2">항목을 눌러 표시/숨김 전환</p>
            </template>
          </div>
        </div>

        </div><!-- /스크롤 영역 -->
      </div>

      <!-- ══ 설정 ══ -->
      <div v-show="activePage === 'settings'" class="flex-1 overflow-hidden flex flex-col max-w-lg mx-auto w-full">

        <!-- 헤더 (고정) -->
        <div class="flex-shrink-0 bg-gray-900 px-4 py-3 border-b border-gray-700/50">
          <h2 class="text-base font-semibold text-gray-100">설정</h2>
        </div>

        <!-- 스크롤 영역 -->
        <div class="flex-1 overflow-y-auto px-4 py-4 space-y-4">

          <!-- 월 시작일 -->
          <div class="bg-gray-800 rounded-2xl overflow-hidden">
            <button @click="toggleSection('monthStart')" class="w-full flex items-center justify-between px-4 py-3.5">
              <div class="flex items-center gap-3">
                <span class="text-sm font-semibold text-gray-100">월 시작일</span>
                <span class="text-xs text-gray-400">{{ settingMonthStartDay }}일 기준</span>
              </div>
              <svg class="w-4 h-4 text-gray-400 transition-transform duration-200" :class="isSectionOpen('monthStart') ? 'rotate-180' : ''" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/></svg>
            </button>
            <div v-show="isSectionOpen('monthStart')" class="px-4 pb-4 border-t border-gray-700/50">
              <div class="flex items-center gap-3 pt-3">
                <select v-model.number="settingMonthStartDay" class="bg-gray-700 text-gray-100 text-sm rounded-xl px-3 py-2 outline-none">
                  <option v-for="d in 28" :key="d" :value="d">{{ d }}일</option>
                </select>
                <span class="text-xs text-gray-400">매월 {{ settingMonthStartDay }}일 ~ 다음달 {{ settingMonthStartDay - 1 }}일 기준</span>
              </div>
            </div>
          </div>

          <!-- 카테고리 관리 -->
          <div class="bg-gray-800 rounded-2xl overflow-hidden">
            <button @click="toggleSection('category')" class="w-full flex items-center justify-between px-4 py-3.5">
              <div class="flex items-center gap-3">
                <span class="text-sm font-semibold text-gray-100">카테고리</span>
                <span class="text-xs text-gray-400">{{ settingCategories.length }}개</span>
              </div>
              <svg class="w-4 h-4 text-gray-400 transition-transform duration-200" :class="isSectionOpen('category') ? 'rotate-180' : ''" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/></svg>
            </button>
            <div v-show="isSectionOpen('category')" class="px-4 pb-4 border-t border-gray-700/50">
              <div class="flex rounded-xl bg-gray-700 p-1 mt-3 mb-3">
                <button @click="settingCatFilter = 'Expense'" :class="settingCatFilter === 'Expense' ? 'bg-red-500 text-white shadow-sm' : 'text-gray-400'" class="flex-1 py-1 text-xs font-medium rounded-lg transition-all">지출</button>
                <button @click="settingCatFilter = 'Income'"  :class="settingCatFilter === 'Income'  ? 'bg-blue-500 text-white shadow-sm' : 'text-gray-400'" class="flex-1 py-1 text-xs font-medium rounded-lg transition-all">수입</button>
                <button @click="settingCatFilter = 'Savings'" :class="settingCatFilter === 'Savings' ? 'bg-emerald-500 text-white shadow-sm' : 'text-gray-400'" class="flex-1 py-1 text-xs font-medium rounded-lg transition-all">저축</button>
              </div>
              <div class="space-y-1.5 mb-3">
                <div v-for="cat in settingCategories.filter(c => c.type === settingCatFilter)" :key="cat.id"
                  class="flex items-center justify-between bg-gray-700/60 rounded-xl px-3 py-2">
                  <span class="text-sm text-gray-200">{{ cat.name }}</span>
                  <span v-if="cat.isDefault" class="text-[10px] text-gray-500 bg-gray-700 px-1.5 py-0.5 rounded">기본</span>
                  <button v-else @click="settingDeleteCat(cat.id)" class="text-gray-500 hover:text-red-400 transition-colors p-1">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>
                  </button>
                </div>
                <p v-if="settingCategories.filter(c => c.type === settingCatFilter).length === 0" class="text-xs text-gray-500 text-center py-2">항목 없음</p>
              </div>
              <div class="flex gap-2">
                <input v-model="newCatName" placeholder="카테고리명" @keyup.enter="settingAddCat"
                  class="flex-1 bg-gray-700 text-gray-100 text-sm rounded-xl px-3 py-2 placeholder-gray-500 outline-none" />
                <button @click="settingAddCat" class="bg-blue-600 hover:bg-blue-500 text-white text-xs px-3 py-2 rounded-xl transition-colors">추가</button>
              </div>
            </div>
          </div>

          <!-- 결제수단 관리 -->
          <div class="bg-gray-800 rounded-2xl overflow-hidden">
            <button @click="toggleSection('paymentMethod')" class="w-full flex items-center justify-between px-4 py-3.5">
              <div class="flex items-center gap-3">
                <span class="text-sm font-semibold text-gray-100">결제수단</span>
                <span class="text-xs text-gray-400">{{ settingPaymentMethods.length }}개</span>
              </div>
              <svg class="w-4 h-4 text-gray-400 transition-transform duration-200" :class="isSectionOpen('paymentMethod') ? 'rotate-180' : ''" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/></svg>
            </button>
            <div v-show="isSectionOpen('paymentMethod')" class="px-4 pb-4 border-t border-gray-700/50">
              <div class="space-y-1.5 mt-3 mb-3">
                <div v-for="m in settingPaymentMethods" :key="m.id"
                  class="flex items-center justify-between bg-gray-700/60 rounded-xl px-3 py-2">
                  <div class="flex items-center gap-2">
                    <span class="text-sm text-gray-200">{{ m.name }}</span>
                    <span class="text-[10px] px-1.5 py-0.5 rounded"
                      :class="m.type === 'CreditCard' || m.type === 'DebitCard' ? 'bg-blue-900/50 text-blue-300' : m.type === 'Point' ? 'bg-yellow-900/50 text-yellow-300' : 'bg-gray-600/60 text-gray-300'">
                      {{ m.type === 'CreditCard' ? '신용카드' : m.type === 'DebitCard' ? '체크카드' : m.type === 'Point' ? '포인트' : '현금' }}
                    </span>
                  </div>
                  <button @click="settingDeleteMethod(m.id)" class="text-gray-500 hover:text-red-400 transition-colors p-1">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>
                  </button>
                </div>
              </div>
              <div class="flex gap-2">
                <input v-model="newMethodName" placeholder="결제수단명" @keyup.enter="settingAddMethod"
                  class="flex-1 bg-gray-700 text-gray-100 text-sm rounded-xl px-3 py-2 placeholder-gray-500 outline-none" />
                <select v-model="newMethodType" class="bg-gray-700 text-gray-300 text-xs rounded-xl px-2 py-2 outline-none">
                  <option value="Cash">현금</option>
                  <option value="CreditCard">신용카드</option>
                  <option value="DebitCard">체크카드</option>
                  <option value="Point">포인트</option>
                </select>
                <button @click="settingAddMethod" class="bg-blue-600 hover:bg-blue-500 text-white text-xs px-3 py-2 rounded-xl transition-colors">추가</button>
              </div>
            </div>
          </div>

          <!-- 카드 청구 설정 -->
          <div v-if="settingCardBilling.length > 0" class="bg-gray-800 rounded-2xl overflow-hidden">
            <button @click="toggleSection('cardBilling')" class="w-full flex items-center justify-between px-4 py-3.5">
              <div class="flex items-center gap-3">
                <span class="text-sm font-semibold text-gray-100">카드 청구 설정</span>
                <span class="text-xs text-gray-400">{{ settingCardBilling.length }}장</span>
              </div>
              <svg class="w-4 h-4 text-gray-400 transition-transform duration-200" :class="isSectionOpen('cardBilling') ? 'rotate-180' : ''" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/></svg>
            </button>
            <div v-show="isSectionOpen('cardBilling')" class="px-4 pb-4 border-t border-gray-700/50">
              <div class="space-y-3 mt-3">
                <div v-for="card in settingCardBilling" :key="card.paymentMethodId" class="bg-gray-700/60 rounded-xl p-3">
                  <p class="text-sm font-medium text-gray-200 mb-2">{{ card.name }}</p>
                  <div class="grid grid-cols-2 gap-3">
                    <div>
                      <label class="text-[10px] text-gray-400 block mb-1">정산일 (이용 마감)</label>
                      <div class="flex items-center gap-1.5">
                        <input v-model.number="card.cutoffDay" type="number" min="1" max="28"
                          class="w-14 bg-gray-700 text-gray-100 text-sm text-center rounded-lg px-2 py-1.5 outline-none" />
                        <span class="text-xs text-gray-400">일</span>
                      </div>
                    </div>
                    <div>
                      <label class="text-[10px] text-gray-400 block mb-1">결제일 (출금일)</label>
                      <div class="flex items-center gap-1.5">
                        <input v-model.number="card.dueDay" type="number" min="1" max="28"
                          class="w-14 bg-gray-700 text-gray-100 text-sm text-center rounded-lg px-2 py-1.5 outline-none" />
                        <span class="text-xs text-gray-400">일</span>
                      </div>
                    </div>
                  </div>
                  <p class="text-[10px] text-gray-500 mt-2">
                    전월 {{ card.cutoffDay + 1 > 28 ? 1 : card.cutoffDay + 1 }}일 ~ 당월 {{ card.cutoffDay }}일 이용분 → {{ card.dueDay }}일 출금
                  </p>
                </div>
              </div>
            </div>
          </div>

          <!-- 저축 수단 관리 -->
          <div class="bg-gray-800 rounded-2xl overflow-hidden">
            <button @click="toggleSection('savings')" class="w-full flex items-center justify-between px-4 py-3.5">
              <div class="flex items-center gap-3">
                <span class="text-sm font-semibold text-gray-100">저축 수단</span>
                <span class="text-xs text-gray-400">{{ settingSavingsMethods.length }}개</span>
              </div>
              <svg class="w-4 h-4 text-gray-400 transition-transform duration-200" :class="isSectionOpen('savings') ? 'rotate-180' : ''" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/></svg>
            </button>
            <div v-show="isSectionOpen('savings')" class="px-4 pb-4 border-t border-gray-700/50">
              <p class="text-[11px] text-gray-400 mt-3 mb-2">적금, 청약 등 저축 거래에 사용하는 계좌</p>
              <div class="space-y-1.5 mb-3">
                <div v-for="s in settingSavingsMethods" :key="s.id"
                  class="flex items-center justify-between bg-gray-700/60 rounded-xl px-3 py-2">
                  <span class="text-sm text-gray-200">{{ s.name }}</span>
                  <span v-if="s.isDefault" class="text-[10px] text-gray-500 bg-gray-700 px-1.5 py-0.5 rounded">기본</span>
                  <button v-else @click="settingDeleteSavings(s.id)" class="text-gray-500 hover:text-red-400 transition-colors p-1">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>
                  </button>
                </div>
              </div>
              <div class="flex gap-2">
                <input v-model="newSavingsName" placeholder="저축 수단명" @keyup.enter="settingAddSavings"
                  class="flex-1 bg-gray-700 text-gray-100 text-sm rounded-xl px-3 py-2 placeholder-gray-500 outline-none" />
                <button @click="settingAddSavings" class="bg-blue-600 hover:bg-blue-500 text-white text-xs px-3 py-2 rounded-xl transition-colors">추가</button>
              </div>
            </div>
          </div>

          <!-- 포인트 예산 -->
          <div class="bg-gray-800 rounded-2xl overflow-hidden">
            <button @click="toggleSection('pointBudget')" class="w-full flex items-center justify-between px-4 py-3.5">
              <div class="flex items-center gap-3">
                <span class="text-sm font-semibold text-gray-100">포인트 예산</span>
                <span class="text-xs text-gray-400">{{ settingPointBudgets.length }}개</span>
              </div>
              <svg class="w-4 h-4 text-gray-400 transition-transform duration-200" :class="isSectionOpen('pointBudget') ? 'rotate-180' : ''" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/></svg>
            </button>
            <div v-show="isSectionOpen('pointBudget')" class="px-4 pb-4 border-t border-gray-700/50">
              <p class="text-[11px] text-gray-400 mt-3 mb-2">복지포인트, 식대 등 별도 예산 잔액 추적</p>
              <div class="space-y-1.5 mb-3">
                <div v-for="p in settingPointBudgets" :key="p.id"
                  class="flex items-center justify-between bg-gray-700/60 rounded-xl px-3 py-2">
                  <div>
                    <span class="text-sm text-gray-200">{{ p.name }}</span>
                    <div class="text-[10px] text-gray-400 mt-0.5">
                      잔액 <span class="text-yellow-400">{{ p.remainingAmount.toLocaleString() }}원</span>
                      <span class="text-gray-500"> / {{ p.totalAmount.toLocaleString() }}원</span>
                    </div>
                  </div>
                  <button @click="settingDeletePoint(p.id)" class="text-gray-500 hover:text-red-400 transition-colors p-1">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>
                  </button>
                </div>
                <p v-if="settingPointBudgets.length === 0" class="text-xs text-gray-500 text-center py-2">등록된 예산 없음</p>
              </div>
              <div class="flex gap-2">
                <input v-model="newPointName" placeholder="예산명"
                  class="flex-1 bg-gray-700 text-gray-100 text-sm rounded-xl px-3 py-2 placeholder-gray-500 outline-none" />
                <input v-model="newPointAmount" placeholder="총액" @keyup.enter="settingAddPoint"
                  class="w-24 bg-gray-700 text-gray-100 text-sm rounded-xl px-3 py-2 placeholder-gray-500 outline-none" />
                <button @click="settingAddPoint" class="bg-blue-600 hover:bg-blue-500 text-white text-xs px-3 py-2 rounded-xl transition-colors">추가</button>
              </div>
            </div>
          </div>

          <div class="pb-4"></div>

        </div><!-- /스크롤 영역 -->
      </div>

    </div>

    <!-- 하단 탭바 -->
    <nav class="flex-shrink-0 bg-gray-900 border-t border-gray-700 flex z-50">
      <button @click="activePage = 'home'" :class="activePage === 'home' ? 'text-white' : 'text-gray-500'" class="flex-1 flex flex-col items-center py-2 text-xs gap-1">
        <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6"/></svg>
        가계부
      </button>
      <button @click="activePage = 'stats'" :class="activePage === 'stats' ? 'text-blue-400' : 'text-gray-500'" class="flex-1 flex flex-col items-center py-2 text-xs gap-1">
        <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z"/></svg>
        통계
      </button>
      <button @click="activePage = 'settings'" :class="activePage === 'settings' ? 'text-blue-400' : 'text-gray-500'" class="flex-1 flex flex-col items-center py-2 text-xs gap-1">
        <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z"/><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"/></svg>
        설정
      </button>
    </nav>

    <!-- ══ 할부 삭제 옵션 시트 ══ -->
    <div v-if="showInstDeleteSheet && instDeleteTx" class="fixed inset-0 bg-black/40 z-[70] flex items-end justify-center" @click.self="showInstDeleteSheet = false">
      <div class="bg-gray-800 rounded-t-2xl w-full max-w-lg pb-safe" @click.stop>
        <div class="flex justify-center pt-3 pb-2"><div class="w-10 h-1 bg-gray-600 rounded-full"/></div>
        <div class="px-5 pb-2">
          <p class="text-sm font-semibold text-gray-100">할부 삭제</p>
          <p class="text-xs text-gray-400 mt-0.5">{{ instDeleteTx.categoryName }} · {{ instDeleteTx.installmentSequence }}/{{ mockInstallmentMasters.find(m => m.id === instDeleteTx!.installmentMasterId)?.totalInstallments }}회차</p>
        </div>
        <div class="px-4 pb-5 space-y-2">
          <button @click="deleteInstAll" class="w-full py-3.5 bg-red-500 text-white rounded-xl text-sm font-semibold hover:bg-red-600 text-left px-4">
            전체 삭제
            <span class="block text-xs font-normal text-red-100 mt-0.5">모든 회차 데이터 삭제</span>
          </button>
          <button @click="deleteInstFromHere" class="w-full py-3.5 border border-red-200 text-red-500 rounded-xl text-sm font-semibold hover:bg-red-50 text-left px-4">
            이후 삭제
            <span class="block text-xs font-normal text-red-400 mt-0.5">{{ instDeleteTx.installmentSequence }}회차부터 이후 회차 삭제</span>
          </button>
          <button @click="deleteInstSingle" class="w-full py-3.5 border border-gray-700 text-gray-200 rounded-xl text-sm font-semibold hover:bg-gray-700 text-left px-4">
            단건 삭제
            <span class="block text-xs font-normal text-gray-400 mt-0.5">{{ instDeleteTx.installmentSequence }}회차만 삭제</span>
          </button>
          <button @click="showInstDeleteSheet = false" class="w-full py-2.5 text-sm text-gray-400">취소</button>
        </div>
      </div>
    </div>

    <!-- ══ 카드 청구 내역 모달 (Sprint 6 Step 1) ══ -->
    <div v-if="cardBillingModal" data-testid="card-billing-modal" class="fixed inset-0 bg-black/50 z-[60] flex items-center justify-center px-4" @click.self="cardBillingModal = null">
      <div class="bg-gray-800 rounded-2xl w-full max-w-lg flex flex-col" style="max-height: 70vh" @click.stop>
        <!-- 모달 헤더 -->
        <div class="flex-shrink-0 bg-gray-900 rounded-t-2xl px-4 pt-4 pb-3">
          <div class="flex items-center justify-between mb-1">
            <div class="flex items-center gap-2">
              <svg class="w-4 h-4 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                  d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z" />
              </svg>
              <span data-testid="card-billing-modal-title" class="text-sm font-semibold text-gray-100">{{ cardBillingModal.name }} 청구 내역</span>
            </div>
            <button data-testid="card-billing-modal-close" @click="cardBillingModal = null" class="p-1 text-gray-400 hover:text-white">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
          <div class="flex items-baseline justify-between">
            <span class="text-xs text-gray-400">
              {{ dayjs(cardBillingModal.periodFrom).format('M/D') }} ~ {{ dayjs(cardBillingModal.periodTo).format('M/D') }}
            </span>
            <span class="text-sm font-bold text-red-500">
              -{{ cardBillingTxList.filter(t => t.type === 'Expense').reduce((s, t) => s + t.amount, 0).toLocaleString() }}원
            </span>
          </div>
        </div>

        <!-- 카테고리 필터 칩 -->
        <div v-if="cardModalCategoryChips.length >= 2" class="flex-shrink-0 flex gap-2 px-4 py-2 overflow-x-auto border-b border-gray-700">
          <button
            @click="cardModalSelectedCategory = null"
            :class="cardModalSelectedCategory === null ? 'bg-blue-600 text-white' : 'bg-gray-700 text-gray-300'"
            class="flex-shrink-0 text-xs px-3 py-1.5 rounded-full font-medium"
          >전체</button>
          <button
            v-for="cat in cardModalCategoryChips"
            :key="cat.id"
            @click="cardModalSelectedCategory = cardModalSelectedCategory === cat.id ? null : cat.id"
            :class="cardModalSelectedCategory === cat.id ? 'bg-red-500 text-white' : 'bg-gray-700 text-gray-300'"
            class="flex-shrink-0 text-xs px-3 py-1.5 rounded-full font-medium"
          >{{ cat.name }}</button>
        </div>

        <!-- 거래 목록 -->
        <div class="flex-1 overflow-y-auto px-4 pt-3 pb-6">
          <div v-if="cardBillingTxListFiltered.length === 0" class="py-10 text-center text-gray-400 text-sm">
            해당 기간에 거래 내역이 없습니다
          </div>
          <template v-else>
            <div v-for="[date, txs] in txGroupByDate(cardBillingTxListFiltered)" :key="date" class="mb-4">
              <div class="flex items-center justify-between mb-1">
                <span class="text-xs font-semibold text-gray-400">{{ dayjs(date).format('MM/DD (ddd)') }}</span>
                <span class="text-xs flex items-center gap-1">
                  <template v-if="txs.filter(t => t.type === 'Income').reduce((s, t) => s + t.amount, 0) > 0"><span class="text-blue-400">+{{ txs.filter(t => t.type === 'Income').reduce((s, t) => s + t.amount, 0).toLocaleString() }}</span></template>
                  <template v-if="txs.filter(t => t.type === 'Income').reduce((s, t) => s + t.amount, 0) > 0 && txs.filter(t => t.type === 'Expense').reduce((s, t) => s + t.amount, 0) > 0"><span class="text-gray-500">/</span></template>
                  <template v-if="txs.filter(t => t.type === 'Expense').reduce((s, t) => s + t.amount, 0) > 0"><span class="text-red-500">-{{ txs.filter(t => t.type === 'Expense').reduce((s, t) => s + t.amount, 0).toLocaleString() }}</span></template>
                  <template v-if="(txs.filter(t => t.type === 'Income').reduce((s, t) => s + t.amount, 0) > 0 || txs.filter(t => t.type === 'Expense').reduce((s, t) => s + t.amount, 0) > 0) && txs.filter(t => t.type === 'Savings').reduce((s, t) => s + t.amount, 0) > 0"><span class="text-gray-500">/</span></template>
                  <template v-if="txs.filter(t => t.type === 'Savings').reduce((s, t) => s + t.amount, 0) > 0"><span class="text-emerald-500">-{{ txs.filter(t => t.type === 'Savings').reduce((s, t) => s + t.amount, 0).toLocaleString() }}</span></template>
                </span>
              </div>
              <div v-for="tx in txs" :key="tx.id"
                data-testid="card-billing-modal-tx-item"
                class="bg-gray-800 rounded-lg px-3 py-2 mb-1 flex items-center border border-gray-700"
                :class="!tx.isIncludedInTotal ? 'opacity-50' : ''"
              >
                <div class="flex-1 min-w-0">
                  <div class="flex items-center gap-1 flex-wrap">
                    <span class="text-xs font-medium truncate text-gray-100">{{ tx.categoryName }}</span>
                    <span v-if="!tx.isIncludedInTotal" class="text-[10px] bg-gray-700 text-gray-400 px-1 py-0.5 rounded">제외</span>
                    <span v-if="tx.installmentMasterId" class="text-[10px] bg-orange-900/20 text-orange-500 px-1 py-0.5 rounded">할부 {{ tx.installmentSequence }}/{{ mockInstallmentMasters.find(m => m.id === tx.installmentMasterId)?.totalInstallments }}회</span>
                    <span v-else-if="tx.recurringMasterId" class="text-[10px] bg-gray-700 text-blue-400 px-1 py-0.5 rounded">반복</span>
                  </div>
                  <div class="text-[11px] text-gray-400 mt-0.5">
                    <span v-if="tx.memo">{{ tx.memo }}</span>
                  </div>
                </div>
                <span :class="tx.type === 'Income' ? 'text-blue-400' : tx.type === 'Savings' ? 'text-emerald-500' : 'text-red-500'" class="font-semibold text-xs ml-2 mr-1 flex-shrink-0">
                  {{ txFormatAmount(tx.amount, tx.type) }}
                </span>
                <button @click.stop="onDeleteClick(tx)" class="flex-shrink-0 p-0.5 text-gray-300 hover:text-red-400">
                  <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>
                </button>
              </div>
            </div>
          </template>
        </div>
      </div>
    </div>

    <!-- ══ 반복 삭제 옵션 시트 ══ -->
    <div v-if="showRecurDeleteSheet && recurDeleteTx" class="fixed inset-0 bg-black/40 z-[70] flex items-end justify-center" @click.self="showRecurDeleteSheet = false">
      <div class="bg-gray-800 rounded-t-2xl w-full max-w-lg pb-safe" @click.stop>
        <div class="flex justify-center pt-3 pb-2"><div class="w-10 h-1 bg-gray-600 rounded-full"/></div>
        <div class="px-5 pb-2">
          <p class="text-sm font-semibold text-gray-100">반복 삭제</p>
          <p class="text-xs text-gray-400 mt-0.5">{{ recurDeleteTx.categoryName }} · 매월 {{ mockRecurringMasters.find(m => m.id === recurDeleteTx!.recurringMasterId)?.dayOfMonth }}일 반복</p>
        </div>
        <div class="px-4 pb-5 space-y-2">
          <button @click="deleteRecurAll" class="w-full py-3.5 bg-red-500 text-white rounded-xl text-sm font-semibold hover:bg-red-600 text-left px-4">
            전체 삭제
            <span class="block text-xs font-normal text-red-100 mt-0.5">등록된 모든 반복 거래 삭제 + 반복 중단</span>
          </button>
          <button @click="deleteRecurFromHere" class="w-full py-3.5 border border-red-200 text-red-500 rounded-xl text-sm font-semibold hover:bg-red-50 text-left px-4">
            이후 삭제
            <span class="block text-xs font-normal text-red-400 mt-0.5">{{ dayjs(recurDeleteTx.date).format('YYYY년 MM월') }}부터 이후 거래 삭제 + 반복 중단</span>
          </button>
          <button @click="deleteRecurSingle" class="w-full py-3.5 border border-gray-700 text-gray-200 rounded-xl text-sm font-semibold hover:bg-gray-700 text-left px-4">
            단건 삭제
            <span class="block text-xs font-normal text-gray-400 mt-0.5">이번 달 거래만 삭제 (반복 유지)</span>
          </button>
          <button @click="showRecurDeleteSheet = false" class="w-full py-2.5 text-sm text-gray-400">취소</button>
        </div>
      </div>
    </div>

    <!-- ══ 가계부 내역 추가/수정 모달 ══ -->
    <div v-if="showModal" class="fixed inset-0 bg-black/50 z-[60] flex items-center justify-center px-4" @click.self="showModal = false">
      <div class="bg-gray-800 rounded-2xl w-full max-w-lg flex flex-col" style="max-height:90dvh">
        <div class="flex-shrink-0 flex items-center justify-between px-4 py-4 border-b border-gray-700">
          <h2 class="font-semibold text-gray-100">{{ editingTxId !== null ? '가계부 내역 수정' : '가계부 내역 추가' }}</h2>
          <button @click="showModal = false" class="text-gray-400 hover:text-gray-200">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>
          </button>
        </div>
        <div class="flex-1 overflow-y-auto">

          <!-- 지출/수입 탭 -->
          <div class="px-4 pt-4 pb-3">
            <div class="flex rounded-xl bg-gray-700 p-1">
              <button @click="formType = 'Expense'" :class="formType === 'Expense' ? 'bg-white text-red-500 shadow-sm' : 'text-gray-400'" class="flex-1 py-2 text-sm font-medium rounded-lg transition-all">지출</button>
              <button @click="formType = 'Income'"  :class="formType === 'Income'  ? 'bg-white text-blue-600 shadow-sm' : 'text-gray-400'" class="flex-1 py-2 text-sm font-medium rounded-lg transition-all">수입</button>
              <button @click="formType = 'Savings'" :class="formType === 'Savings' ? 'bg-white text-emerald-500 shadow-sm' : 'text-gray-400'" class="flex-1 py-2 text-sm font-medium rounded-lg transition-all">저축</button>
            </div>
          </div>

          <!-- 기본 정보 -->
          <div class="px-4 py-3 space-y-3 border-t border-gray-700">
            <p class="text-xs font-medium text-gray-400 uppercase tracking-wide">기본 정보</p>

            <!-- 금액 -->
            <div>
              <div class="flex items-center justify-between mb-1">
                <label class="text-xs text-gray-400">금액</label>
                <!-- 할부 토글 (신규, 지출만) / 할부 배지 (수정, 지출만) -->
                <button v-if="editingTxId === null && formType === 'Expense'" @click="formIsInstallment = !formIsInstallment"
                  :class="formIsInstallment ? 'bg-orange-500 text-white' : 'bg-gray-700 text-gray-400'"
                  class="flex items-center gap-1 text-xs px-2.5 py-1 rounded-full font-medium transition-colors">
                  <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z"/></svg>
                  할부
                </button>
                <span v-else-if="formIsInstallment && formType === 'Expense'" class="flex items-center gap-1 text-xs px-2.5 py-1 rounded-full font-medium bg-orange-100 text-orange-600">
                  <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z"/></svg>
                  할부
                </span>
              </div>
              <input :value="formAmount" @input="onFormAmountInput" type="text" inputmode="numeric" placeholder="0" class="w-full border border-gray-700 rounded-xl px-3 py-2.5 text-lg font-semibold focus:outline-none focus:border-blue-400 bg-gray-700 text-gray-100" />
            </div>

            <!-- 할부 개월수 + 미리보기 -->
            <template v-if="formIsInstallment && formType === 'Expense'">
              <div>
                <label class="block text-xs text-gray-400 mb-1">총 개월수</label>
                <div class="flex items-center gap-2">
                  <input v-model.number="formInstallmentMonths" type="number" min="2" max="60" placeholder="12" class="w-24 border border-gray-700 rounded-xl px-3 py-2.5 text-sm text-center focus:outline-none focus:border-orange-400 bg-gray-700 text-gray-100" />
                  <span class="text-sm text-gray-400">개월</span>
                </div>
              </div>
              <div v-if="installmentPreview" class="bg-orange-900/20 rounded-xl px-3 py-2.5 space-y-1">
                <div class="flex justify-between text-sm"><span class="text-gray-300">1회차</span><span class="font-semibold text-orange-700">{{ installmentPreview.firstMonthAmount.toLocaleString() }}원</span></div>
                <div class="flex justify-between text-sm"><span class="text-gray-300">2회차 이후</span><span class="font-semibold text-orange-700">{{ installmentPreview.monthlyAmount.toLocaleString() }}원</span></div>
              </div>
            </template>

            <!-- 날짜 -->
            <div>
              <label class="block text-xs text-gray-400 mb-1">날짜</label>
              <input v-model="formDate" type="date" class="w-full border border-gray-700 rounded-xl px-3 py-2.5 text-sm focus:outline-none focus:border-blue-400 bg-gray-700 text-gray-100" />
            </div>
          </div>

          <!-- 분류 -->
          <div class="px-4 py-3 space-y-3 border-t border-gray-700">
            <p class="text-xs font-medium text-gray-400 uppercase tracking-wide">분류</p>
            <div>
              <label class="block text-xs text-gray-400 mb-1">카테고리</label>
              <select v-model="formCategoryId" class="w-full border border-gray-700 rounded-xl px-3 py-2.5 text-sm focus:outline-none focus:border-blue-400 bg-gray-700 text-gray-100">
                <option :value="0">선택하세요</option>
                <option v-for="c in formCategories" :key="c.id" :value="c.id">{{ c.name }}</option>
              </select>
            </div>
            <div v-if="formType === 'Expense'">
              <label class="block text-xs text-gray-400 mb-1">결제수단</label>
              <select v-model="formPaymentMethodId" class="w-full border border-gray-700 rounded-xl px-3 py-2.5 text-sm focus:outline-none focus:border-blue-400 bg-gray-700 text-gray-100">
                <option :value="0">선택하세요</option>
                <option v-for="m in MOCK_PAYMENT_METHODS" :key="m.id" :value="m.id">{{ m.name }}<template v-if="m.type === 'Point'"> (잔액 {{ m.remainingAmount?.toLocaleString() }}원)</template></option>
              </select>
            </div>
            <div v-if="formType === 'Savings'">
              <label class="block text-xs text-gray-400 mb-1">저축 수단</label>
              <select v-model="formSavingsMethodId" class="w-full border border-gray-700 rounded-xl px-3 py-2.5 text-sm focus:outline-none focus:border-emerald-400 bg-gray-700 text-gray-100">
                <option :value="0">선택하세요</option>
                <option v-for="m in MOCK_SAVINGS_METHODS" :key="m.id" :value="m.id">{{ m.name }}</option>
              </select>
            </div>
          </div>

          <!-- 부가 정보 -->
          <div class="px-4 py-3 space-y-3 border-t border-gray-700">
            <p class="text-xs font-medium text-gray-400 uppercase tracking-wide">부가 정보</p>
            <input v-model="formMemo" type="text" placeholder="메모 (선택)" class="w-full border border-gray-700 rounded-xl px-3 py-2.5 text-sm focus:outline-none focus:border-blue-400 bg-gray-700 text-gray-100" />

            <!-- 반복 토글 -->
            <div>
              <label class="flex items-center gap-3 cursor-pointer">
                <div @click="formIsRecurring = !formIsRecurring" :class="formIsRecurring ? 'bg-teal-500' : 'bg-gray-600'" class="w-11 h-6 rounded-full transition-colors relative flex-shrink-0">
                  <div :class="formIsRecurring ? 'translate-x-5' : 'translate-x-0.5'" class="absolute top-0.5 w-5 h-5 bg-white rounded-full shadow transition-transform"/>
                </div>
                <span class="text-sm text-gray-200">매월 반복</span>
              </label>
              <!-- 반복 ON 시 펼쳐지는 필드 -->
              <template v-if="formIsRecurring">
                <div class="mt-3 flex items-center gap-2">
                  <span class="text-sm text-gray-300">매월</span>
                  <input v-model.number="formRecurringDay" type="number" min="1" max="28"
                    class="w-16 border border-gray-700 rounded-xl px-3 py-2 text-sm text-center focus:outline-none focus:border-teal-400 bg-gray-700 text-gray-100" />
                  <span class="text-sm text-gray-300">일 반복</span>
                </div>
                <div class="mt-2">
                  <label class="block text-xs text-gray-400 mb-1">종료일 <span class="text-gray-400">(없으면 무기한)</span></label>
                  <input v-model="formRecurringEndDate" type="date"
                    class="w-full border border-gray-700 rounded-xl px-3 py-2.5 text-sm focus:outline-none focus:border-teal-400 bg-gray-700 text-gray-100" />
                </div>
              </template>
            </div>

            <label class="flex items-center gap-3 cursor-pointer">
              <div @click="formIsIncluded = !formIsIncluded" :class="formIsIncluded ? 'bg-blue-600' : 'bg-gray-600'" class="w-11 h-6 rounded-full transition-colors relative">
                <div :class="formIsIncluded ? 'translate-x-5' : 'translate-x-0.5'" class="absolute top-0.5 w-5 h-5 bg-white rounded-full shadow transition-transform"/>
              </div>
              <span class="text-sm text-gray-200">합산에 포함</span>
            </label>
          </div>
        </div>

        <!-- 저장 버튼 -->
        <div class="flex-shrink-0 px-4 py-4 border-t border-gray-700">
          <button @click="formSaveMock" :disabled="!formIsValid && !formSaved"
            :class="formSaved ? 'bg-green-500' : !formIsValid ? 'bg-gray-700 text-gray-400 cursor-not-allowed' : (formIsInstallment && formType === 'Expense') ? 'bg-orange-500 hover:bg-orange-600' : 'bg-blue-600 hover:bg-blue-700'"
            class="w-full text-white py-3 rounded-xl font-semibold text-sm transition-colors">
            {{ formSaved ? '✓ 저장 완료' : editingTxId !== null ? '수정 완료' : (formIsInstallment && formType === 'Expense') ? '할부 등록' : formIsRecurring ? '반복 등록' : '추가' }}
          </button>
        </div>
      </div>
    </div>


  </div>
</template>

<style scoped>
/* 목업 내 스크롤 영역 — 다크 테마 얇은 스크롤바 */
* {
  scrollbar-width: thin;
  scrollbar-color: #4B5563 transparent;
}
*::-webkit-scrollbar {
  width: 4px;
  height: 4px;
}
*::-webkit-scrollbar-track {
  background: transparent;
}
*::-webkit-scrollbar-thumb {
  background-color: #4B5563;
  border-radius: 9999px;
}
*::-webkit-scrollbar-thumb:hover {
  background-color: #6B7280;
}
</style>
