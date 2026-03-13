namespace BudgetTracker.Api.DTOs.Responses;

public class PointBudgetResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal RemainingAmount { get; set; }
}
