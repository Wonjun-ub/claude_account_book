import Dexie, { type Table } from 'dexie'
import dayjs from 'dayjs'
import { getMonthPeriod } from '@/utils/monthPeriod'

// ── 타입 정의 ─────────────────────────────────────────────────────────────────

export type TransactionType   = 'Income' | 'Expense' | 'Savings'
export type PaymentMethodType = 'Cash' | 'CreditCard' | 'DebitCard' | 'Point'

export interface Transaction {
  id?:                       number
  amount:                    number
  date:                      string          // YYYY-MM-DD
  type:                      TransactionType
  categoryId:                number
  paymentMethodId?:          number          // Expense 전용
  savingsMethodId?:          number          // Savings 전용
  memo?:                     string
  isIncludedInTotal:         boolean
  recurringTransactionId?:   number          // 반복 원부 참조
  installmentTransactionId?: number          // 할부 원부 참조
  installmentSequence?:      number          // 할부 회차 (1-based)
}

export interface Category {
  id?:       number
  name:      string
  type:      TransactionType
  isDefault: boolean
  isDeleted: boolean          // soft-delete: 기존 거래 보존용
}

export interface PaymentMethod {
  id?:              number
  name:             string
  type:             PaymentMethodType
  isDefault:        boolean
  billingCutoffDay?: number   // CreditCard/DebitCard — 정산일 (이용 마감)
  paymentDueDay?:   number    // CreditCard/DebitCard — 결제일 (출금일)
  totalBudget?:     number    // Point — 총 예산
  remainingBudget?: number    // Point — 현재 잔액
}

export interface SavingsMethod {
  id?:       number
  name:      string
  isDefault: boolean
}

export interface RecurringTransaction {
  id?:              number
  amount:           number
  type:             TransactionType
  categoryId:       number
  paymentMethodId?: number    // Expense / Income 전용
  savingsMethodId?: number    // Savings 전용
  dayOfMonth:       number    // 매월 반복 일자 (1~28)
  startDate:        string    // YYYY-MM-DD
  endDate?:         string    // YYYY-MM-DD — undefined = 무기한
  isActive:         boolean
  memo?:            string
}

export interface RecurringSkip {
  id?:                    number
  recurringTransactionId: number
  year:                   number
  month:                  number  // 1~12
}

export interface InstallmentTransaction {
  id?:               number
  totalAmount:       number
  monthlyAmount:     number    // floor(totalAmount / totalInstallments)
  firstMonthAmount:  number    // monthlyAmount + (totalAmount % totalInstallments)
  totalInstallments: number
  startDate:         string    // YYYY-MM-DD — 첫 회차 날짜
  categoryId:        number
  paymentMethodId:   number
  memo?:             string
  isActive:          boolean
}

export interface UserSettings {
  id?:           number
  monthStartDay: number    // 커스텀 월 시작일 (1~28, 기본값: 1)
}

// ── Dexie 클래스 ──────────────────────────────────────────────────────────────

class BudgetTrackerDB extends Dexie {
  transactions!:           Table<Transaction>
  categories!:             Table<Category>
  paymentMethods!:         Table<PaymentMethod>
  savingsMethods!:         Table<SavingsMethod>
  recurringTransactions!:  Table<RecurringTransaction>
  recurringSkips!:         Table<RecurringSkip>
  installmentTransactions!: Table<InstallmentTransaction>
  userSettings!:           Table<UserSettings>

  constructor() {
    super('BudgetTrackerDB')

    /**
     * version(1) — 초기 스키마
     *
     * 인덱스 표기:
     *   ++id          → auto-increment PK
     *   date          → 단일 인덱스 (날짜 범위 쿼리)
     *   [year+month]  → 복합 인덱스
     */
    this.version(1).stores({
      transactions:
        '++id, date, type, categoryId, paymentMethodId, savingsMethodId, ' +
        'recurringTransactionId, installmentTransactionId',

      categories:
        '++id, type, isDeleted',

      paymentMethods:
        '++id, type',

      savingsMethods:
        '++id',

      recurringTransactions:
        '++id, type, isActive, categoryId',

      recurringSkips:
        '++id, recurringTransactionId, [recurringTransactionId+year+month]',

      installmentTransactions:
        '++id, isActive',

      userSettings:
        '++id',
    })
  }
}

export const db = new BudgetTrackerDB()

// ── 초기 시드 데이터 ──────────────────────────────────────────────────────────

/**
 * 앱 최초 실행 시 한 번만 기본 데이터를 삽입한다.
 * userSettings 레코드가 존재하면 이미 초기화된 것으로 간주하고 건너뜀.
 */
export async function seedDatabase(): Promise<void> {
  const alreadySeeded = (await db.userSettings.count()) > 0
  if (alreadySeeded) return

  await db.transaction(
    'rw',
    [db.userSettings, db.categories, db.paymentMethods, db.savingsMethods],
    async () => {
      // 사용자 설정 (월 시작일 기본값 1일)
      await db.userSettings.add({ monthStartDay: 1 })

      // 기본 카테고리 — 지출
      await db.categories.bulkAdd([
        { name: '식비',   type: 'Expense', isDefault: true, isDeleted: false },
        { name: '교통',   type: 'Expense', isDefault: true, isDeleted: false },
        { name: '쇼핑',   type: 'Expense', isDefault: true, isDeleted: false },
        { name: '의료',   type: 'Expense', isDefault: true, isDeleted: false },
        { name: '문화',   type: 'Expense', isDefault: true, isDeleted: false },
        { name: '공과금', type: 'Expense', isDefault: true, isDeleted: false },
        { name: '기타',   type: 'Expense', isDefault: true, isDeleted: false },
      ])

      // 기본 카테고리 — 수입
      await db.categories.bulkAdd([
        { name: '급여', type: 'Income', isDefault: true, isDeleted: false },
        { name: '부업', type: 'Income', isDefault: true, isDeleted: false },
        { name: '기타', type: 'Income', isDefault: true, isDeleted: false },
      ])

      // 기본 카테고리 — 저축
      await db.categories.bulkAdd([
        { name: '적금',   type: 'Savings', isDefault: true, isDeleted: false },
        { name: '청약',   type: 'Savings', isDefault: true, isDeleted: false },
        { name: '비상금', type: 'Savings', isDefault: true, isDeleted: false },
      ])

      // 기본 결제수단
      await db.paymentMethods.bulkAdd([
        { name: '현금', type: 'Cash', isDefault: true },
      ])

      // 기본 저축 수단
      await db.savingsMethods.bulkAdd([
        { name: '기업은행',   isDefault: true },
        { name: '카카오뱅크', isDefault: true },
        { name: '현금',       isDefault: false },
      ])
    },
  )
}

// ── 할부 계산 헬퍼 ────────────────────────────────────────────────────────────

/**
 * 카드사 방식 할부 금액 계산
 * - monthlyAmount  = floor(totalAmount / months)
 * - firstMonthAmount = monthlyAmount + (totalAmount mod months)  ← 나머지를 1회차에 합산
 */
export function calcInstallment(
  totalAmount: number,
  months: number,
): { monthlyAmount: number; firstMonthAmount: number } {
  const monthly = Math.floor(totalAmount / months)
  const first   = monthly + (totalAmount % months)
  return { monthlyAmount: monthly, firstMonthAmount: first }
}

/**
 * 할부 원부 등록 + 회차별 Transaction N건 일괄 생성
 * startDate 기준으로 매월 +1개월씩 날짜를 증가한다.
 */
export async function createInstallment(params: {
  totalAmount:       number
  totalInstallments: number
  startDate:         string   // YYYY-MM-DD
  categoryId:        number
  paymentMethodId:   number
  memo?:             string
  isIncludedInTotal: boolean
}): Promise<number> {
  const { monthlyAmount, firstMonthAmount } = calcInstallment(
    params.totalAmount,
    params.totalInstallments,
  )

  return db.transaction(
    'rw',
    [db.installmentTransactions, db.transactions],
    async () => {
      const masterId = await db.installmentTransactions.add({
        totalAmount:       params.totalAmount,
        monthlyAmount,
        firstMonthAmount,
        totalInstallments: params.totalInstallments,
        startDate:         params.startDate,
        categoryId:        params.categoryId,
        paymentMethodId:   params.paymentMethodId,
        memo:              params.memo,
        isActive:          true,
      })

      for (let i = 0; i < params.totalInstallments; i++) {
        const dateStr = dayjs(params.startDate).add(i, 'month').format('YYYY-MM-DD')

        await db.transactions.add({
          amount:                    i === 0 ? firstMonthAmount : monthlyAmount,
          date:                      dateStr,
          type:                      'Expense',
          categoryId:                params.categoryId,
          paymentMethodId:           params.paymentMethodId,
          memo:                      params.memo,
          isIncludedInTotal:         params.isIncludedInTotal,
          installmentTransactionId:  masterId as number,
          installmentSequence:       i + 1,
        })
      }

      return masterId as number
    },
  )
}

// ── 반복 거래 헬퍼 ────────────────────────────────────────────────────────────

/**
 * 특정 월에 아직 생성되지 않은 반복 거래 목록을 반환한다. (pending 조회)
 * - isActive = true
 * - startDate <= periodEnd
 * - endDate가 없거나 >= periodStart
 * - 해당 월에 RecurringSkip 없음
 * - 해당 월에 이미 생성된 Transaction이 없음
 */
export async function getRecurringPending(
  year: number,
  month: number,
  monthStartDay: number,
): Promise<RecurringTransaction[]> {
  const { start, end } = getMonthPeriod(year, month, monthStartDay)

  const allActive = await db.recurringTransactions
    .filter(r => r.isActive)
    .toArray()

  const result: RecurringTransaction[] = []

  for (const r of allActive) {
    if (!r.id) continue

    // 시작일 ~ 종료일 범위 체크
    if (r.startDate > end)                           continue
    if (r.endDate && r.endDate < start)              continue

    // 스킵 여부 확인
    const skipped = await db.recurringSkips
      .where('[recurringTransactionId+year+month]')
      .equals([r.id, year, month])
      .count()
    if (skipped > 0) continue

    // 이미 생성된 거래 여부 확인
    const exists = await db.transactions
      .where('recurringTransactionId')
      .equals(r.id)
      .filter(t => t.date >= start && t.date <= end)
      .count()
    if (exists > 0) continue

    result.push(r)
  }

  return result
}

/**
 * 반복 거래 적용 — 해당 월에 Transaction을 생성한다. (멱등)
 * pending 목록에 있는 항목에 대해서만 생성.
 */
export async function applyRecurringForMonth(
  year: number,
  month: number,
  monthStartDay: number,
): Promise<void> {
  const pending = await getRecurringPending(year, month, monthStartDay)

  for (const r of pending) {
    if (!r.id) continue

    const dateStr = `${year}-${String(month).padStart(2, '0')}-${String(r.dayOfMonth).padStart(2, '0')}`

    await db.transactions.add({
      amount:                  r.amount,
      date:                    dateStr,
      type:                    r.type,
      categoryId:              r.categoryId,
      paymentMethodId:         r.paymentMethodId,
      savingsMethodId:         r.savingsMethodId,
      memo:                    r.memo,
      isIncludedInTotal:       true,
      recurringTransactionId:  r.id,
    })
  }
}
