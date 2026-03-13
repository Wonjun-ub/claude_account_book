namespace BudgetTracker.Api.DTOs.Responses;

public class RecurringTransactionResponse
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int PaymentMethodId { get; set; }
    public string PaymentMethodName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int DayOfMonth { get; set; }
    public int? TotalInstallments { get; set; }
    public int? RemainingInstallments { get; set; }
    public string? Memo { get; set; }
    public bool IsActive { get; set; }
}
