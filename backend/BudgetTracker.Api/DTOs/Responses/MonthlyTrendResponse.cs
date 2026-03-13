namespace BudgetTracker.Api.DTOs.Responses;

public class MonthlyTrendResponse
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
}
