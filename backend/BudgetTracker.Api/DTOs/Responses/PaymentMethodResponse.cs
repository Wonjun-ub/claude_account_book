namespace BudgetTracker.Api.DTOs.Responses;

public class PaymentMethodResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public int? PointBudgetId { get; set; }
    public PointBudgetResponse? PointBudget { get; set; }
}
