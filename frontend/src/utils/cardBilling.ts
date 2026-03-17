import dayjs from 'dayjs'

/**
 * 정산일 기준 현재 청구 기간 계산
 *
 * - cutoffDay=15, today=3/16 → periodFrom=2/16, periodTo=3/15  (이번달 청구 기간)
 * - cutoffDay=15, today=3/10 → periodFrom=1/16, periodTo=2/15  (전월 청구 기간)
 *
 * 오늘이 정산일 이후이면 "당월 16 ~ 다음달 15" 기간이 진행 중,
 * 오늘이 정산일 이전이면 "전전월 16 ~ 전월 15" 기간이 청구 중.
 */
export function getCurrentBillingPeriod(
  cutoffDay: number,
  today: string,
): { periodFrom: string; periodTo: string } {
  const t = dayjs(today)
  const todayDay = t.date()

  // 이번 달의 정산일
  const cutoffThisMonth = t.date(cutoffDay)

  if (todayDay >= cutoffDay) {
    // 오늘이 정산일 이후 → 정산일+1 ~ 다음달 정산일
    const periodFrom = cutoffThisMonth.add(1, 'day').format('YYYY-MM-DD')
    const periodTo   = cutoffThisMonth.add(1, 'month').format('YYYY-MM-DD')
    return { periodFrom, periodTo }
  } else {
    // 오늘이 정산일 이전 → 전월 정산일+1 ~ 이번달 정산일
    const prevCutoff = cutoffThisMonth.subtract(1, 'month')
    const periodFrom = prevCutoff.add(1, 'day').format('YYYY-MM-DD')
    const periodTo   = cutoffThisMonth.format('YYYY-MM-DD')
    return { periodFrom, periodTo }
  }
}

/**
 * 결제일까지 남은 일수 레이블 계산
 * - "D-3", "D-day", "D+1" 형태로 반환
 */
export function calcDDay(dueDate: string, today: string): string {
  const diff = dayjs(dueDate).diff(dayjs(today), 'day')
  if (diff === 0) return 'D-day'
  if (diff > 0)   return `D-${diff}`
  return `D+${Math.abs(diff)}`
}
