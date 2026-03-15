import dayjs from 'dayjs'

/**
 * 커스텀 월 시작일(startDay)을 기준으로 해당 year/month의 실제 날짜 범위를 계산합니다.
 *
 * 예) startDay=25, month=3(3월) → start=2/25, end=3/24
 *     startDay=1  , month=3(3월) → start=3/01, end=3/31
 */
export function getMonthPeriod(year: number, month: number, startDay: number) {
  if (startDay === 1) {
    const s = dayjs(`${year}-${String(month).padStart(2, '0')}-01`)
    return { start: s.format('YYYY-MM-DD'), end: s.endOf('month').format('YYYY-MM-DD') }
  }
  const start = dayjs(`${year}-${String(month).padStart(2, '0')}-01`).subtract(1, 'month').date(startDay)
  const end   = dayjs(`${year}-${String(month).padStart(2, '0')}-01`).date(startDay - 1)
  return { start: start.format('YYYY-MM-DD'), end: end.format('YYYY-MM-DD') }
}
