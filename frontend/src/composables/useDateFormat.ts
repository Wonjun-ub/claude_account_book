import dayjs from 'dayjs'

export function useDateFormat() {
  // MM/DD (ddd) 포맷 — HomeView 거래 목록 날짜
  function formatDate(dateStr: string): string {
    return dayjs(dateStr).format('MM/DD (ddd)')
  }

  // YYYY년 MM월 포맷 — DeleteOptionSheet 이후 삭제 설명
  function formatYearMonth(dateStr: string): string {
    return dayjs(dateStr).format('YYYY년 MM월')
  }

  return { formatDate, formatYearMonth }
}
