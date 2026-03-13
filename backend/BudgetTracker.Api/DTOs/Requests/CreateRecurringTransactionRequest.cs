using System.ComponentModel.DataAnnotations;
using BudgetTracker.Api.Models.Enums;

namespace BudgetTracker.Api.DTOs.Requests;

public class CreateRecurringTransactionRequest
{
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "금액은 0보다 커야 합니다.")]
    public decimal Amount { get; set; }

    [Required]
    public int CategoryId { get; set; }

    [Required]
    public int PaymentMethodId { get; set; }

    [Required]
    public RecurringType Type { get; set; }

    [Required]
    [Range(1, 31, ErrorMessage = "일자는 1~31 사이여야 합니다.")]
    public int DayOfMonth { get; set; }

    // Installment 타입일 때 필수
    public int? TotalInstallments { get; set; }

    public string? Memo { get; set; }
}
