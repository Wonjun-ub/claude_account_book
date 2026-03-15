namespace BudgetTracker.Api.DTOs.Responses;

public class MonthlySummaryResponse
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal Balance { get; set; }

    // 전월 대비 지출 변화 (양수 = 증가, 음수 = 감소)
    public decimal MonthOverMonthChange { get; set; }

    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    // 건수 (IsIncludedInTotal 포함 여부 무관)
    public int IncomeCount { get; set; }
    public int ExpenseCount { get; set; }
}
