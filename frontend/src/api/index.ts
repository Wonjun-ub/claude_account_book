import { apiClient } from './client'
import type {
  Transaction, Category, PaymentMethod, PointBudget,
  RecurringTransaction, InstallmentTransaction, UserSettings, MonthlySummary,
  CategorySummary, MonthlyTrend,
  CreateTransactionRequest, CreateCategoryRequest,
  CreatePaymentMethodRequest, CreatePointBudgetRequest,
  CreateRecurringTransactionRequest, CreateInstallmentTransactionRequest,
} from '@/types'

// ── 거래 ───────────────────────────────────────────────────────────────────
export const transactionsApi = {
  getAll: (params?: Record<string, string | number>) => {
    const query = params ? '?' + new URLSearchParams(params as Record<string, string>).toString() : ''
    return apiClient.get<Transaction[]>(`/api/transactions${query}`)
  },
  getById: (id: number) => apiClient.get<Transaction>(`/api/transactions/${id}`),
  create: (data: CreateTransactionRequest) => apiClient.post<Transaction>('/api/transactions', data),
  update: (id: number, data: CreateTransactionRequest) => apiClient.put<Transaction>(`/api/transactions/${id}`, data),
  delete: (id: number) => apiClient.delete(`/api/transactions/${id}`),
}

// ── 카테고리 ───────────────────────────────────────────────────────────────
export const categoriesApi = {
  getAll: (type?: string) => {
    const query = type ? `?type=${type}` : ''
    return apiClient.get<Category[]>(`/api/categories${query}`)
  },
  create: (data: CreateCategoryRequest) => apiClient.post<Category>('/api/categories', data),
  update: (id: number, data: CreateCategoryRequest) => apiClient.put<Category>(`/api/categories/${id}`, data),
  delete: (id: number) => apiClient.delete(`/api/categories/${id}`),
}

// ── 결제수단 ───────────────────────────────────────────────────────────────
export const paymentMethodsApi = {
  getAll: (type?: string) => {
    const query = type ? `?type=${type}` : ''
    return apiClient.get<PaymentMethod[]>(`/api/payment-methods${query}`)
  },
  create: (data: CreatePaymentMethodRequest) => apiClient.post<PaymentMethod>('/api/payment-methods', data),
  update: (id: number, data: CreatePaymentMethodRequest) => apiClient.put<PaymentMethod>(`/api/payment-methods/${id}`, data),
  delete: (id: number) => apiClient.delete(`/api/payment-methods/${id}`),
}

// ── 포인트 예산 ────────────────────────────────────────────────────────────
export const pointBudgetsApi = {
  getAll: () => apiClient.get<PointBudget[]>('/api/point-budgets'),
  create: (data: CreatePointBudgetRequest) => apiClient.post<PointBudget>('/api/point-budgets', data),
}

// ── 반복 지출 ──────────────────────────────────────────────────────────────
export const recurringApi = {
  getAll: () => apiClient.get<RecurringTransaction[]>('/api/recurring-transactions'),
  getPending: (year: number, month: number) =>
    apiClient.get<RecurringTransaction[]>(`/api/recurring-transactions/pending?year=${year}&month=${month}`),
  create: (data: CreateRecurringTransactionRequest) => apiClient.post<RecurringTransaction>('/api/recurring-transactions', data),
  // mode: 'all' | 'fromHere' | 'skipMonth', date: YYYY-MM-DD (fromHere), year/month (skipMonth)
  deleteWithMode: (id: number, mode: string, params?: { date?: string; year?: number; month?: number }) => {
    const qs = new URLSearchParams({ mode })
    if (params?.date) qs.append('date', params.date)
    if (params?.year != null) qs.append('year', String(params.year))
    if (params?.month != null) qs.append('month', String(params.month))
    return apiClient.delete(`/api/recurring-transactions/${id}?${qs.toString()}`)
  },
}

// ── 할부 거래 ──────────────────────────────────────────────────────────────
export const installmentApi = {
  getAll: () => apiClient.get<InstallmentTransaction[]>('/api/installment-transactions'),
  create: (data: CreateInstallmentTransactionRequest) => apiClient.post<InstallmentTransaction>('/api/installment-transactions', data),
  // mode: 'all' | 'fromHere' | 'single', seq: 회차 (fromHere/single 필수)
  deleteWithMode: (id: number, mode: string, seq?: number) => {
    const qs = new URLSearchParams({ mode })
    if (seq != null) qs.append('seq', String(seq))
    return apiClient.delete(`/api/installment-transactions/${id}?${qs.toString()}`)
  },
}

// ── 요약/통계 ──────────────────────────────────────────────────────────────
export const summaryApi = {
  getMonthly: (year: number, month: number) =>
    apiClient.get<MonthlySummary>(`/api/summary/monthly?year=${year}&month=${month}`),
  getCategory: (year: number, month: number) =>
    apiClient.get<CategorySummary[]>(`/api/summary/category?year=${year}&month=${month}`),
  getTrend: (months = 6) =>
    apiClient.get<MonthlyTrend[]>(`/api/summary/trend?months=${months}`),
}

// ── 설정 ───────────────────────────────────────────────────────────────────
export const settingsApi = {
  get: () => apiClient.get<UserSettings>('/api/settings'),
  update: (monthStartDay: number) => apiClient.put<UserSettings>('/api/settings', { monthStartDay }),
}
