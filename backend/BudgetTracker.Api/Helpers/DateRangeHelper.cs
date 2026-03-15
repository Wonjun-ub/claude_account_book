namespace BudgetTracker.Api.Helpers;

public static class DateRangeHelper
{
    /// <summary>
    /// 커스텀 월 시작일 기준으로 해당 월의 시작/종료 날짜를 계산합니다.
    /// 예: MonthStartDay=25, year=2026, month=3 → 2026-02-25 ~ 2026-03-24
    ///     (3월로 표시되는 청구 주기는 전월 25일 ~ 당월 24일)
    /// </summary>
    public static (DateTime Start, DateTime End) GetMonthRange(int year, int month, int monthStartDay)
    {
        // startDay=1이면 일반 달력 월과 동일
        if (monthStartDay == 1)
        {
            var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = new DateTime(year, month, DateTime.DaysInMonth(year, month), 23, 59, 59, DateTimeKind.Utc);

            return (start, end);
        }

        // 시작: 전월의 startDay
        // 예) month=3, startDay=25 → 2월 25일
        var prevMonth = new DateTime(year, month, 1).AddMonths(-1);
        int daysInPrevMonth = DateTime.DaysInMonth(prevMonth.Year, prevMonth.Month);
        int actualStartDay = Math.Min(monthStartDay, daysInPrevMonth);
        var rangeStart = new DateTime(prevMonth.Year, prevMonth.Month, actualStartDay, 0, 0, 0, DateTimeKind.Utc);

        // 종료: 당월의 (startDay - 1)일
        // 예) month=3, startDay=25 → 3월 24일
        int daysInMonth = DateTime.DaysInMonth(year, month);
        int endDay = Math.Min(monthStartDay - 1, daysInMonth);
        var rangeEnd = new DateTime(year, month, endDay, 23, 59, 59, DateTimeKind.Utc);

        return (rangeStart, rangeEnd);
    }

    /// <summary>
    /// 특정 반복 지출의 이번 달 실제 거래 날짜를 계산합니다.
    /// </summary>
    public static DateTime GetTransactionDate(int year, int month, int dayOfMonth)
    {
        int daysInMonth = DateTime.DaysInMonth(year, month);
        int actualDay = Math.Min(dayOfMonth, daysInMonth);

        return new DateTime(year, month, actualDay, 0, 0, 0, DateTimeKind.Utc);
    }
}
