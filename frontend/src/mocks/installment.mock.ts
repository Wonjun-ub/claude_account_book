// Sprint 3 목업용 — Sprint 5에서 실제 API로 교체

export function calcMockInstallment(total: number, months: number) {
  const monthly = Math.floor(total / months)
  const firstExtra = total - monthly * months
  return {
    monthlyAmount: monthly,
    firstMonthAmount: monthly + firstExtra,
  }
}
