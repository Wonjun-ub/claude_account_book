using System.ComponentModel.DataAnnotations;
using BudgetTracker.Api.Models.Enums;

namespace BudgetTracker.Api.DTOs.Requests;

public class CreateTransactionRequest
{
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "금액은 0보다 커야 합니다.")]
    public decimal Amount { get; set; }

    [Required]
    public DateTime Date { get; set; }

    public string? Memo { get; set; }

    [Required]
    public TransactionType Type { get; set; }

    [Required]
    public int CategoryId { get; set; }

    [Required]
    public int PaymentMethodId { get; set; }

    public bool IsIncludedInTotal { get; set; } = true;

    // 반복 거래 첫 번째 건 등록 시 원부 연결용 (nullable)
    public int? RecurringTransactionId { get; set; }
}
