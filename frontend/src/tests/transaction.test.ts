/**
 * Sprint 9 Task 2 — 거래 CRUD Dexie 테스트
 *
 * fake-indexeddb(v3.1.7)로 IndexedDB를 폴리필하여 실제 Dexie DB 동작을 검증한다.
 * 각 테스트는 beforeEach에서 DB를 초기화하여 독립적으로 실행된다.
 */
import { describe, it, expect, beforeEach } from 'vitest'
import {
  db,
  seedDatabase,
  calcInstallment,
  createInstallment,
  getRecurringPending,
  applyRecurringForMonth,
} from '@/database/db'

// ── DB 초기화 헬퍼 ────────────────────────────────────────────────────────────

async function resetDb() {
  // 기존 DB를 닫고 삭제 후 재오픈
  await db.close()
  await db.delete()
  await db.open()
}

// ── 테스트용 기본 데이터 ────────────────────────────────────────────────────────

async function insertSeedData() {
  await db.categories.bulkAdd([
    { id: 1, name: '식비',   type: 'Expense', isDefault: true,  isDeleted: false },
    { id: 2, name: '급여',   type: 'Income',  isDefault: true,  isDeleted: false },
    { id: 3, name: '적금',   type: 'Savings', isDefault: true,  isDeleted: false },
  ])
  await db.paymentMethods.bulkAdd([
    { id: 1, name: '현금', type: 'Cash', isDefault: true },
    { id: 2, name: 'POINT', type: 'Point', isDefault: false, totalBudget: 10000, remainingBudget: 10000 },
  ])
  await db.savingsMethods.add({ id: 1, name: '기업은행', isDefault: true })
  await db.userSettings.add({ id: 1, monthStartDay: 1 })
}

// ── 테스트 ────────────────────────────────────────────────────────────────────

describe('calcInstallment', () => {

  it('TC-DB-INST-01: 300,000 / 3개월 → 균등 분할', () => {
    const r = calcInstallment(300000, 3)
    expect(r.monthlyAmount).toBe(100000)
    expect(r.firstMonthAmount).toBe(100000)
  })

  it('TC-DB-INST-02: 100,000 / 3개월 → 나머지 1회차 합산', () => {
    const r = calcInstallment(100000, 3)
    expect(r.monthlyAmount).toBe(33333)
    expect(r.firstMonthAmount).toBe(33334)
    expect(r.firstMonthAmount + r.monthlyAmount * 2).toBe(100000)
  })

})

describe('seedDatabase', () => {
  beforeEach(resetDb)

  it('TC-DB-SEED-01: 최초 실행 시 기본 카테고리/결제수단/저축수단/설정 삽입', async () => {
    await seedDatabase()

    const cats    = await db.categories.count()
    const pms     = await db.paymentMethods.count()
    const sms     = await db.savingsMethods.count()
    const settings = await db.userSettings.count()

    expect(cats).toBeGreaterThan(0)
    expect(pms).toBeGreaterThan(0)
    expect(sms).toBeGreaterThan(0)
    expect(settings).toBe(1)
  })

  it('TC-DB-SEED-02: 중복 실행 시 멱등 (데이터 중복 삽입 없음)', async () => {
    await seedDatabase()
    await seedDatabase()

    const settings = await db.userSettings.count()
    expect(settings).toBe(1)
  })

})

describe('단건 거래 CRUD', () => {
  beforeEach(async () => {
    await resetDb()
    await insertSeedData()
  })

  it('TC-DB-TX-01: 단건 지출 등록 후 조회', async () => {
    const id = await db.transactions.add({
      amount:            5000,
      date:              '2026-03-15',
      type:              'Expense',
      categoryId:        1,
      paymentMethodId:   1,
      isIncludedInTotal: true,
    })

    const tx = await db.transactions.get(id as number)
    expect(tx).toBeDefined()
    expect(tx!.amount).toBe(5000)
    expect(tx!.type).toBe('Expense')
  })

  it('TC-DB-TX-02: 단건 수입 등록', async () => {
    const id = await db.transactions.add({
      amount:            300000,
      date:              '2026-03-10',
      type:              'Income',
      categoryId:        2,
      isIncludedInTotal: true,
    })

    const tx = await db.transactions.get(id as number)
    expect(tx!.type).toBe('Income')
    expect(tx!.amount).toBe(300000)
  })

  it('TC-DB-TX-03: 저축 거래 등록 (savingsMethodId)', async () => {
    const id = await db.transactions.add({
      amount:            50000,
      date:              '2026-03-20',
      type:              'Savings',
      categoryId:        3,
      savingsMethodId:   1,
      isIncludedInTotal: true,
    })

    const tx = await db.transactions.get(id as number)
    expect(tx!.type).toBe('Savings')
    expect(tx!.savingsMethodId).toBe(1)
  })

  it('TC-DB-TX-04: 거래 수정 후 값 갱신 확인', async () => {
    const id = await db.transactions.add({
      amount: 1000, date: '2026-03-01', type: 'Expense',
      categoryId: 1, paymentMethodId: 1, isIncludedInTotal: true,
    })

    await db.transactions.update(id as number, { amount: 2000, memo: '수정됨' })
    const tx = await db.transactions.get(id as number)
    expect(tx!.amount).toBe(2000)
    expect(tx!.memo).toBe('수정됨')
  })

  it('TC-DB-TX-05: 거래 삭제 후 조회 시 undefined', async () => {
    const id = await db.transactions.add({
      amount: 500, date: '2026-03-05', type: 'Expense',
      categoryId: 1, paymentMethodId: 1, isIncludedInTotal: true,
    })

    await db.transactions.delete(id as number)
    const tx = await db.transactions.get(id as number)
    expect(tx).toBeUndefined()
  })

  it('TC-DB-TX-06: 날짜 범위 쿼리 — 범위 내 항목만 반환', async () => {
    await db.transactions.bulkAdd([
      { amount: 100, date: '2026-02-28', type: 'Expense', categoryId: 1, isIncludedInTotal: true },
      { amount: 200, date: '2026-03-01', type: 'Expense', categoryId: 1, isIncludedInTotal: true },
      { amount: 300, date: '2026-03-15', type: 'Expense', categoryId: 1, isIncludedInTotal: true },
      { amount: 400, date: '2026-03-31', type: 'Expense', categoryId: 1, isIncludedInTotal: true },
      { amount: 500, date: '2026-04-01', type: 'Expense', categoryId: 1, isIncludedInTotal: true },
    ])

    const march = await db.transactions
      .where('date').between('2026-03-01', '2026-03-31', true, true)
      .toArray()

    expect(march.length).toBe(3)
    expect(march.map(t => t.amount).sort()).toEqual([200, 300, 400])
  })

})

describe('createInstallment', () => {
  beforeEach(async () => {
    await resetDb()
    await insertSeedData()
  })

  it('TC-DB-INST-03: 300,000 / 3개월 → 3건 Transaction 생성', async () => {
    const masterId = await createInstallment({
      totalAmount:       300000,
      totalInstallments: 3,
      startDate:         '2026-03-01',
      categoryId:        1,
      paymentMethodId:   1,
      isIncludedInTotal: true,
    })

    const txs = await db.transactions
      .where('installmentTransactionId').equals(masterId)
      .toArray()

    expect(txs.length).toBe(3)
    expect(txs[0]!.amount).toBe(100000)  // 1회차 (나머지 없음)
    expect(txs[1]!.amount).toBe(100000)
    expect(txs[2]!.amount).toBe(100000)
  })

  it('TC-DB-INST-04: 100,000 / 3개월 → 1회차 금액 보정', async () => {
    const masterId = await createInstallment({
      totalAmount:       100000,
      totalInstallments: 3,
      startDate:         '2026-03-01',
      categoryId:        1,
      paymentMethodId:   1,
      isIncludedInTotal: true,
    })

    const txs = await db.transactions
      .where('installmentTransactionId').equals(masterId)
      .sortBy('installmentSequence')

    expect(txs[0]!.amount).toBe(33334)  // 1회차 = 33333 + 나머지 1
    expect(txs[1]!.amount).toBe(33333)
    expect(txs[2]!.amount).toBe(33333)
    expect(txs[0]!.amount + txs[1]!.amount + txs[2]!.amount).toBe(100000)
  })

  it('TC-DB-INST-05: 날짜가 매월 +1개월씩 증가', async () => {
    const masterId = await createInstallment({
      totalAmount: 30000, totalInstallments: 3,
      startDate: '2026-03-15', categoryId: 1, paymentMethodId: 1, isIncludedInTotal: true,
    })

    const txs = await db.transactions
      .where('installmentTransactionId').equals(masterId)
      .sortBy('installmentSequence')

    expect(txs[0]!.date).toBe('2026-03-15')
    expect(txs[1]!.date).toBe('2026-04-15')
    expect(txs[2]!.date).toBe('2026-05-15')
  })

  it('TC-DB-INST-06: 원부 isActive = true', async () => {
    const masterId = await createInstallment({
      totalAmount: 60000, totalInstallments: 2,
      startDate: '2026-03-01', categoryId: 1, paymentMethodId: 1, isIncludedInTotal: true,
    })

    const master = await db.installmentTransactions.get(masterId)
    expect(master!.isActive).toBe(true)
  })

})

describe('반복 거래 (getRecurringPending / applyRecurringForMonth)', () => {
  beforeEach(async () => {
    await resetDb()
    await insertSeedData()
  })

  it('TC-DB-RECUR-01: 신규 반복 등록 → pending에 포함', async () => {
    await db.recurringTransactions.add({
      amount: 10000, type: 'Expense', categoryId: 1, paymentMethodId: 1,
      dayOfMonth: 15, startDate: '2026-03-01', isActive: true,
    })

    const pending = await getRecurringPending(2026, 3, 1)
    expect(pending.length).toBe(1)
    expect(pending[0]!.amount).toBe(10000)
  })

  it('TC-DB-RECUR-02: applyRecurringForMonth 후 pending에서 제거', async () => {
    await db.recurringTransactions.add({
      amount: 10000, type: 'Expense', categoryId: 1, paymentMethodId: 1,
      dayOfMonth: 15, startDate: '2026-03-01', isActive: true,
    })

    await applyRecurringForMonth(2026, 3, 1)

    const pending = await getRecurringPending(2026, 3, 1)
    expect(pending.length).toBe(0)

    const txs = await db.transactions.toArray()
    expect(txs.length).toBe(1)
    expect(txs[0]!.date).toBe('2026-03-15')
  })

  it('TC-DB-RECUR-03: applyRecurringForMonth 멱등 — 중복 생성 없음', async () => {
    await db.recurringTransactions.add({
      amount: 5000, type: 'Income', categoryId: 2,
      dayOfMonth: 10, startDate: '2026-03-01', isActive: true,
    })

    await applyRecurringForMonth(2026, 3, 1)
    await applyRecurringForMonth(2026, 3, 1)  // 2번 호출

    const txs = await db.transactions.toArray()
    expect(txs.length).toBe(1)  // 1건만 생성
  })

  it('TC-DB-RECUR-04: RecurringSkip 등록 시 pending에서 제외', async () => {
    const rid = await db.recurringTransactions.add({
      amount: 3000, type: 'Expense', categoryId: 1, paymentMethodId: 1,
      dayOfMonth: 5, startDate: '2026-03-01', isActive: true,
    })

    await db.recurringSkips.add({ recurringTransactionId: rid as number, year: 2026, month: 3 })

    const pending = await getRecurringPending(2026, 3, 1)
    expect(pending.length).toBe(0)
  })

  it('TC-DB-RECUR-05: isActive=false 반복은 pending에서 제외', async () => {
    await db.recurringTransactions.add({
      amount: 7000, type: 'Expense', categoryId: 1, paymentMethodId: 1,
      dayOfMonth: 20, startDate: '2026-03-01', isActive: false,  // 비활성
    })

    const pending = await getRecurringPending(2026, 3, 1)
    expect(pending.length).toBe(0)
  })

  it('TC-DB-RECUR-06: 시작일 이후 월만 pending에 포함', async () => {
    // 4월 시작 반복 → 3월에는 pending 없음
    await db.recurringTransactions.add({
      amount: 2000, type: 'Expense', categoryId: 1, paymentMethodId: 1,
      dayOfMonth: 1, startDate: '2026-04-01', isActive: true,
    })

    const march = await getRecurringPending(2026, 3, 1)
    const april = await getRecurringPending(2026, 4, 1)

    expect(march.length).toBe(0)
    expect(april.length).toBe(1)
  })

  it('TC-DB-RECUR-07: endDate 이후 월은 pending에서 제외', async () => {
    // 3월에 종료되는 반복 → 4월에는 pending 없음
    await db.recurringTransactions.add({
      amount: 1500, type: 'Expense', categoryId: 1, paymentMethodId: 1,
      dayOfMonth: 10, startDate: '2026-01-10', endDate: '2026-03-31', isActive: true,
    })

    const march = await getRecurringPending(2026, 3, 1)
    const april = await getRecurringPending(2026, 4, 1)

    expect(march.length).toBe(1)
    expect(april.length).toBe(0)
  })

})

describe('포인트 잔액 차감/복구', () => {
  beforeEach(async () => {
    await resetDb()
    await insertSeedData()
    // 포인트 결제수단: totalBudget=10000, remainingBudget=10000 (id=2)
  })

  it('TC-DB-POINT-01: 포인트 거래 생성 시 잔액 차감', async () => {
    await db.transaction('rw', db.paymentMethods, db.transactions, async () => {
      const pm = await db.paymentMethods.get(2)
      await db.paymentMethods.update(2, { remainingBudget: pm!.remainingBudget! - 3000 })
      await db.transactions.add({
        amount: 3000, date: '2026-03-10', type: 'Expense',
        categoryId: 1, paymentMethodId: 2, isIncludedInTotal: true,
      })
    })

    const pm = await db.paymentMethods.get(2)
    expect(pm!.remainingBudget).toBe(7000)
  })

  it('TC-DB-POINT-02: 포인트 거래 삭제 시 잔액 복구', async () => {
    // 먼저 차감
    await db.paymentMethods.update(2, { remainingBudget: 7000 })
    const txId = await db.transactions.add({
      amount: 3000, date: '2026-03-10', type: 'Expense',
      categoryId: 1, paymentMethodId: 2, isIncludedInTotal: true,
    })

    // 삭제 + 복구
    await db.transaction('rw', db.paymentMethods, db.transactions, async () => {
      const pm = await db.paymentMethods.get(2)
      await db.paymentMethods.update(2, { remainingBudget: pm!.remainingBudget! + 3000 })
      await db.transactions.delete(txId as number)
    })

    const pm = await db.paymentMethods.get(2)
    expect(pm!.remainingBudget).toBe(10000)
    const tx = await db.transactions.get(txId as number)
    expect(tx).toBeUndefined()
  })

  it('TC-DB-POINT-03: 잔액 부족 시 거래 등록 불가 (throw)', async () => {
    await db.paymentMethods.update(2, { remainingBudget: 1000 })

    await expect(
      db.transaction('rw', db.paymentMethods, db.transactions, async () => {
        const pm = await db.paymentMethods.get(2)
        if (pm!.remainingBudget! < 5000) throw new Error('포인트 잔액이 부족합니다.')
        await db.transactions.add({
          amount: 5000, date: '2026-03-10', type: 'Expense',
          categoryId: 1, paymentMethodId: 2, isIncludedInTotal: true,
        })
      })
    ).rejects.toThrow('포인트 잔액이 부족합니다.')

    // DB 롤백 확인
    const txs = await db.transactions.toArray()
    expect(txs.length).toBe(0)
  })

})

describe('할부 삭제 (all / fromHere / single)', () => {
  beforeEach(async () => {
    await resetDb()
    await insertSeedData()
  })

  async function createTestInstallment() {
    return createInstallment({
      totalAmount: 30000, totalInstallments: 3,
      startDate: '2026-03-01', categoryId: 1, paymentMethodId: 1, isIncludedInTotal: true,
    })
  }

  it('TC-DB-DEL-01: mode=all → 원부 + 전체 회차 삭제', async () => {
    const masterId = await createTestInstallment()

    await db.transaction('rw', [db.installmentTransactions, db.transactions], async () => {
      await db.transactions.where('installmentTransactionId').equals(masterId).delete()
      await db.installmentTransactions.delete(masterId)
    })

    const master = await db.installmentTransactions.get(masterId)
    const txs    = await db.transactions.where('installmentTransactionId').equals(masterId).toArray()
    expect(master).toBeUndefined()
    expect(txs.length).toBe(0)
  })

  it('TC-DB-DEL-02: mode=fromHere(seq=2) → 2회차 이후 삭제, 1회차 유지', async () => {
    const masterId = await createTestInstallment()

    await db.transactions
      .where('installmentTransactionId').equals(masterId)
      .filter(t => (t.installmentSequence ?? 0) >= 2)
      .delete()

    const remaining = await db.transactions
      .where('installmentTransactionId').equals(masterId).toArray()
    expect(remaining.length).toBe(1)
    expect(remaining[0]!.installmentSequence).toBe(1)
  })

  it('TC-DB-DEL-03: mode=single(seq=2) → 2회차만 삭제', async () => {
    const masterId = await createTestInstallment()

    await db.transactions
      .where('installmentTransactionId').equals(masterId)
      .filter(t => t.installmentSequence === 2)
      .delete()

    const remaining = await db.transactions
      .where('installmentTransactionId').equals(masterId).toArray()
    expect(remaining.length).toBe(2)
    const seqs = remaining.map(t => t.installmentSequence).sort()
    expect(seqs).toEqual([1, 3])
  })

})

describe('cardBilling.ts 유틸', () => {

  it('TC-DB-CARD-01: 오늘이 정산일 이후 → 정산일+1 ~ 다음달 정산일', async () => {
    const { getCurrentBillingPeriod } = await import('@/utils/cardBilling')
    const { periodFrom, periodTo } = getCurrentBillingPeriod(15, '2026-03-16')
    expect(periodFrom).toBe('2026-03-16')
    expect(periodTo).toBe('2026-04-15')
  })

  it('TC-DB-CARD-02: 오늘이 정산일 이전 → 전월 정산일+1 ~ 당월 정산일', async () => {
    const { getCurrentBillingPeriod } = await import('@/utils/cardBilling')
    const { periodFrom, periodTo } = getCurrentBillingPeriod(15, '2026-03-10')
    expect(periodFrom).toBe('2026-02-16')
    expect(periodTo).toBe('2026-03-15')
  })

  it('TC-DB-CARD-03: D-day 계산', async () => {
    const { calcDDay } = await import('@/utils/cardBilling')
    expect(calcDDay('2026-03-20', '2026-03-17')).toBe('D-3')
    expect(calcDDay('2026-03-17', '2026-03-17')).toBe('D-day')
    expect(calcDDay('2026-03-15', '2026-03-17')).toBe('D+2')
  })

})
