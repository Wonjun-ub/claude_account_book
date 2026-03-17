import { defineStore } from 'pinia'
import { ref } from 'vue'
import { db } from '@/database/db'
import type { PaymentMethod, SavingsMethod, PaymentMethodType } from '@/database/db'
import type { CardBillingSummary, TransactionView } from '@/types'
import { getCurrentBillingPeriod, calcDDay } from '@/utils/cardBilling'
import dayjs from 'dayjs'

export const usePaymentMethodStore = defineStore('paymentMethod', () => {
  const paymentMethods = ref<(PaymentMethod & { id: number })[]>([])
  const savingsMethods = ref<(SavingsMethod & { id: number })[]>([])

  // ── 결제수단 ──────────────────────────────────────────────────────────────

  async function loadPaymentMethods(): Promise<void> {
    const all = await db.paymentMethods.toArray()
    paymentMethods.value = all.filter((p): p is PaymentMethod & { id: number } => p.id !== undefined)
  }

  async function addPaymentMethod(params: {
    name: string
    type: PaymentMethodType
  }): Promise<void> {
    await db.paymentMethods.add({ ...params, isDefault: false })
    await loadPaymentMethods()
  }

  async function deletePaymentMethod(id: number): Promise<void> {
    const linked = await db.transactions.where('paymentMethodId').equals(id).count()
    if (linked > 0) throw new Error('연결된 거래가 있어 삭제할 수 없습니다.')
    await db.paymentMethods.delete(id)
    await loadPaymentMethods()
  }

  async function updateBillingSettings(
    id: number,
    billingCutoffDay: number,
    paymentDueDay: number,
  ): Promise<void> {
    await db.paymentMethods.update(id, { billingCutoffDay, paymentDueDay })
    await loadPaymentMethods()
  }

  async function updatePointBudget(
    id: number,
    totalBudget: number,
    remainingBudget: number,
  ): Promise<void> {
    await db.paymentMethods.update(id, { totalBudget, remainingBudget })
    await loadPaymentMethods()
  }

  // ── 포인트 잔액 ───────────────────────────────────────────────────────────

  async function deductPoint(paymentMethodId: number, amount: number): Promise<void> {
    await db.transaction('rw', db.paymentMethods, async () => {
      const pm = await db.paymentMethods.get(paymentMethodId)
      if (!pm || pm.remainingBudget === undefined) return
      if (pm.remainingBudget < amount) throw new Error('포인트 잔액이 부족합니다.')
      await db.paymentMethods.update(paymentMethodId, {
        remainingBudget: pm.remainingBudget - amount,
      })
    })
    await loadPaymentMethods()
  }

  async function recoverPoint(paymentMethodId: number, amount: number): Promise<void> {
    await db.transaction('rw', db.paymentMethods, async () => {
      const pm = await db.paymentMethods.get(paymentMethodId)
      if (!pm || pm.remainingBudget === undefined) return
      await db.paymentMethods.update(paymentMethodId, {
        remainingBudget: pm.remainingBudget + amount,
      })
    })
    await loadPaymentMethods()
  }

  // ── 저축 수단 ─────────────────────────────────────────────────────────────

  async function loadSavingsMethods(): Promise<void> {
    const all = await db.savingsMethods.toArray()
    savingsMethods.value = all.filter((s): s is SavingsMethod & { id: number } => s.id !== undefined)
  }

  async function addSavingsMethod(name: string): Promise<void> {
    await db.savingsMethods.add({ name, isDefault: false })
    await loadSavingsMethods()
  }

  async function deleteSavingsMethod(id: number): Promise<void> {
    const linked = await db.transactions.where('savingsMethodId').equals(id).count()
    if (linked > 0) throw new Error('연결된 거래가 있어 삭제할 수 없습니다.')
    await db.savingsMethods.delete(id)
    await loadSavingsMethods()
  }

  // ── 카드 청구 현황 ────────────────────────────────────────────────────────

  function getCardBillingSummary(
    transactions: TransactionView[],
    today: string = dayjs().format('YYYY-MM-DD'),
  ): CardBillingSummary[] {
    const result: CardBillingSummary[] = []

    for (const pm of paymentMethods.value) {
      if (pm.type !== 'CreditCard' && pm.type !== 'DebitCard') continue
      if (!pm.billingCutoffDay || !pm.paymentDueDay) continue

      const { periodFrom, periodTo } = getCurrentBillingPeriod(pm.billingCutoffDay, today)

      // 결제일 계산 — 청구 마감(periodTo) 기준 다음 paymentDueDay
      const periodToDay = dayjs(periodTo)
      let dueDate = periodToDay.date(pm.paymentDueDay)
      if (dueDate.isBefore(periodToDay) || dueDate.isSame(periodToDay)) {
        dueDate = dueDate.add(1, 'month')
      }
      const dueDateStr = dueDate.format('YYYY-MM-DD')

      const amount = transactions
        .filter(
          t =>
            t.type === 'Expense' &&
            t.paymentMethodId === pm.id &&
            t.date >= periodFrom &&
            t.date <= periodTo,
        )
        .reduce((sum, t) => sum + t.amount, 0)

      result.push({
        id:         pm.id,
        name:       pm.name,
        dueDate:    dueDateStr,
        periodFrom,
        periodTo,
        amount,
        dDayLabel:  calcDDay(dueDateStr, today),
      })
    }

    return result
  }

  return {
    paymentMethods,
    savingsMethods,
    loadPaymentMethods,
    addPaymentMethod,
    deletePaymentMethod,
    updateBillingSettings,
    updatePointBudget,
    deductPoint,
    recoverPoint,
    loadSavingsMethods,
    addSavingsMethod,
    deleteSavingsMethod,
    getCardBillingSummary,
  }
})
