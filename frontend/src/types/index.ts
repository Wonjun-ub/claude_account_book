// ── 공통 Enum ──────────────────────────────────────────────────────────────
export type TransactionType = 'Income' | 'Expense'
export type CategoryType = 'Income' | 'Expense'
export type PaymentMethodType = 'Cash' | 'Card' | 'Point'
export type RecurringType = 'Fixed'

// ── 엔티티 ─────────────────────────────────────────────────────────────────
export interface Category {
  id: number
  name: string
  type: CategoryType
  isDefault: boolean
}

export interface PointBudget {
  id: number
  name: string
  totalAmount: number
  remainingAmount: number
}

export interface PaymentMethod {
  id: number
  name: string
  type: PaymentMethodType
  isDefault: boolean
  pointBudgetId?: number
  pointBudget?: PointBudget
}

export interface Transaction {
  id: number
  amount: number
  date: string
  memo?: string
  type: TransactionType
  categoryId: number
  categoryName: string
  paymentMethodId: number
  paymentMethodName: string
  isIncludedInTotal: boolean
  recurringTransactionId?: number
  // 할부 관련 (nullable)
  installmentTransactionId?: number
  installmentSequence?: number
  installmentTotalInstallments?: number
}

export interface RecurringTransaction {
  id: number
  amount: number
  categoryId: number
  categoryName: string
  paymentMethodId: number
  paymentMethodName: string
  type: RecurringType
  dayOfMonth: number
  memo?: string
  isActive: boolean
  startDate?: string
  endDate?: string
}

export interface InstallmentTransaction {
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
  memo?: string
  isActive: boolean
}

export interface UserSettings {
  id: number
  monthStartDay: number
}

// ── 요약/통계 ──────────────────────────────────────────────────────────────
export interface MonthlySummary {
  year: number
  month: number
  totalIncome: number
  totalExpense: number
  balance: number
  monthOverMonthChange: number
  periodStart: string
  periodEnd: string
  incomeCount: number
  expenseCount: number
}

export interface CategorySummary {
  categoryId: number
  categoryName: string
  categoryType: CategoryType
  amount: number
  percentage: number
}

export interface MonthlyTrend {
  year: number
  month: number
  income: number
  expense: number
}

// ── 요청 DTO ───────────────────────────────────────────────────────────────
export interface CreateTransactionRequest {
  amount: number
  date: string
  memo?: string
  type: TransactionType
  categoryId: number
  paymentMethodId: number
  isIncludedInTotal: boolean
  recurringTransactionId?: number
}

export interface CreateCategoryRequest {
  name: string
  type: CategoryType
}

export interface CreatePaymentMethodRequest {
  name: string
  type: PaymentMethodType
  pointBudgetId?: number
}

export interface CreatePointBudgetRequest {
  name: string
  totalAmount: number
}

export interface CreateRecurringTransactionRequest {
  amount: number
  categoryId: number
  paymentMethodId: number
  dayOfMonth: number
  memo?: string
  startDate?: string
  endDate?: string
}

export interface CreateInstallmentTransactionRequest {
  totalAmount: number
  totalInstallments: number
  startDate: string
  categoryId: number
  paymentMethodId: number
  memo?: string
  isIncludedInTotal: boolean
}
