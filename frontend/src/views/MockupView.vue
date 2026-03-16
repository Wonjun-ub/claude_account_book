<script setup lang="ts">
// DEV-only 목업 페이지 — 백엔드 미접근, mock 데이터로만 동작
// 실제 앱과 동일한 UI 구조로 신규 기능 UI를 확인한 후 실서비스에 이관합니다.

import { ref, computed, watch, onMounted } from 'vue'
import dayjs from 'dayjs'
import { calcMockInstallment } from '@/mocks/installment.mock'
import { getMonthPeriod } from '@/utils/monthPeriod'
import UpcomingWidget from '@/components/UpcomingWidget.vue'
import type { CardFilter } from '@/components/UpcomingWidget.vue'

const activePage = ref('home')

// ── 인터페이스 ──────────────────────────────────────────────────────────────

interface MockTxRecord {
  id: number
  amount: number
  date: string
  type: 'Income' | 'Expense'
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
  type: 'Income' | 'Expense'
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
  { id: 1, name: '식비',   type: 'Expense' as const, isDefault: true },
  { id: 2, name: '교통',   type: 'Expense' as const, isDefault: true },
  { id: 3, name: '쇼핑',   type: 'Expense' as const, isDefault: false },
  { id: 4, name: '의료',   type: 'Expense' as const, isDefault: false },
  { id: 5, name: '급여',   type: 'Income'  as const, isDefault: true },
  { id: 6, name: '부업',   type: 'Income'  as const, isDefault: false },
]

const MOCK_PAYMENT_METHODS = [
  { id: 1, name: '신한카드',   type: 'Card'  as const, remainingAmount: undefined as number | undefined },
  { id: 2, name: '현금',       type: 'Cash'  as const, remainingAmount: undefined },
  { id: 3, name: '네이버페이', type: 'Point' as const, remainingAmount: 45000 },
  { id: 4, name: '국민카드',   type: 'Card'  as const, remainingAmount: undefined as number | undefined },
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
  // 2026-03 국민카드 거래 (청구 기간 02/16~03/15 내 샘플)
  { id: 7,   amount: 89000,   date: '2026-03-12', type: 'Expense', categoryId: 3, categoryName: '쇼핑',  paymentMethodId: 4, paymentMethodName: '국민카드',   memo: '쿠팡',       isIncludedInTotal: true,  installmentMasterId: undefined, installmentSequence: undefined, recurringMasterId: undefined },
  { id: 8,   amount: 32000,   date: '2026-03-07', type: 'Expense', categoryId: 1, categoryName: '식비',  paymentMethodId: 4, paymentMethodName: '국민카드',   memo: '편의점',     isIncludedInTotal: true,  installmentMasterId: undefined, installmentSequence: undefined, recurringMasterId: undefined },
  { id: 9,   amount: 21000,   date: '2026-02-25', type: 'Expense', categoryId: 2, categoryName: '교통',  paymentMethodId: 4, paymentMethodName: '국민카드',   memo: 'KTX',        isIncludedInTotal: true,  installmentMasterId: undefined, installmentSequence: undefined, recurringMasterId: undefined },
  // 2026-04
  { id: 401, amount: 3000000, date: '2026-04-10', type: 'Income',  categoryId: 5, categoryName: '급여',  paymentMethodId: 2, paymentMethodName: '현금',       memo: '4월 급여',   isIncludedInTotal: true,  installmentMasterId: undefined, installmentSequence: undefined, recurringMasterId: 1 },
  { id: 402, amount: 42000,   date: '2026-04-05', type: 'Expense', categoryId: 1, categoryName: '식비',  paymentMethodId: 1, paymentMethodName: '신한카드',   memo: undefined,    isIncludedInTotal: true,  installmentMasterId: undefined, installmentSequence: undefined, recurringMasterId: undefined },
  { id: 403, amount: 100000,  date: '2026-04-08', type: 'Expense', categoryId: 3, categoryName: '쇼핑',  paymentMethodId: 1, paymentMethodName: '신한카드',   memo: '의류 2/3',   isIncludedInTotal: true,  installmentMasterId: 1,         installmentSequence: 2,        recurringMasterId: undefined },
  // 2026-05
  { id: 501, amount: 3000000, date: '2026-05-10', type: 'Income',  categoryId: 5, categoryName: '급여',  paymentMethodId: 2, paymentMethodName: '현금',       memo: '5월 급여',   isIncludedInTotal: true,  installmentMasterId: undefined, installmentSequence: undefined, recurringMasterId: 1 },
  { id: 502, amount: 300000,  date: '2026-05-05', type: 'Expense', categoryId: 4, categoryName: '의료',  paymentMethodId: 1, paymentMethodName: '신한카드',   memo: '건강검진',   isIncludedInTotal: true,  installmentMasterId: undefined, installmentSequence: undefined, recurringMasterId: undefined },
  { id: 503, amount: 100000,  date: '2026-05-08', type: 'Expense', categoryId: 3, categoryName: '쇼핑',  paymentMethodId: 1, paymentMethodName: '신한카드',   memo: '의류 3/3',   isIncludedInTotal: true,  installmentMasterId: 1,         installmentSequence: 3,        recurringMasterId: undefined },
]

const MOCK_INSTALLMENT_MASTERS_INITIAL: MockInstallmentMaster[] = [
  { id: 1, totalAmount: 300000, monthlyAmount: 100000, firstMonthAmount: 100000, totalInstallments: 3, startDate: '2026-03-08', categoryId: 3, categoryName: '쇼핑', paymentMethodId: 1, paymentMethodName: '신한카드', memo: '의류 할부' },
]

const MOCK_RECURRING_MASTERS_INITIAL: MockRecurringMaster[] = [
  { id: 1, amount: 3000000, type: 'Income', dayOfMonth: 10, startDate: '2025-12-10', endDate: undefined, isActive: true, categoryId: 5, categoryName: '급여', paymentMethodId: 2, paymentMethodName: '현금', memo: '월급' },
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
  txSelectedCategory.value = null
}
function nextMonth() {
  if (mockYear.value === MOCK_NAV_MAX.year && mockMonth.value === MOCK_NAV_MAX.month) return
  if (mockMonth.value === 12) { mockYear.value++; mockMonth.value = 1 }
  else mockMonth.value++
  txSelectedCategory.value = null
}
const canGoPrev = computed(() => !(mockYear.value === MOCK_NAV_MIN.year && mockMonth.value === MOCK_NAV_MIN.month))
const canGoNext = computed(() => !(mockYear.value === MOCK_NAV_MAX.year && mockMonth.value === MOCK_NAV_MAX.month))

// ── localStorage 영속성 ───────────────────────────────────────────────────────

const LS_TX   = 'mockup_transactions'
const LS_INST = 'mockup_installment_masters'
const LS_RECR = 'mockup_recurring_masters'
const LS_SKIP = 'mockup_recurring_skipped'  // 반복 월별 스킵 키: `{masterId}-{YYYY}-{MM}`

function saveAll() {
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
  try {
    const t = localStorage.getItem(LS_TX);   if (t) txTransactions.value        = JSON.parse(t)
    const i = localStorage.getItem(LS_INST); if (i) mockInstallmentMasters.value = JSON.parse(i)
    const r = localStorage.getItem(LS_RECR); if (r) mockRecurringMasters.value   = JSON.parse(r)
    const s = localStorage.getItem(LS_SKIP); if (s) recurSkippedSet.value        = JSON.parse(s)
  } catch { /* 손상 시 기본값 유지 */ }
})

// ── 홈: 거래 목록 ─────────────────────────────────────────────────────────────

const txSearch          = ref('')
const txSelectedCategory = ref<number | null>(null)
const showModal          = ref(false)


const txMonthFiltered = computed(() => {
  const { start, end } = getMonthPeriod(mockYear.value, mockMonth.value, settingMonthStartDay.value)
  return txTransactions.value.filter(t => t.date >= start && t.date <= end)
})

const mockSummary = computed(() => {
  const included     = txMonthFiltered.value.filter(t => t.isIncludedInTotal)
  const incomeList   = included.filter(t => t.type === 'Income')
  const expenseList  = included.filter(t => t.type === 'Expense')
  const totalIncome  = incomeList.reduce((s, t) => s + t.amount, 0)
  const totalExpense = expenseList.reduce((s, t) => s + t.amount, 0)
  return { totalIncome, totalExpense, balance: totalIncome - totalExpense,
    incomeCount: incomeList.length, expenseCount: expenseList.length }
})

// ── 반복 예정 배너 ────────────────────────────────────────────────────────────

const showPendingBanner = ref(false)
const activeCardFilter = ref<CardFilter | null>(null)

// 이번 월에 아직 등록되지 않은 반복 마스터 목록
const recurringPending = computed(() => {
  const { start, end } = getMonthPeriod(mockYear.value, mockMonth.value, settingMonthStartDay.value)
  const monthKey = (id: number) => `${id}-${mockYear.value}-${String(mockMonth.value).padStart(2, '0')}`
  return mockRecurringMasters.value.filter(master => {
    if (!master.isActive) return false
    if (master.startDate > end) return false
    if (master.endDate && master.endDate < start) return false
    if (recurSkippedSet.value.includes(monthKey(master.id))) return false
    return !txTransactions.value.some(t =>
      t.recurringMasterId === master.id && t.date >= start && t.date <= end
    )
  })
})

const pendingSummary = computed(() => {
  const income  = recurringPending.value.filter(m => m.type === 'Income').reduce((s, m) => s + m.amount, 0)
  const expense = recurringPending.value.filter(m => m.type === 'Expense').reduce((s, m) => s + m.amount, 0)
  return { income, expense }
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
  // 카드 필터 활성 시: 월 범위 무시, 카드 청구 기간 전체에서 필터링
  if (activeCardFilter.value) {
    const { id, periodFrom, periodTo } = activeCardFilter.value
    return txTransactions.value.filter(t =>
      t.paymentMethodId === id && t.date >= periodFrom && t.date <= periodTo
    )
  }
  let list = txMonthFiltered.value
  if (txSearch.value) list = list.filter(t => t.memo?.includes(txSearch.value) || t.categoryName.includes(txSearch.value))
  if (txSelectedCategory.value !== null) list = list.filter(t => t.categoryId === txSelectedCategory.value)
  return list
})

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
const formType              = ref<'Expense' | 'Income'>('Expense')
const formAmount            = ref('')
const formDate              = ref(dayjs().format('YYYY-MM-DD'))
const formCategoryId        = ref(0)
const formPaymentMethodId   = ref(0)
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
  return true
})

const installmentPreview = computed(() => {
  const total  = parseInt(formAmount.value.replace(/,/g, ''), 10) || 0
  const months = formInstallmentMonths.value
  if (!total || !months || months < 2) return null
  return calcMockInstallment(total, months)
})

// 탭 전환 시 타입별 카테고리 선택값 저장/복원
const savedCatByType: { Expense: number; Income: number } = { Expense: 0, Income: 0 }
watch(formType, (newType, oldType) => {
  savedCatByType[oldType as 'Expense' | 'Income'] = formCategoryId.value
  formCategoryId.value = savedCatByType[newType as 'Expense' | 'Income']

  if (editingTxId.value === null) {
    // 신규 추가 모드: 수입 전환 시 할부 상태 리셋
    if (newType === 'Income') {
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
  formType.value = 'Expense'
  formAmount.value = ''
  formDate.value = dayjs().format('YYYY-MM-DD')
  formCategoryId.value = 0
  formPaymentMethodId.value = 0
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
  // 수입에는 결제수단 없음
  const methodName = formType.value === 'Expense'
    ? (MOCK_PAYMENT_METHODS.find(m => m.id === formPaymentMethodId.value)?.name ?? '기타')
    : ''

  if (formIsRecurring.value && editingTxId.value === null) {
    // ── 반복 신규: 원부 생성 + 현재 날짜 거래 1건 등록
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
      paymentMethodId: formType.value === 'Expense' ? formPaymentMethodId.value : 0,
      paymentMethodName: methodName,
      memo: formMemo.value || undefined,
    })
    txTransactions.value.push({
      id: masterId + 1,
      amount,
      date: formDate.value,
      type: formType.value,
      categoryId: formCategoryId.value,
      categoryName: catName,
      paymentMethodId: formType.value === 'Expense' ? formPaymentMethodId.value : 0,
      paymentMethodName: methodName,
      memo: formMemo.value || undefined,
      isIncludedInTotal: formIsIncluded.value,
      installmentMasterId: undefined,
      installmentSequence: undefined,
      recurringMasterId: masterId,
    })
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
        paymentMethodId: formType.value === 'Expense' ? formPaymentMethodId.value : 0,
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
      paymentMethodId: formType.value === 'Expense' ? formPaymentMethodId.value : 0,
      paymentMethodName: methodName,
      memo: formMemo.value || undefined,
      isIncludedInTotal: formIsIncluded.value,
      installmentMasterId: undefined,
      installmentSequence: undefined,
      recurringMasterId: undefined,
    })
  }

  saveAll()
  formSaved.value = true
  setTimeout(() => { showModal.value = false; formSaved.value = false; editingTxId.value = null }, 1000)
}

// ── 거래 수정/삭제 ────────────────────────────────────────────────────────────

function openEdit(tx: MockTxRecord) {
  editingTxId.value = tx.id
  formType.value = tx.type
  formDate.value = tx.date
  formCategoryId.value = tx.categoryId
  formPaymentMethodId.value = tx.paymentMethodId
  formMemo.value = tx.memo ?? ''
  formIsIncluded.value = tx.isIncludedInTotal
  formSaved.value = false
  savedCatByType.Expense = tx.type === 'Expense' ? tx.categoryId : 0
  savedCatByType.Income  = tx.type === 'Income'  ? tx.categoryId : 0

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

// 배너 항목 X 클릭 → 기존 삭제 시트 재사용 (synthetic tx 생성)
function onPendingDeleteClick(master: MockRecurringMaster) {
  recurDeleteTx.value = {
    id: -1,  // 실제 거래 없음을 표시
    amount: master.amount,
    date: `${mockYear.value}-${String(mockMonth.value).padStart(2, '0')}-${String(master.dayOfMonth).padStart(2, '0')}`,
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
const settingPaymentMethods = ref<{ id: number; name: string; type: 'Cash' | 'Card' | 'Point'; remainingAmount: number | undefined }[]>(
  MOCK_PAYMENT_METHODS.map(m => ({ ...m }))
)
</script>

<template>
  <div class="h-full flex flex-col bg-gray-50">

    <!-- DEV 배너 -->
    <div class="flex-shrink-0 bg-orange-500 text-white text-xs text-center py-1 font-medium tracking-wide">
      DEV MOCKUP — 로컬 개발 전용 · 백엔드 미접근
    </div>

    <div class="flex-1 overflow-hidden flex flex-col">

      <!-- ══ 홈 ══ -->
      <div v-show="activePage === 'home'" class="flex-1 overflow-hidden flex flex-col max-w-lg mx-auto w-full relative">

        <!-- 월 헤더 -->
        <div class="flex-shrink-0 bg-blue-600 text-white px-4 pt-6 pb-5">
          <div class="flex items-center justify-between mb-4">
            <button @click="prevMonth" :disabled="!canGoPrev" class="p-1 rounded-full hover:bg-blue-500 disabled:opacity-30 disabled:cursor-not-allowed">
              <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"/></svg>
            </button>
            <div class="text-center">
              <div class="text-lg font-semibold">{{ mockMonthLabel }}</div>
              <div v-if="mockMonthRange" class="text-xs text-blue-200 mt-0.5">{{ mockMonthRange }}</div>
            </div>
            <button @click="nextMonth" :disabled="!canGoNext" class="p-1 rounded-full hover:bg-blue-500 disabled:opacity-30 disabled:cursor-not-allowed">
              <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"/></svg>
            </button>
          </div>
          <div class="grid grid-cols-3 gap-2 text-center">
            <div class="bg-blue-500 rounded-xl p-3">
              <div class="text-blue-200 text-xs mb-1">수입</div>
              <div class="font-bold text-sm">{{ mockSummary.totalIncome.toLocaleString() }}원</div>
              <div class="text-blue-300 text-xs mt-0.5">{{ mockSummary.incomeCount }}건</div>
            </div>
            <div class="bg-blue-500 rounded-xl p-3">
              <div class="text-blue-200 text-xs mb-1">지출</div>
              <div class="font-bold text-sm">{{ mockSummary.totalExpense.toLocaleString() }}원</div>
              <div class="text-blue-300 text-xs mt-0.5">{{ mockSummary.expenseCount }}건</div>
            </div>
            <div class="bg-blue-500 rounded-xl p-3">
              <div class="text-blue-200 text-xs mb-1">잔액</div>
              <div class="font-bold text-sm">{{ mockSummary.balance.toLocaleString() }}원</div>
              <div class="text-blue-300 text-xs mt-0.5">합산 기준</div>
            </div>
          </div>
        </div>

        <!-- 검색 -->
        <div class="flex-shrink-0 px-4 py-2 bg-white border-b border-gray-100 flex gap-2">
          <input v-model="txSearch" type="text" placeholder="메모/카테고리 검색..." class="flex-1 text-sm border border-gray-200 rounded-lg px-3 py-1.5 focus:outline-none focus:border-blue-400" />
        </div>

        <!-- 반복 예정 배너 -->
        <div v-if="recurringPending.length > 0" class="flex-shrink-0 bg-teal-50 border-b border-teal-100">
          <button @click="showPendingBanner = !showPendingBanner" class="w-full px-4 py-2.5 flex items-center justify-between">
            <div class="flex items-center gap-2 min-w-0">
              <svg class="w-3.5 h-3.5 text-teal-600 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"/></svg>
              <span class="text-xs font-semibold text-teal-700">반복 예정 {{ recurringPending.length }}건</span>
              <span class="text-xs text-teal-600 truncate">
                <template v-if="pendingSummary.income > 0">수입 +{{ pendingSummary.income.toLocaleString() }}원</template>
                <template v-if="pendingSummary.income > 0 && pendingSummary.expense > 0"> · </template>
                <template v-if="pendingSummary.expense > 0">지출 -{{ pendingSummary.expense.toLocaleString() }}원</template>
              </span>
            </div>
            <svg :class="showPendingBanner ? 'rotate-180' : ''" class="w-4 h-4 text-teal-500 flex-shrink-0 transition-transform" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/></svg>
          </button>
          <div v-if="showPendingBanner" class="px-4 pb-3 space-y-2">
            <div v-for="master in recurringPending" :key="master.id"
              class="flex items-center justify-between bg-white rounded-xl px-3 py-2.5 border border-teal-100">
              <div class="flex items-center gap-2 min-w-0">
                <div class="w-1.5 h-1.5 rounded-full flex-shrink-0" :class="master.type === 'Income' ? 'bg-blue-400' : 'bg-red-400'"/>
                <span class="text-sm text-gray-700 truncate">{{ master.categoryName }}</span>
                <span class="text-xs text-gray-400 flex-shrink-0">매월 {{ master.dayOfMonth }}일</span>
                <span v-if="master.memo" class="text-xs text-gray-400 truncate">· {{ master.memo }}</span>
              </div>
              <div class="flex items-center gap-1 ml-2 flex-shrink-0">
                <span :class="master.type === 'Income' ? 'text-blue-600' : 'text-red-500'" class="text-sm font-semibold">
                  {{ master.type === 'Income' ? '+' : '-' }}{{ master.amount.toLocaleString() }}원
                </span>
                <button @click="onPendingDeleteClick(master)" class="p-1 text-gray-300 hover:text-red-400">
                  <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- 카드 결제 예정 배너 (Sprint 6 Step 1) -->
        <UpcomingWidget @filter-card="activeCardFilter = $event" />

        <!-- 카드 필터 칩 (드릴다운 활성 시) -->
        <div v-if="activeCardFilter" class="flex-shrink-0 px-4 py-2 bg-white border-b border-gray-100 flex items-center gap-2">
          <span class="text-xs text-gray-400">필터</span>
          <button
            class="flex items-center gap-1.5 text-xs bg-blue-50 text-blue-700 border border-blue-200 px-3 py-1.5 rounded-full font-medium"
            @click="activeCardFilter = null"
          >
            <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z" />
            </svg>
            {{ activeCardFilter.name }} 청구 내역
            <svg class="w-3 h-3 text-blue-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
          <span class="text-xs text-gray-400">
            {{ dayjs(activeCardFilter.periodFrom).format('M/D') }}~{{ dayjs(activeCardFilter.periodTo).format('M/D') }}
          </span>
        </div>

        <!-- 카테고리 칩 -->
        <div v-if="txCategoryChips.length > 0" class="flex-shrink-0 flex gap-2 px-4 py-2 overflow-x-auto bg-white border-b border-gray-100">
          <button @click="txSelectedCategory = null" :class="txSelectedCategory === null ? 'bg-blue-600 text-white' : 'bg-gray-100 text-gray-600'" class="flex-shrink-0 text-xs px-3 py-1.5 rounded-full font-medium">전체</button>
          <button v-for="cat in txCategoryChips" :key="cat.id"
            @click="txSelectedCategory = txSelectedCategory === cat.id ? null : cat.id"
            :class="txSelectedCategory === cat.id ? (cat.type === 'Income' ? 'bg-blue-600 text-white' : 'bg-red-500 text-white') : 'bg-gray-100 text-gray-600'"
            class="flex-shrink-0 text-xs px-3 py-1.5 rounded-full font-medium">{{ cat.name }}</button>
        </div>

        <!-- 거래 목록 — FAB 영역 확보를 위해 하단 패딩 추가 -->
        <div class="flex-1 overflow-y-auto px-4 pt-2 pb-20">
          <div v-if="txFiltered.length === 0" class="py-10 text-center text-gray-400 text-sm">거래 내역이 없습니다</div>
          <template v-else>
            <div v-for="[date, txs] in txGroupByDate(txFiltered)" :key="date" class="mb-4">
              <div class="flex items-center justify-between mb-1">
                <span class="text-xs font-semibold text-gray-500">{{ dayjs(date).format('MM/DD (ddd)') }}</span>
                <span class="text-xs text-gray-400">
                  <template v-if="txs.filter(t=>t.type==='Income').reduce((s,t)=>s+t.amount,0) > 0">+{{ txs.filter(t=>t.type==='Income').reduce((s,t)=>s+t.amount,0).toLocaleString() }}</template>
                  <template v-if="txs.filter(t=>t.type==='Expense').reduce((s,t)=>s+t.amount,0) > 0"> -{{ txs.filter(t=>t.type==='Expense').reduce((s,t)=>s+t.amount,0).toLocaleString() }}</template>
                </span>
              </div>
              <div v-for="tx in txs" :key="tx.id" @click="openEdit(tx)" class="bg-white rounded-xl p-3 mb-2 flex items-center shadow-sm cursor-pointer active:bg-gray-50" :class="!tx.isIncludedInTotal ? 'opacity-50' : ''">
                <div class="flex-1 min-w-0">
                  <div class="flex items-center gap-1.5 flex-wrap">
                    <span class="text-sm font-medium truncate">{{ tx.categoryName }}</span>
                    <span v-if="!tx.isIncludedInTotal" class="text-xs bg-gray-100 text-gray-500 px-1.5 py-0.5 rounded">제외</span>
                    <span v-if="tx.installmentMasterId" class="text-xs bg-orange-50 text-orange-500 px-1.5 py-0.5 rounded">할부 {{ tx.installmentSequence }}회</span>
                    <span v-else-if="tx.recurringMasterId" class="text-xs bg-blue-50 text-blue-500 px-1.5 py-0.5 rounded">반복</span>
                  </div>
                  <div class="text-xs text-gray-400 mt-0.5">
                    <template v-if="tx.type === 'Expense'">{{ tx.paymentMethodName }}<span v-if="tx.memo"> · {{ tx.memo }}</span></template>
                    <span v-else-if="tx.memo">{{ tx.memo }}</span>
                  </div>
                </div>
                <span :class="tx.type === 'Income' ? 'text-blue-600' : 'text-red-500'" class="font-semibold text-sm ml-2 mr-1">{{ txFormatAmount(tx.amount, tx.type) }}</span>
                <button @click.stop="onDeleteClick(tx)" class="flex-shrink-0 p-1 text-gray-300 hover:text-red-400">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>
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

    </div>

    <!-- 하단 탭바 -->
    <nav class="flex-shrink-0 bg-white border-t border-gray-200 flex z-50">
      <button @click="activePage = 'home'" class="flex-1 flex flex-col items-center py-2 text-xs gap-1 text-blue-600">
        <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6"/></svg>
        가계부
      </button>
    </nav>

    <!-- ══ 할부 삭제 옵션 시트 ══ -->
    <div v-if="showInstDeleteSheet && instDeleteTx" class="fixed inset-0 bg-black/40 z-[60] flex items-end justify-center" @click.self="showInstDeleteSheet = false">
      <div class="bg-white rounded-t-2xl w-full max-w-lg pb-safe" @click.stop>
        <div class="flex justify-center pt-3 pb-2"><div class="w-10 h-1 bg-gray-200 rounded-full"/></div>
        <div class="px-5 pb-2">
          <p class="text-sm font-semibold text-gray-800">할부 삭제</p>
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
          <button @click="deleteInstSingle" class="w-full py-3.5 border border-gray-200 text-gray-700 rounded-xl text-sm font-semibold hover:bg-gray-50 text-left px-4">
            단건 삭제
            <span class="block text-xs font-normal text-gray-400 mt-0.5">{{ instDeleteTx.installmentSequence }}회차만 삭제</span>
          </button>
          <button @click="showInstDeleteSheet = false" class="w-full py-2.5 text-sm text-gray-400">취소</button>
        </div>
      </div>
    </div>

    <!-- ══ 반복 삭제 옵션 시트 ══ -->
    <div v-if="showRecurDeleteSheet && recurDeleteTx" class="fixed inset-0 bg-black/40 z-[60] flex items-end justify-center" @click.self="showRecurDeleteSheet = false">
      <div class="bg-white rounded-t-2xl w-full max-w-lg pb-safe" @click.stop>
        <div class="flex justify-center pt-3 pb-2"><div class="w-10 h-1 bg-gray-200 rounded-full"/></div>
        <div class="px-5 pb-2">
          <p class="text-sm font-semibold text-gray-800">반복 삭제</p>
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
          <button @click="deleteRecurSingle" class="w-full py-3.5 border border-gray-200 text-gray-700 rounded-xl text-sm font-semibold hover:bg-gray-50 text-left px-4">
            단건 삭제
            <span class="block text-xs font-normal text-gray-400 mt-0.5">이번 달 거래만 삭제 (반복 유지)</span>
          </button>
          <button @click="showRecurDeleteSheet = false" class="w-full py-2.5 text-sm text-gray-400">취소</button>
        </div>
      </div>
    </div>

    <!-- ══ 가계부 내역 추가/수정 모달 ══ -->
    <div v-if="showModal" class="fixed inset-0 bg-black/50 z-[60] flex items-center justify-center px-4" @click.self="showModal = false">
      <div class="bg-white rounded-2xl w-full max-w-lg flex flex-col" style="max-height:90dvh">
        <div class="flex-shrink-0 flex items-center justify-between px-4 py-4 border-b border-gray-100">
          <h2 class="font-semibold text-gray-800">{{ editingTxId !== null ? '가계부 내역 수정' : '가계부 내역 추가' }}</h2>
          <button @click="showModal = false" class="text-gray-400 hover:text-gray-600">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>
          </button>
        </div>
        <div class="flex-1 overflow-y-auto">

          <!-- 지출/수입 탭 -->
          <div class="px-4 pt-4 pb-3">
            <div class="flex rounded-xl bg-gray-100 p-1">
              <button @click="formType = 'Expense'" :class="formType === 'Expense' ? 'bg-white text-red-500 shadow-sm' : 'text-gray-500'" class="flex-1 py-2 text-sm font-medium rounded-lg transition-all">지출</button>
              <button @click="formType = 'Income'"  :class="formType === 'Income'  ? 'bg-white text-blue-600 shadow-sm' : 'text-gray-500'" class="flex-1 py-2 text-sm font-medium rounded-lg transition-all">수입</button>
            </div>
          </div>

          <!-- 기본 정보 -->
          <div class="px-4 py-3 space-y-3 border-t border-gray-100">
            <p class="text-xs font-medium text-gray-400 uppercase tracking-wide">기본 정보</p>

            <!-- 금액 -->
            <div>
              <div class="flex items-center justify-between mb-1">
                <label class="text-xs text-gray-500">금액</label>
                <!-- 할부 토글 (신규, 지출만) / 할부 배지 (수정, 지출만) -->
                <button v-if="editingTxId === null && formType === 'Expense'" @click="formIsInstallment = !formIsInstallment"
                  :class="formIsInstallment ? 'bg-orange-500 text-white' : 'bg-gray-100 text-gray-500'"
                  class="flex items-center gap-1 text-xs px-2.5 py-1 rounded-full font-medium transition-colors">
                  <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z"/></svg>
                  할부
                </button>
                <span v-else-if="formIsInstallment && formType === 'Expense'" class="flex items-center gap-1 text-xs px-2.5 py-1 rounded-full font-medium bg-orange-100 text-orange-600">
                  <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z"/></svg>
                  할부
                </span>
              </div>
              <input :value="formAmount" @input="onFormAmountInput" type="text" inputmode="numeric" placeholder="0" class="w-full border border-gray-200 rounded-xl px-3 py-2.5 text-lg font-semibold focus:outline-none focus:border-blue-400" />
            </div>

            <!-- 할부 개월수 + 미리보기 -->
            <template v-if="formIsInstallment && formType === 'Expense'">
              <div>
                <label class="block text-xs text-gray-500 mb-1">총 개월수</label>
                <div class="flex items-center gap-2">
                  <input v-model.number="formInstallmentMonths" type="number" min="2" max="60" placeholder="12" class="w-24 border border-gray-200 rounded-xl px-3 py-2.5 text-sm text-center focus:outline-none focus:border-orange-400" />
                  <span class="text-sm text-gray-500">개월</span>
                </div>
              </div>
              <div v-if="installmentPreview" class="bg-orange-50 rounded-xl px-3 py-2.5 space-y-1">
                <div class="flex justify-between text-sm"><span class="text-gray-600">1회차</span><span class="font-semibold text-orange-700">{{ installmentPreview.firstMonthAmount.toLocaleString() }}원</span></div>
                <div class="flex justify-between text-sm"><span class="text-gray-600">2회차 이후</span><span class="font-semibold text-orange-700">{{ installmentPreview.monthlyAmount.toLocaleString() }}원</span></div>
              </div>
            </template>

            <!-- 날짜 -->
            <div>
              <label class="block text-xs text-gray-500 mb-1">날짜</label>
              <input v-model="formDate" type="date" class="w-full border border-gray-200 rounded-xl px-3 py-2.5 text-sm focus:outline-none focus:border-blue-400" />
            </div>
          </div>

          <!-- 분류 -->
          <div class="px-4 py-3 space-y-3 border-t border-gray-100">
            <p class="text-xs font-medium text-gray-400 uppercase tracking-wide">분류</p>
            <div>
              <label class="block text-xs text-gray-500 mb-1">카테고리</label>
              <select v-model="formCategoryId" class="w-full border border-gray-200 rounded-xl px-3 py-2.5 text-sm focus:outline-none focus:border-blue-400">
                <option :value="0">선택하세요</option>
                <option v-for="c in formCategories" :key="c.id" :value="c.id">{{ c.name }}</option>
              </select>
            </div>
            <div v-if="formType === 'Expense'">
              <label class="block text-xs text-gray-500 mb-1">결제수단</label>
              <select v-model="formPaymentMethodId" class="w-full border border-gray-200 rounded-xl px-3 py-2.5 text-sm focus:outline-none focus:border-blue-400">
                <option :value="0">선택하세요</option>
                <option v-for="m in MOCK_PAYMENT_METHODS" :key="m.id" :value="m.id">{{ m.name }}<template v-if="m.type === 'Point'"> (잔액 {{ m.remainingAmount?.toLocaleString() }}원)</template></option>
              </select>
            </div>
          </div>

          <!-- 부가 정보 -->
          <div class="px-4 py-3 space-y-3 border-t border-gray-100">
            <p class="text-xs font-medium text-gray-400 uppercase tracking-wide">부가 정보</p>
            <input v-model="formMemo" type="text" placeholder="메모 (선택)" class="w-full border border-gray-200 rounded-xl px-3 py-2.5 text-sm focus:outline-none focus:border-blue-400" />

            <!-- 반복 토글 -->
            <div>
              <label class="flex items-center gap-3 cursor-pointer">
                <div @click="formIsRecurring = !formIsRecurring" :class="formIsRecurring ? 'bg-teal-500' : 'bg-gray-200'" class="w-11 h-6 rounded-full transition-colors relative flex-shrink-0">
                  <div :class="formIsRecurring ? 'translate-x-5' : 'translate-x-0.5'" class="absolute top-0.5 w-5 h-5 bg-white rounded-full shadow transition-transform"/>
                </div>
                <span class="text-sm text-gray-700">매월 반복</span>
              </label>
              <!-- 반복 ON 시 펼쳐지는 필드 -->
              <template v-if="formIsRecurring">
                <div class="mt-3 flex items-center gap-2">
                  <span class="text-sm text-gray-600">매월</span>
                  <input v-model.number="formRecurringDay" type="number" min="1" max="28"
                    class="w-16 border border-gray-200 rounded-xl px-3 py-2 text-sm text-center focus:outline-none focus:border-teal-400" />
                  <span class="text-sm text-gray-600">일 반복</span>
                </div>
                <div class="mt-2">
                  <label class="block text-xs text-gray-500 mb-1">종료일 <span class="text-gray-400">(없으면 무기한)</span></label>
                  <input v-model="formRecurringEndDate" type="date"
                    class="w-full border border-gray-200 rounded-xl px-3 py-2.5 text-sm focus:outline-none focus:border-teal-400" />
                </div>
              </template>
            </div>

            <label class="flex items-center gap-3 cursor-pointer">
              <div @click="formIsIncluded = !formIsIncluded" :class="formIsIncluded ? 'bg-blue-600' : 'bg-gray-200'" class="w-11 h-6 rounded-full transition-colors relative">
                <div :class="formIsIncluded ? 'translate-x-5' : 'translate-x-0.5'" class="absolute top-0.5 w-5 h-5 bg-white rounded-full shadow transition-transform"/>
              </div>
              <span class="text-sm text-gray-700">합산에 포함</span>
            </label>
          </div>
        </div>

        <!-- 저장 버튼 -->
        <div class="flex-shrink-0 px-4 py-4 border-t border-gray-100">
          <button @click="formSaveMock" :disabled="!formIsValid && !formSaved"
            :class="formSaved ? 'bg-green-500' : !formIsValid ? 'bg-gray-200 text-gray-400 cursor-not-allowed' : (formIsInstallment && formType === 'Expense') ? 'bg-orange-500 hover:bg-orange-600' : 'bg-blue-600 hover:bg-blue-700'"
            class="w-full text-white py-3 rounded-xl font-semibold text-sm transition-colors">
            {{ formSaved ? '✓ 저장 완료' : editingTxId !== null ? '수정 완료' : (formIsInstallment && formType === 'Expense') ? '할부 등록' : formIsRecurring ? '반복 등록' : '추가' }}
          </button>
        </div>
      </div>
    </div>


  </div>
</template>
