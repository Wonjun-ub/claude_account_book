// Sprint 3 목업용 — Sprint 5에서 실제 API로 교체

export interface MockInstallmentInput {
  totalAmount: number
  totalInstallments: number
  startDate: string
  categoryId: number
  paymentMethodId: number
  memo?: string
}

export function calcMockInstallment(total: number, months: number) {
  const monthly = Math.floor(total / months)
  const firstExtra = total - monthly * months
  return {
    monthlyAmount: monthly,
    firstMonthAmount: monthly + firstExtra,
  }
}

export async function mockCreateInstallment(input: MockInstallmentInput) {
  console.log('[Mock] 할부 등록 (Sprint 5에서 실제 API로 교체):', input)
  return { id: -1, ...input }
}
