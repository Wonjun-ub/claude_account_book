using BudgetTracker.Api.Helpers;
using Xunit;

namespace BudgetTracker.Tests.Helpers;

public class DateRangeHelperTests
{
    // ── GetMonthRange ─────────────────────────────────────────────────────────

    [Fact]
    public void GetMonthRange_StartDay1_ReturnsCalendarMonth()
    {
        var (start, end) = DateRangeHelper.GetMonthRange(2026, 1, 1);

        Assert.Equal(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), start);
        Assert.Equal(new DateTime(2026, 1, 31, 23, 59, 59, DateTimeKind.Utc), end);
    }

    [Fact]
    public void GetMonthRange_StartDay1_February_ReturnsCorrectDays()
    {
        // 2026년 2월 — 윤년 아님 (28일)
        var (start, end) = DateRangeHelper.GetMonthRange(2026, 2, 1);

        Assert.Equal(new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), start);
        Assert.Equal(new DateTime(2026, 2, 28, 23, 59, 59, DateTimeKind.Utc), end);
    }

    [Fact]
    public void GetMonthRange_StartDay1_LeapYearFebruary_Returns29Days()
    {
        // 2024년 2월 — 윤년 (29일)
        var (start, end) = DateRangeHelper.GetMonthRange(2024, 2, 1);

        Assert.Equal(new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc), start);
        Assert.Equal(new DateTime(2024, 2, 29, 23, 59, 59, DateTimeKind.Utc), end);
    }

    [Fact]
    public void GetMonthRange_StartDay25_March_ReturnsFeb25ToMar24()
    {
        // monthStartDay=25, 3월 → 전월(2월) 25일 ~ 당월(3월) 24일
        var (start, end) = DateRangeHelper.GetMonthRange(2026, 3, 25);

        Assert.Equal(new DateTime(2026, 2, 25, 0, 0, 0, DateTimeKind.Utc), start);
        Assert.Equal(new DateTime(2026, 3, 24, 23, 59, 59, DateTimeKind.Utc), end);
    }

    [Fact]
    public void GetMonthRange_StartDay25_January_ReturnsPrevYearDec25ToJan24()
    {
        // monthStartDay=25, 1월 → 전년도(12월) 25일 ~ 당월(1월) 24일
        var (start, end) = DateRangeHelper.GetMonthRange(2026, 1, 25);

        Assert.Equal(new DateTime(2025, 12, 25, 0, 0, 0, DateTimeKind.Utc), start);
        Assert.Equal(new DateTime(2026, 1, 24, 23, 59, 59, DateTimeKind.Utc), end);
    }

    [Fact]
    public void GetMonthRange_StartDay31_PrevMonthHas28Days_ClampsStartDay()
    {
        // monthStartDay=31, 3월 → 전월(2월)은 28일이므로 시작일=28일로 클램프
        // end = Math.Min(30, 31) = 30 → 3월 30일
        var (start, end) = DateRangeHelper.GetMonthRange(2026, 3, 31);

        Assert.Equal(new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc), start);
        Assert.Equal(new DateTime(2026, 3, 30, 23, 59, 59, DateTimeKind.Utc), end);
    }

    [Fact]
    public void GetMonthRange_StartDay10_June_ReturnsMay10ToJun9()
    {
        var (start, end) = DateRangeHelper.GetMonthRange(2026, 6, 10);

        Assert.Equal(new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc), start);
        Assert.Equal(new DateTime(2026, 6, 9, 23, 59, 59, DateTimeKind.Utc), end);
    }

    // ── GetTransactionDate ────────────────────────────────────────────────────

    [Fact]
    public void GetTransactionDate_DayWithinMonth_ReturnsExactDate()
    {
        var result = DateRangeHelper.GetTransactionDate(2026, 3, 15);

        Assert.Equal(new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public void GetTransactionDate_DayExceedsFebruary_ClampsToLastDay()
    {
        // 2월은 28일이므로 day=31은 28로 클램프
        var result = DateRangeHelper.GetTransactionDate(2026, 2, 31);

        Assert.Equal(new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public void GetTransactionDate_Day29_LeapYearFebruary_Returns29()
    {
        // 윤년(2024) 2월은 29일까지 있음
        var result = DateRangeHelper.GetTransactionDate(2024, 2, 29);

        Assert.Equal(new DateTime(2024, 2, 29, 0, 0, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public void GetTransactionDate_Day31_January_Returns31()
    {
        var result = DateRangeHelper.GetTransactionDate(2026, 1, 31);

        Assert.Equal(new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc), result);
    }
}
