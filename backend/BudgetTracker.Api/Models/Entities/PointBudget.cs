namespace BudgetTracker.Api.Models.Entities;

public class PointBudget
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal RemainingAmount { get; set; }

    // 탐색 속성
    public PaymentMethod? PaymentMethod { get; set; }
}
