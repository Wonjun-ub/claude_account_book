using System.ComponentModel.DataAnnotations;

namespace BudgetTracker.Api.DTOs.Requests;

public class CreateInstallmentTransactionRequest
{
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "금액은 0보다 커야 합니다.")]
    public decimal TotalAmount { get; set; }

    [Required]
    [Range(2, 60, ErrorMessage = "할부 개월수는 2~60 사이여야 합니다.")]
    public int TotalInstallments { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public int CategoryId { get; set; }

    [Required]
    public int PaymentMethodId { get; set; }

    public string? Memo { get; set; }

    public bool IsIncludedInTotal { get; set; } = true;
}
