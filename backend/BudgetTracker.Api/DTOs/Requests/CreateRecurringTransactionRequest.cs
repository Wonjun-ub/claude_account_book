using System.ComponentModel.DataAnnotations;

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
    [Range(1, 31, ErrorMessage = "일자는 1~31 사이여야 합니다.")]
    public int DayOfMonth { get; set; }

    public string? Memo { get; set; }

    // 반복 시작일 (null이면 즉시 시작)
    public DateTime? StartDate { get; set; }

    // 반복 종료일 (null이면 무기한)
    public DateTime? EndDate { get; set; }
}
