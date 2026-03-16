export function useCurrencyFormat() {
  // 부호 포함 금액 포맷: +50,000원 / -50,000원
  function formatAmount(amount: number, type: 'Income' | 'Expense'): string {
    const sign = type === 'Income' ? '+' : '-'
    return `${sign}${amount.toLocaleString()}원`
  }

  // 부호 없는 금액 포맷: 50,000원
  function formatAmountAbs(amount: number): string {
    return `${amount.toLocaleString()}원`
  }

  return { formatAmount, formatAmountAbs }
}
