namespace BudgetTracker.Api.Helpers;

public static class DateRangeHelper
{
    /// <summary>
    /// 커스텀 월 시작일 기준으로 해당 월의 시작/종료 날짜를 계산합니다.
    /// 예: MonthStartDay=25, year=2026, month=3 → 2026-03-25 ~ 2026-04-24
    /// </summary>
    public static (DateTime Start, DateTime End) GetMonthRange(int year, int month, int monthStartDay)
    {
        // 해당 월의 최대 일수를 고려하여 실제 시작일 결정
        int daysInStartMonth = DateTime.DaysInMonth(year, month);
        int actualStartDay = Math.Min(monthStartDay, daysInStartMonth);

        var start = new DateTime(year, month, actualStartDay, 0, 0, 0, DateTimeKind.Utc);

        // 다음 달 계산
        var nextMonth = start.AddMonths(1);
        int daysInNextMonth = DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month);
        int actualEndDay = Math.Min(monthStartDay - 1, daysInNextMonth);

        // monthStartDay=1인 경우 종료일은 해당 월 말일
        DateTime end;
        if (monthStartDay == 1)
        {
            int daysInMonth = DateTime.DaysInMonth(year, month);
            end = new DateTime(year, month, daysInMonth, 23, 59, 59, DateTimeKind.Utc);
        }
        else
        {
            // 다음 달의 (시작일 - 1)일까지
            var endMonth = month == 12 ? 1 : month + 1;
            var endYear = month == 12 ? year + 1 : year;
            int daysInEndMonth = DateTime.DaysInMonth(endYear, endMonth);
            int endDay = Math.Min(monthStartDay - 1, daysInEndMonth);
            end = new DateTime(endYear, endMonth, endDay, 23, 59, 59, DateTimeKind.Utc);
        }

        return (start, end);
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
