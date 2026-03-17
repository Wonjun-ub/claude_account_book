// ── 구 백엔드 API 클라이언트 (Sprint 9에서 Dexie로 대체됨, 레거시 보존) ────────

import { apiClient } from './client'
import type {
  Transaction, Category, PaymentMethod,
  RecurringTransaction, InstallmentTransaction, UserSettings,
  MonthlySummary, CategorySummary, MonthlyTrend,
  TransactionView,
} from '@/types'
import type { TransactionType } from '@/database/db'

// 삭제 모드 Literal Types
export type RecurringDeleteMode   = 'all' | 'fromHere' | 'skipMonth'
export type InstallmentDeleteMode = 'all' | 'fromHere' | 'single'

// 인라인 요청 타입 (구 DTO)
interface CreateTransactionRequest {
  amount: number; date: string; memo?: string
  type: TransactionType; categoryId: number; paymentMethodId?: number
  isIncludedInTotal: boolean; recurringTransactionId?: number
}
interface CreateCategoryRequest { name: string; type: TransactionType }
interface CreatePaymentMethodRequest { name: string; type: string }
interface CreateRecurringTransactionRequest {
  amount: number; categoryId: number; paymentMethodId?: number
  dayOfMonth: number; memo?: string; startDate?: string; endDate?: string
}
interface CreateInstallmentTransactionRequest {
  totalAmount: number; totalInstallments: number; startDate: string
  categoryId: number; paymentMethodId: number; memo?: string; isIncludedInTotal: boolean
}

// ── 거래 ──────────────────────────────────────────────────────────────────────
export const transactionsApi = {
  getAll: (params?: Record<string, string | number>) => {
    const query = params ? '?' + new URLSearchParams(params as Record<string, string>).toString() : ''
    return apiClient.get<TransactionView[]>(`/api/transactions${query}`)
  },
  create: (data: CreateTransactionRequest) => apiClient.post<TransactionView>('/api/transactions', data),
  update: (id: number, data: CreateTransactionRequest) => apiClient.put<TransactionView>(`/api/transactions/${id}`, data),
  delete: (id: number) => apiClient.delete(`/api/transactions/${id}`),
}

// ── 카테고리 ──────────────────────────────────────────────────────────────────
export const categoriesApi = {
  getAll: (type?: TransactionType) => {
    const query = type ? `?type=${type}` : ''
    return apiClient.get<Category[]>(`/api/categories${query}`)
  },
  create: (data: CreateCategoryRequest) => apiClient.post<Category>('/api/categories', data),
  delete: (id: number) => apiClient.delete(`/api/categories/${id}`),
}

// ── 결제수단 ──────────────────────────────────────────────────────────────────
export const paymentMethodsApi = {
  getAll: () => apiClient.get<PaymentMethod[]>('/api/payment-methods'),
  create: (data: CreatePaymentMethodRequest) => apiClient.post<PaymentMethod>('/api/payment-methods', data),
  delete: (id: number) => apiClient.delete(`/api/payment-methods/${id}`),
}

// ── 반복 거래 ─────────────────────────────────────────────────────────────────
export const recurringApi = {
  getAll: () => apiClient.get<RecurringTransaction[]>('/api/recurring-transactions'),
  getPending: (year: number, month: number) =>
    apiClient.get<RecurringTransaction[]>(`/api/recurring-transactions/pending?year=${year}&month=${month}`),
  create: (data: CreateRecurringTransactionRequest) =>
    apiClient.post<RecurringTransaction>('/api/recurring-transactions', data),
  deleteWithMode: (id: number, mode: RecurringDeleteMode, params?: { date?: string; year?: number; month?: number }) => {
    const qs = new URLSearchParams({ mode })
    if (params?.date)      qs.append('date',  params.date)
    if (params?.year  != null) qs.append('year',  String(params.year))
    if (params?.month != null) qs.append('month', String(params.month))
    return apiClient.delete(`/api/recurring-transactions/${id}?${qs.toString()}`)
  },
}

// ── 할부 거래 ─────────────────────────────────────────────────────────────────
export const installmentApi = {
  getAll: () => apiClient.get<InstallmentTransaction[]>('/api/installment-transactions'),
  create: (data: CreateInstallmentTransactionRequest) =>
    apiClient.post<InstallmentTransaction>('/api/installment-transactions', data),
  deleteWithMode: (id: number, mode: InstallmentDeleteMode, seq?: number) => {
    const qs = new URLSearchParams({ mode })
    if (seq != null) qs.append('seq', String(seq))
    return apiClient.delete(`/api/installment-transactions/${id}?${qs.toString()}`)
  },
}

// ── 요약/통계 ─────────────────────────────────────────────────────────────────
export const summaryApi = {
  getMonthly:  (year: number, month: number) =>
    apiClient.get<MonthlySummary>(`/api/summary/monthly?year=${year}&month=${month}`),
  getCategory: (year: number, month: number) =>
    apiClient.get<CategorySummary[]>(`/api/summary/category?year=${year}&month=${month}`),
  getTrend:    (months = 6) =>
    apiClient.get<MonthlyTrend[]>(`/api/summary/trend?months=${months}`),
}

// ── 설정 ──────────────────────────────────────────────────────────────────────
export const settingsApi = {
  get:    () => apiClient.get<UserSettings>('/api/settings'),
  update: (monthStartDay: number) => apiClient.put<UserSettings>('/api/settings', { monthStartDay }),
}
