using BudgetTracker.Api.Models.Enums;

namespace BudgetTracker.Api.DTOs.Responses;

public class TransactionResponse
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string? Memo { get; set; }
    public string Type { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int PaymentMethodId { get; set; }
    public string PaymentMethodName { get; set; } = string.Empty;
    public bool IsIncludedInTotal { get; set; }
    public int? RecurringTransactionId { get; set; }
}
