// ── db.ts 타입 re-export ────────────────────────────────────────────────────
export type {
  TransactionType,
  PaymentMethodType,
  Transaction,
  Category,
  PaymentMethod,
  SavingsMethod,
  RecurringTransaction,
  RecurringSkip,
  InstallmentTransaction,
  UserSettings,
} from '@/database/db'

// ── 뷰 전용 composite 타입 ──────────────────────────────────────────────────

/** 거래 + 조인된 이름 필드 (목록 표시용) */
export interface TransactionView {
  id:                        number
  amount:                    number
  date:                      string
  type:                      import('@/database/db').TransactionType
  categoryId:                number
  categoryName:              string
  paymentMethodId?:          number
  paymentMethodName?:        string
  savingsMethodId?:          number
  savingsMethodName?:        string
  memo?:                     string
  isIncludedInTotal:         boolean
  recurringTransactionId?:   number
  installmentTransactionId?: number
  installmentSequence?:      number
  installmentTotalInstallments?: number  // "N/M회" 표시용
}

/** 월별 요약 (로컬 계산) */
export interface MonthlySummary {
  year:         number
  month:        number
  totalIncome:  number
  totalExpense: number
  totalSavings: number
  balance:      number          // totalIncome - totalExpense
  periodStart:  string          // YYYY-MM-DD
  periodEnd:    string          // YYYY-MM-DD
  incomeCount:  number
  expenseCount: number
  savingsCount: number
}

/** 카테고리별 집계 (통계 탭) */
export interface CategorySummary {
  categoryId:   number
  categoryName: string
  categoryType: import('@/database/db').TransactionType
  amount:       number
  percentage:   number
}

/** 카드별 청구 현황 (카드 결제 위젯) */
export interface CardBillingSummary {
  id:          number
  name:        string
  dueDate:     string    // YYYY-MM-DD — 결제일
  periodFrom:  string    // YYYY-MM-DD — 청구 시작일
  periodTo:    string    // YYYY-MM-DD — 청구 종료일
  amount:      number    // 해당 청구 기간 내 합계
  dDayLabel:   string    // "D-3", "D-day", "D+1" 등
}

/** 월별 추이 (6개월 차트) */
export interface MonthlyTrend {
  year:    number
  month:   number
  income:  number
  expense: number
  savings: number
}

// ── 스토어 action 파라미터 타입 ──────────────────────────────────────────────

export interface CreateTransactionParams {
  amount:            number
  date:              string
  type:              import('@/database/db').TransactionType
  categoryId:        number
  paymentMethodId?:  number
  savingsMethodId?:  number
  memo?:             string
  isIncludedInTotal: boolean
}

export interface CreateInstallmentParams {
  totalAmount:       number
  totalInstallments: number
  startDate:         string
  categoryId:        number
  paymentMethodId:   number
  memo?:             string
  isIncludedInTotal: boolean
}

export interface CreateRecurringParams {
  amount:           number
  type:             import('@/database/db').TransactionType
  categoryId:       number
  paymentMethodId?: number
  savingsMethodId?: number
  dayOfMonth:       number
  startDate:        string
  endDate?:         string
  memo?:            string
}

export interface UpdateInstallmentParams {
  totalAmount:       number
  totalInstallments: number
  categoryId:        number
  paymentMethodId:   number
  memo?:             string
}

export interface UpdateRecurringParams {
  amount?:          number
  categoryId?:      number
  paymentMethodId?: number
  savingsMethodId?: number
  dayOfMonth?:      number
  endDate?:         string
  memo?:            string
}
