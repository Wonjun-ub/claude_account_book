namespace BudgetTracker.Api.DTOs.Responses;

public class InstallmentTransactionResponse
{
    public int Id { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal MonthlyAmount { get; set; }
    public decimal FirstMonthAmount { get; set; }
    public int TotalInstallments { get; set; }
    public DateTime StartDate { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int PaymentMethodId { get; set; }
    public string PaymentMethodName { get; set; } = string.Empty;
    public string? Memo { get; set; }
    public bool IsActive { get; set; }
}
