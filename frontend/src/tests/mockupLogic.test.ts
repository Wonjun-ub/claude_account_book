import { describe, it, expect, beforeEach } from 'vitest'
import { ref, computed } from 'vue'
import { getMonthPeriod } from '@/utils/monthPeriod'
import type { Ref } from 'vue'

// ── 타입 ─────────────────────────────────────────────────────────────────────

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
  installmentMasterId: number | undefined
  installmentSequence: number | undefined
  recurringMasterId: number | undefined
}

interface MockRecurringMaster {
  id: number
  amount: number
  type: 'Income' | 'Expense'
  dayOfMonth: number
  startDate: string
  endDate: string | undefined
  isActive: boolean
  categoryId: number
  categoryName: string
  paymentMethodId: number
  paymentMethodName: string
  memo: string | undefined
}

// ── 헬퍼 ─────────────────────────────────────────────────────────────────────

function makeTx(override: Partial<MockTxRecord>): MockTxRecord {
  return {
    id: 1, amount: 10000, date: '2026-03-10', type: 'Expense',
    categoryId: 1, categoryName: '식비', paymentMethodId: 1, paymentMethodName: '신한카드',
    memo: undefined, isIncludedInTotal: true,
    installmentMasterId: undefined, installmentSequence: undefined, recurringMasterId: undefined,
    ...override,
  }
}

function makeRecurMaster(override: Partial<MockRecurringMaster>): MockRecurringMaster {
  return {
    id: 100, amount: 3000000, type: 'Income', dayOfMonth: 10,
    startDate: '2026-01-10', endDate: undefined, isActive: true,
    categoryId: 5, categoryName: '급여', paymentMethodId: 0, paymentMethodName: '', memo: undefined,
    ...override,
  }
}

// ── 월 필터 로직 ─────────────────────────────────────────────────────────────

function makeMonthFilter(
  txs: Ref<MockTxRecord[]>,
  year: Ref<number>,
  month: Ref<number>,
  startDay: Ref<number>,
) {
  return computed(() => {
    const { start, end } = getMonthPeriod(year.value, month.value, startDay.value)
    return txs.value.filter(t => t.date >= start && t.date <= end)
  })
}

// ── 요약 로직 ────────────────────────────────────────────────────────────────

function makeSummary(filtered: Ref<MockTxRecord[]>) {
  return computed(() => {
    const included     = filtered.value.filter(t => t.isIncludedInTotal)
    const incomeList   = included.filter(t => t.type === 'Income')
    const expenseList  = included.filter(t => t.type === 'Expense')
    const totalIncome  = incomeList.reduce((s, t) => s + t.amount, 0)
    const totalExpense = expenseList.reduce((s, t) => s + t.amount, 0)
    return { totalIncome, totalExpense, balance: totalIncome - totalExpense,
      incomeCount: incomeList.length, expenseCount: expenseList.length }
  })
}

// ── 반복 예정 로직 ───────────────────────────────────────────────────────────

function makeRecurPending(
  masters: Ref<MockRecurringMaster[]>,
  txs: Ref<MockTxRecord[]>,
  skipped: Ref<string[]>,
  year: Ref<number>,
  month: Ref<number>,
  startDay: Ref<number>,
) {
  return computed(() => {
    const { start, end } = getMonthPeriod(year.value, month.value, startDay.value)
    const monthKey = (id: number) => `${id}-${year.value}-${String(month.value).padStart(2, '0')}`
    return masters.value.filter(master => {
      if (!master.isActive) return false
      if (master.startDate > end) return false
      if (master.endDate && master.endDate < start) return false
      if (skipped.value.includes(monthKey(master.id))) return false
      return !txs.value.some(t =>
        t.recurringMasterId === master.id && t.date >= start && t.date <= end
      )
    })
  })
}

// ── 유효성 검사 로직 ─────────────────────────────────────────────────────────

function makeFormIsValid(
  formAmount: Ref<string>,
  formCategoryId: Ref<number>,
  formPaymentMethodId: Ref<number>,
  formType: Ref<'Expense' | 'Income'>,
) {
  return computed(() => {
    const amount = parseInt(formAmount.value.replace(/,/g, ''), 10) || 0
    if (amount <= 0) return false
    if (formCategoryId.value === 0) return false
    if (formType.value === 'Expense' && formPaymentMethodId.value === 0) return false
    return true
  })
}

// ══════════════════════════════════════════════════════════════════════════════

describe('월별 거래 필터링 (txMonthFiltered)', () => {
  const year     = ref(2026)
  const month    = ref(3)
  const startDay = ref(1)
  const txs      = ref<MockTxRecord[]>([])
  const filtered = makeMonthFilter(txs, year, month, startDay)

  beforeEach(() => { txs.value = []; startDay.value = 1; month.value = 3 })

  it('TC-FILTER-01: startDay=1, 3월 거래만 필터링', () => {
    txs.value = [
      makeTx({ id: 1, date: '2026-03-10' }),
      makeTx({ id: 2, date: '2026-02-28' }),
      makeTx({ id: 3, date: '2026-04-01' }),
    ]
    expect(filtered.value.map(t => t.id)).toEqual([1])
  })

  it('TC-FILTER-02: startDay=25, 2/25 거래는 3월 범위에 포함', () => {
    startDay.value = 25
    txs.value = [
      makeTx({ id: 1, date: '2026-02-25' }),   // 3월 시작일 → 포함
      makeTx({ id: 2, date: '2026-02-24' }),   // 3월 시작일 전날 → 제외
      makeTx({ id: 3, date: '2026-03-24' }),   // 3월 마지막날 → 포함
      makeTx({ id: 4, date: '2026-03-25' }),   // 4월 시작일 → 제외
    ]
    const ids = filtered.value.map(t => t.id)
    expect(ids).toContain(1)
    expect(ids).toContain(3)
    expect(ids).not.toContain(2)
    expect(ids).not.toContain(4)
  })

  it('TC-FILTER-03: startDay=25, 2/24 거래는 2월 범위에 포함', () => {
    startDay.value = 25
    month.value = 2
    txs.value = [makeTx({ id: 1, date: '2026-02-24' })]
    expect(filtered.value.map(t => t.id)).toContain(1)
  })

  it('TC-FILTER-04: 빈 거래 목록 → 빈 결과', () => {
    txs.value = []
    expect(filtered.value).toHaveLength(0)
  })

  it('TC-FILTER-05: 경계값 — 월 첫째날, 마지막날 포함', () => {
    startDay.value = 1
    txs.value = [
      makeTx({ id: 1, date: '2026-03-01' }),
      makeTx({ id: 2, date: '2026-03-31' }),
    ]
    expect(filtered.value).toHaveLength(2)
  })
})

// ──────────────────────────────────────────────────────────────────────────────

describe('월 요약 계산 (mockSummary)', () => {
  const txs      = ref<MockTxRecord[]>([])
  const year     = ref(2026)
  const month    = ref(3)
  const startDay = ref(1)
  const filtered = makeMonthFilter(txs, year, month, startDay)
  const summary  = makeSummary(filtered)

  beforeEach(() => { txs.value = [] })

  it('TC-SUM-01: 수입/지출 합계 정상 계산', () => {
    txs.value = [
      makeTx({ id: 1, amount: 3000000, type: 'Income', date: '2026-03-10', paymentMethodId: 0, paymentMethodName: '' }),
      makeTx({ id: 2, amount: 50000,   type: 'Expense', date: '2026-03-15' }),
    ]
    expect(summary.value.totalIncome).toBe(3000000)
    expect(summary.value.totalExpense).toBe(50000)
    expect(summary.value.balance).toBe(2950000)
  })

  it('TC-SUM-02: isIncludedInTotal=false 거래는 합계에서 제외', () => {
    txs.value = [
      makeTx({ id: 1, amount: 100000, type: 'Income', date: '2026-03-10', isIncludedInTotal: false }),
      makeTx({ id: 2, amount: 50000,  type: 'Expense', date: '2026-03-15', isIncludedInTotal: true }),
    ]
    expect(summary.value.totalIncome).toBe(0)
    expect(summary.value.totalExpense).toBe(50000)
    expect(summary.value.balance).toBe(-50000)
  })

  it('TC-SUM-03: 거래 건수 정상 계산', () => {
    txs.value = [
      makeTx({ id: 1, amount: 1000000, type: 'Income',  date: '2026-03-10', paymentMethodId: 0, paymentMethodName: '' }),
      makeTx({ id: 2, amount: 50000,   type: 'Income',  date: '2026-03-11', paymentMethodId: 0, paymentMethodName: '' }),
      makeTx({ id: 3, amount: 30000,   type: 'Expense', date: '2026-03-12' }),
    ]
    expect(summary.value.incomeCount).toBe(2)
    expect(summary.value.expenseCount).toBe(1)
  })

  it('TC-SUM-04: 빈 거래 → 모두 0', () => {
    txs.value = []
    expect(summary.value.totalIncome).toBe(0)
    expect(summary.value.totalExpense).toBe(0)
    expect(summary.value.balance).toBe(0)
    expect(summary.value.incomeCount).toBe(0)
    expect(summary.value.expenseCount).toBe(0)
  })
})

// ──────────────────────────────────────────────────────────────────────────────

describe('반복 예정 배너 (recurringPending)', () => {
  const masters  = ref<MockRecurringMaster[]>([])
  const txs      = ref<MockTxRecord[]>([])
  const skipped  = ref<string[]>([])
  const year     = ref(2026)
  const month    = ref(3)
  const startDay = ref(25)   // 25일 시작
  const pending  = makeRecurPending(masters, txs, skipped, year, month, startDay)

  beforeEach(() => { masters.value = []; txs.value = []; skipped.value = [] })

  it('TC-PEND-01: 활성 마스터 + 해당 월 거래 없음 → pending 포함', () => {
    masters.value = [makeRecurMaster({ id: 1 })]
    expect(pending.value).toHaveLength(1)
  })

  it('TC-PEND-02: 활성 마스터 + 해당 월 거래 있음 → pending 제외', () => {
    masters.value = [makeRecurMaster({ id: 1 })]
    // 3월 범위(startDay=25) = 2026-02-25~2026-03-24, 3/10은 범위 안
    txs.value = [makeTx({ date: '2026-03-10', recurringMasterId: 1 })]
    expect(pending.value).toHaveLength(0)
  })

  it('TC-PEND-03: isActive=false → pending 제외', () => {
    masters.value = [makeRecurMaster({ id: 1, isActive: false })]
    expect(pending.value).toHaveLength(0)
  })

  it('TC-PEND-04: skip 키 있음 → pending 제외', () => {
    masters.value = [makeRecurMaster({ id: 1 })]
    skipped.value = ['1-2026-03']  // 3월 skip
    expect(pending.value).toHaveLength(0)
  })

  it('TC-PEND-05: 다른 달 skip 키 → 현재 달 pending 유지', () => {
    masters.value = [makeRecurMaster({ id: 1 })]
    skipped.value = ['1-2026-02']  // 2월 skip (현재 3월과 무관)
    expect(pending.value).toHaveLength(1)
  })

  it('TC-PEND-06: startDate > 월 end → pending 제외 (아직 시작 안 됨)', () => {
    // 3월 범위 end = 2026-03-24, startDate가 그 이후
    masters.value = [makeRecurMaster({ id: 1, startDate: '2026-03-25' })]
    expect(pending.value).toHaveLength(0)
  })

  it('TC-PEND-07: endDate < 월 start → pending 제외 (이미 종료)', () => {
    // 3월 범위 start = 2026-02-25, endDate가 그 이전
    masters.value = [makeRecurMaster({ id: 1, endDate: '2026-02-24' })]
    expect(pending.value).toHaveLength(0)
  })

  it('TC-PEND-08: 수입/지출 마스터 혼합 → 모두 pending 포함', () => {
    masters.value = [
      makeRecurMaster({ id: 1, type: 'Income' }),
      makeRecurMaster({ id: 2, type: 'Expense' }),
    ]
    expect(pending.value).toHaveLength(2)
  })
})

// ──────────────────────────────────────────────────────────────────────────────

describe('반복 skip 키 메커니즘', () => {
  const year     = ref(2026)
  const month    = ref(3)
  const startDay = ref(25)
  const masters  = ref<MockRecurringMaster[]>([])
  const txs      = ref<MockTxRecord[]>([])
  const skipped  = ref<string[]>([])
  const pending  = makeRecurPending(masters, txs, skipped, year, month, startDay)

  beforeEach(() => { masters.value = []; txs.value = []; skipped.value = [] })

  // deleteRecurSingle 로직 인라인 (mockYear/mockMonth 기준)
  function deleteRecurSingle(tx: MockTxRecord) {
    if (tx.recurringMasterId !== undefined) {
      const key = `${tx.recurringMasterId}-${year.value}-${String(month.value).padStart(2, '0')}`
      if (!skipped.value.includes(key)) skipped.value.push(key)
    }
    if (tx.id !== -1) txs.value = txs.value.filter(t => t.id !== tx.id)
  }

  // deleteRecurAll 로직 인라인
  function deleteRecurAll(masterId: number) {
    txs.value = txs.value.filter(t => t.recurringMasterId !== masterId)
    masters.value = masters.value.filter(m => m.id !== masterId)
    skipped.value = skipped.value.filter(k => !k.startsWith(`${masterId}-`))
  }

  it('TC-SKIP-01: deleteRecurSingle → mockYear/mockMonth 기준 skip 키 추가', () => {
    masters.value = [makeRecurMaster({ id: 1 })]
    // 2/25 거래 (3월 범위 내)
    const tx = makeTx({ id: 10, date: '2026-02-25', recurringMasterId: 1 })
    txs.value = [tx]
    deleteRecurSingle(tx)
    // skip 키는 표시 월 기준 (mockMonth=3)
    expect(skipped.value).toContain('1-2026-03')
    expect(pending.value).toHaveLength(0)
  })

  it('TC-SKIP-02: deleteRecurSingle 중복 호출 → skip 키 1개만 유지', () => {
    masters.value = [makeRecurMaster({ id: 1 })]
    const tx = makeTx({ id: 10, date: '2026-03-10', recurringMasterId: 1 })
    txs.value = [tx]
    deleteRecurSingle(tx)
    deleteRecurSingle({ ...tx, id: -1 }) // 중복 호출
    expect(skipped.value.filter(k => k === '1-2026-03')).toHaveLength(1)
  })

  it('TC-SKIP-03: deleteRecurAll → 해당 마스터 skip 키 전체 제거', () => {
    skipped.value = ['1-2026-03', '1-2026-02', '2-2026-03'] // 마스터 1 skip 2건 + 마스터 2 skip 1건
    deleteRecurAll(1)
    expect(skipped.value).not.toContain('1-2026-03')
    expect(skipped.value).not.toContain('1-2026-02')
    expect(skipped.value).toContain('2-2026-03') // 다른 마스터 skip은 유지
  })

  it('TC-SKIP-04: pending 컨텍스트(id=-1) 단건 삭제 → 거래 삭제 없이 skip만 추가', () => {
    masters.value = [makeRecurMaster({ id: 1 })]
    const synthetic = makeTx({ id: -1, date: '2026-03-10', recurringMasterId: 1 })
    deleteRecurSingle(synthetic)
    expect(txs.value).toHaveLength(0) // 원래부터 없었음
    expect(skipped.value).toContain('1-2026-03')
    expect(pending.value).toHaveLength(0)
  })
})

// ──────────────────────────────────────────────────────────────────────────────

describe('할부 삭제 로직', () => {
  const txs = ref<MockTxRecord[]>([])

  function makeInstTx(id: number, seq: number, masterId = 99): MockTxRecord {
    return makeTx({ id, installmentMasterId: masterId, installmentSequence: seq, date: `2026-0${seq + 2}-08` })
  }

  beforeEach(() => {
    txs.value = [makeInstTx(1, 1), makeInstTx(2, 2), makeInstTx(3, 3)]
  })

  // deleteInstAll 로직
  function deleteInstAll(masterId: number) {
    txs.value = txs.value.filter(t => t.installmentMasterId !== masterId)
  }

  // deleteInstFromHere 로직
  function deleteInstFromHere(tx: MockTxRecord) {
    txs.value = txs.value.filter(t =>
      t.installmentMasterId !== tx.installmentMasterId ||
      (t.installmentSequence ?? 0) < (tx.installmentSequence ?? 0)
    )
  }

  // deleteInstSingle 로직
  function deleteInstSingle(id: number) {
    txs.value = txs.value.filter(t => t.id !== id)
  }

  it('TC-INST-DEL-01: deleteInstAll → 전체 회차 삭제', () => {
    deleteInstAll(99)
    expect(txs.value).toHaveLength(0)
  })

  it('TC-INST-DEL-02: deleteInstFromHere(2회차) → 1회차 유지, 2·3회차 삭제', () => {
    deleteInstFromHere(makeInstTx(2, 2))
    expect(txs.value.map(t => t.installmentSequence)).toEqual([1])
  })

  it('TC-INST-DEL-03: deleteInstFromHere(1회차) → 전체 삭제', () => {
    deleteInstFromHere(makeInstTx(1, 1))
    expect(txs.value).toHaveLength(0)
  })

  it('TC-INST-DEL-04: deleteInstFromHere(3회차) → 1·2회차 유지, 3회차 삭제', () => {
    deleteInstFromHere(makeInstTx(3, 3))
    expect(txs.value.map(t => t.installmentSequence)).toEqual([1, 2])
  })

  it('TC-INST-DEL-05: deleteInstSingle(2회차) → 1·3회차 유지', () => {
    deleteInstSingle(2)
    expect(txs.value.map(t => t.installmentSequence)).toEqual([1, 3])
  })
})

// ──────────────────────────────────────────────────────────────────────────────

describe('반복 삭제 로직', () => {
  const masters = ref<MockRecurringMaster[]>([])
  const txs     = ref<MockTxRecord[]>([])

  function makeRecurTx(id: number, date: string, masterId = 100): MockTxRecord {
    return makeTx({ id, date, recurringMasterId: masterId })
  }

  beforeEach(() => {
    masters.value = [makeRecurMaster({ id: 100 })]
    txs.value = [
      makeRecurTx(1, '2026-01-10'),
      makeRecurTx(2, '2026-02-10'),
      makeRecurTx(3, '2026-03-10'),
    ]
  })

  function deleteRecurAll(masterId: number, skipped: Ref<string[]>) {
    txs.value = txs.value.filter(t => t.recurringMasterId !== masterId)
    masters.value = masters.value.filter(m => m.id !== masterId)
    skipped.value = skipped.value.filter(k => !k.startsWith(`${masterId}-`))
  }

  function deleteRecurFromHere(tx: MockTxRecord) {
    txs.value = txs.value.filter(t =>
      t.recurringMasterId !== tx.recurringMasterId || t.date < tx.date
    )
    const master = masters.value.find(m => m.id === tx.recurringMasterId)
    if (master) master.isActive = false
  }

  it('TC-RECUR-DEL-01: deleteRecurAll → 모든 거래 + 마스터 삭제', () => {
    const skipped = ref<string[]>([])
    deleteRecurAll(100, skipped)
    expect(txs.value).toHaveLength(0)
    expect(masters.value).toHaveLength(0)
  })

  it('TC-RECUR-DEL-02: deleteRecurAll → skip 키도 함께 삭제', () => {
    const skipped = ref(['100-2026-01', '100-2026-02', '200-2026-03'])
    deleteRecurAll(100, skipped)
    expect(skipped.value).not.toContain('100-2026-01')
    expect(skipped.value).toContain('200-2026-03') // 다른 마스터 skip 유지
  })

  it('TC-RECUR-DEL-03: deleteRecurFromHere(3월) → 1·2월 거래 유지, 3월 삭제, 마스터 비활성화', () => {
    deleteRecurFromHere(makeRecurTx(3, '2026-03-10'))
    expect(txs.value.map(t => t.id)).toEqual([1, 2])
    expect(masters.value[0]?.isActive).toBe(false)
  })

  it('TC-RECUR-DEL-04: deleteRecurFromHere(1월) → 전체 삭제, 마스터 비활성화', () => {
    deleteRecurFromHere(makeRecurTx(1, '2026-01-10'))
    expect(txs.value).toHaveLength(0)
    expect(masters.value[0]?.isActive).toBe(false)
  })
})

// ──────────────────────────────────────────────────────────────────────────────

describe('폼 유효성 검사 (formIsValid)', () => {
  const formAmount          = ref('')
  const formCategoryId      = ref(0)
  const formPaymentMethodId = ref(0)
  const formType            = ref<'Expense' | 'Income'>('Expense')
  const isValid = makeFormIsValid(formAmount, formCategoryId, formPaymentMethodId, formType)

  beforeEach(() => {
    formAmount.value = ''
    formCategoryId.value = 0
    formPaymentMethodId.value = 0
    formType.value = 'Expense'
  })

  it('TC-VALID-01: 금액=0 → invalid', () => {
    formCategoryId.value = 1
    formPaymentMethodId.value = 1
    formAmount.value = '0'
    expect(isValid.value).toBe(false)
  })

  it('TC-VALID-02: 금액 미입력 → invalid', () => {
    formCategoryId.value = 1
    formPaymentMethodId.value = 1
    formAmount.value = ''
    expect(isValid.value).toBe(false)
  })

  it('TC-VALID-03: 카테고리 미선택(0) → invalid', () => {
    formAmount.value = '10,000'
    formPaymentMethodId.value = 1
    formCategoryId.value = 0
    expect(isValid.value).toBe(false)
  })

  it('TC-VALID-04: 지출 + 결제수단 미선택(0) → invalid', () => {
    formAmount.value = '10,000'
    formCategoryId.value = 1
    formType.value = 'Expense'
    formPaymentMethodId.value = 0
    expect(isValid.value).toBe(false)
  })

  it('TC-VALID-05: 지출 + 모두 입력 → valid', () => {
    formAmount.value = '10,000'
    formCategoryId.value = 1
    formPaymentMethodId.value = 1
    formType.value = 'Expense'
    expect(isValid.value).toBe(true)
  })

  it('TC-VALID-06: 수입 + 결제수단 미선택 → valid (수입은 결제수단 불필요)', () => {
    formAmount.value = '3,000,000'
    formCategoryId.value = 5
    formPaymentMethodId.value = 0
    formType.value = 'Income'
    expect(isValid.value).toBe(true)
  })

  it('TC-VALID-07: 쉼표 포함 금액(1,000) → 올바르게 파싱되어 valid', () => {
    formAmount.value = '1,000'
    formCategoryId.value = 1
    formPaymentMethodId.value = 1
    formType.value = 'Expense'
    expect(isValid.value).toBe(true)
  })
})
