import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import dayjs from 'dayjs'
import {
  db,
  calcInstallment,
  createInstallment,
  applyRecurringForMonth,
  getRecurringPending,
} from '@/database/db'
import { getMonthPeriod } from '@/utils/monthPeriod'
import type {
  TransactionView,
  MonthlySummary,
  MonthlyTrend,
  CreateTransactionParams,
  CreateInstallmentParams,
  CreateRecurringParams,
  UpdateInstallmentParams,
  UpdateRecurringParams,
} from '@/types'
import type { Transaction, RecurringTransaction } from '@/database/db'

export const useTransactionStore = defineStore('transaction', () => {
  const transactions = ref<TransactionView[]>([])

  // ── 조회 ──────────────────────────────────────────────────────────────────

  /** 현재 월 거래 로드 (반복 자동 생성 후 조회) */
  async function loadTransactions(
    year: number,
    month: number,
    monthStartDay: number,
  ): Promise<void> {
    await applyRecurringForMonth(year, month, monthStartDay)
    const { start, end } = getMonthPeriod(year, month, monthStartDay)

    const rawTxList = await db.transactions
      .where('date')
      .between(start, end, true, true)
      .toArray()

    transactions.value = await _joinNames(rawTxList as (Transaction & { id: number })[])
  }

  /** 특정 월 거래 조회 (통계용 — 필터 없음) */
  async function getTransactionsForMonth(
    year: number,
    month: number,
    monthStartDay: number,
  ): Promise<TransactionView[]> {
    const { start, end } = getMonthPeriod(year, month, monthStartDay)
    const rawTxList = await db.transactions
      .where('date')
      .between(start, end, true, true)
      .toArray()
    return _joinNames(rawTxList as (Transaction & { id: number })[])
  }

  /** 최근 N개월 월별 요약 조회 (추이 계산용) */
  async function getMonthlyTrend(
    baseYear: number,
    baseMonth: number,
    months: number,
    monthStartDay: number,
  ): Promise<MonthlyTrend[]> {
    const result: MonthlyTrend[] = []
    for (let i = months - 1; i >= 0; i--) {
      const d = dayjs(`${baseYear}-${String(baseMonth).padStart(2, '0')}-01`).subtract(i, 'month')
      const y = d.year()
      const m = d.month() + 1
      const txs = await getTransactionsForMonth(y, m, monthStartDay)
      result.push({
        year:    y,
        month:   m,
        income:  txs.filter(t => t.type === 'Income').reduce((s, t) => s + t.amount, 0),
        expense: txs.filter(t => t.type === 'Expense').reduce((s, t) => s + t.amount, 0),
        savings: txs.filter(t => t.type === 'Savings').reduce((s, t) => s + t.amount, 0),
      })
    }
    return result
  }

  /** 반복 예정 목록 */
  async function getPendingRecurring(
    year: number,
    month: number,
    monthStartDay: number,
  ): Promise<RecurringTransaction[]> {
    return getRecurringPending(year, month, monthStartDay)
  }

  // ── 현재 월 요약 computed ─────────────────────────────────────────────────

  const summary = computed<MonthlySummary | null>(() => {
    if (transactions.value.length === 0 && _summaryMeta.value === null) return null
    const meta = _summaryMeta.value
    if (!meta) return null
    const included = transactions.value.filter(t => t.isIncludedInTotal)
    return {
      year:         meta.year,
      month:        meta.month,
      totalIncome:  included.filter(t => t.type === 'Income').reduce((s, t) => s + t.amount, 0),
      totalExpense: included.filter(t => t.type === 'Expense').reduce((s, t) => s + t.amount, 0),
      totalSavings: included.filter(t => t.type === 'Savings').reduce((s, t) => s + t.amount, 0),
      balance:
        included.filter(t => t.type === 'Income').reduce((s, t) => s + t.amount, 0) -
        included.filter(t => t.type === 'Expense').reduce((s, t) => s + t.amount, 0),
      periodStart:  meta.periodStart,
      periodEnd:    meta.periodEnd,
      incomeCount:  transactions.value.filter(t => t.type === 'Income').length,
      expenseCount: transactions.value.filter(t => t.type === 'Expense').length,
      savingsCount: transactions.value.filter(t => t.type === 'Savings').length,
    }
  })

  // summary 계산에 필요한 메타 (year, month, period)를 별도 보관
  const _summaryMeta = ref<{
    year: number; month: number; periodStart: string; periodEnd: string
  } | null>(null)

  async function _loadWithMeta(year: number, month: number, monthStartDay: number) {
    const { start, end } = getMonthPeriod(year, month, monthStartDay)
    _summaryMeta.value = { year, month, periodStart: start, periodEnd: end }
    await loadTransactions(year, month, monthStartDay)
  }

  // ── 생성 ──────────────────────────────────────────────────────────────────

  async function createSingle(params: CreateTransactionParams): Promise<void> {
    await db.transactions.add({ ...params })
  }

  async function createInstallmentTx(params: CreateInstallmentParams): Promise<void> {
    await createInstallment(params)
  }

  async function createRecurring(params: CreateRecurringParams): Promise<void> {
    await db.recurringTransactions.add({ ...params, isActive: true })
  }

  // ── 수정 ──────────────────────────────────────────────────────────────────

  async function updateTransaction(
    id: number,
    params: Partial<Transaction>,
  ): Promise<void> {
    await db.transactions.update(id, params)
  }

  async function updateInstallmentMaster(
    masterId: number,
    params: UpdateInstallmentParams,
  ): Promise<void> {
    const { monthlyAmount, firstMonthAmount } = calcInstallment(
      params.totalAmount,
      params.totalInstallments,
    )

    await db.transaction(
      'rw',
      [db.installmentTransactions, db.transactions],
      async () => {
        await db.installmentTransactions.update(masterId, {
          totalAmount:       params.totalAmount,
          monthlyAmount,
          firstMonthAmount,
          totalInstallments: params.totalInstallments,
          categoryId:        params.categoryId,
          paymentMethodId:   params.paymentMethodId,
          memo:              params.memo,
        })

        const linked = await db.transactions
          .where('installmentTransactionId')
          .equals(masterId)
          .toArray()

        for (const t of linked) {
          if (!t.id) continue
          const amount = t.installmentSequence === 1 ? firstMonthAmount : monthlyAmount
          await db.transactions.update(t.id, {
            amount,
            categoryId:      params.categoryId,
            paymentMethodId: params.paymentMethodId,
            memo:            params.memo,
          })
        }
      },
    )
  }

  async function updateRecurringMaster(
    masterId: number,
    params: UpdateRecurringParams,
  ): Promise<void> {
    await db.recurringTransactions.update(masterId, params)
  }

  // ── 삭제 ──────────────────────────────────────────────────────────────────

  async function deleteTransaction(id: number): Promise<void> {
    await db.transactions.delete(id)
  }

  async function deleteInstallment(
    masterId: number,
    mode: 'all' | 'fromHere' | 'single',
    sequence?: number,
  ): Promise<void> {
    await db.transaction(
      'rw',
      [db.installmentTransactions, db.transactions],
      async () => {
        if (mode === 'all') {
          await db.transactions
            .where('installmentTransactionId')
            .equals(masterId)
            .delete()
          await db.installmentTransactions.delete(masterId)
        } else if (mode === 'fromHere' && sequence !== undefined) {
          await db.transactions
            .where('installmentTransactionId')
            .equals(masterId)
            .filter(t => (t.installmentSequence ?? 0) >= sequence)
            .delete()
          // 남은 회차 있는지 확인
          const remaining = await db.transactions
            .where('installmentTransactionId')
            .equals(masterId)
            .count()
          if (remaining === 0) await db.installmentTransactions.delete(masterId)
          else await db.installmentTransactions.update(masterId, { isActive: false })
        } else if (mode === 'single' && sequence !== undefined) {
          await db.transactions
            .where('installmentTransactionId')
            .equals(masterId)
            .filter(t => t.installmentSequence === sequence)
            .delete()
        }
      },
    )
  }

  async function deleteRecurring(
    masterId: number,
    mode: 'all' | 'fromHere' | 'skipMonth',
    year?: number,
    month?: number,
    fromDate?: string,
  ): Promise<void> {
    if (mode === 'all') {
      await db.transaction(
        'rw',
        [db.recurringTransactions, db.transactions, db.recurringSkips],
        async () => {
          await db.transactions
            .where('recurringTransactionId')
            .equals(masterId)
            .delete()
          await db.recurringSkips
            .where('recurringTransactionId')
            .equals(masterId)
            .delete()
          await db.recurringTransactions.delete(masterId)
        },
      )
    } else if (mode === 'fromHere' && fromDate) {
      await db.transaction(
        'rw',
        [db.recurringTransactions, db.transactions],
        async () => {
          await db.transactions
            .where('recurringTransactionId')
            .equals(masterId)
            .filter(t => t.date >= fromDate)
            .delete()
          await db.recurringTransactions.update(masterId, {
            isActive: false,
            endDate:  dayjs(fromDate).subtract(1, 'day').format('YYYY-MM-DD'),
          })
        },
      )
    } else if (mode === 'skipMonth' && year !== undefined && month !== undefined) {
      await db.recurringSkips.add({ recurringTransactionId: masterId, year, month })
      // 이미 생성된 해당 월 거래 삭제
      const { start, end } = getMonthPeriod(year, month, 1)
      await db.transactions
        .where('recurringTransactionId')
        .equals(masterId)
        .filter(t => t.date >= start && t.date <= end)
        .delete()
    }
  }

  // ── 반복 자동 적용 ────────────────────────────────────────────────────────

  async function applyRecurring(
    year: number,
    month: number,
    monthStartDay: number,
  ): Promise<void> {
    await applyRecurringForMonth(year, month, monthStartDay)
  }

  // ── 내부 헬퍼 ─────────────────────────────────────────────────────────────

  async function _joinNames(
    txList: (Transaction & { id: number })[],
  ): Promise<TransactionView[]> {
    const [allCats, allPMs, allSMs, allMasters] = await Promise.all([
      db.categories.toArray(),
      db.paymentMethods.toArray(),
      db.savingsMethods.toArray(),
      db.installmentTransactions.toArray(),
    ])

    const catMap  = new Map(allCats.map(c => [c.id!, c.name]))
    const pmMap   = new Map(allPMs.map(p => [p.id!, p.name]))
    const smMap   = new Map(allSMs.map(s => [s.id!, s.name]))
    const mstMap  = new Map(allMasters.map(m => [m.id!, m.totalInstallments]))

    return txList.map(t => ({
      id:                          t.id,
      amount:                      t.amount,
      date:                        t.date,
      type:                        t.type,
      categoryId:                  t.categoryId,
      categoryName:                catMap.get(t.categoryId) ?? '(삭제됨)',
      paymentMethodId:             t.paymentMethodId,
      paymentMethodName:           t.paymentMethodId ? pmMap.get(t.paymentMethodId) : undefined,
      savingsMethodId:             t.savingsMethodId,
      savingsMethodName:           t.savingsMethodId ? smMap.get(t.savingsMethodId) : undefined,
      memo:                        t.memo,
      isIncludedInTotal:           t.isIncludedInTotal,
      recurringTransactionId:      t.recurringTransactionId,
      installmentTransactionId:    t.installmentTransactionId,
      installmentSequence:         t.installmentSequence,
      installmentTotalInstallments:
        t.installmentTransactionId ? mstMap.get(t.installmentTransactionId) : undefined,
    }))
  }

  return {
    transactions,
    summary,
    loadTransactions: _loadWithMeta,
    getTransactionsForMonth,
    getMonthlyTrend,
    getPendingRecurring,
    createSingle,
    createInstallment: createInstallmentTx,
    createRecurring,
    updateTransaction,
    updateInstallmentMaster,
    updateRecurringMaster,
    deleteTransaction,
    deleteInstallment,
    deleteRecurring,
    applyRecurring,
  }
})
