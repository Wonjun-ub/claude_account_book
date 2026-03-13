using System.ComponentModel.DataAnnotations;

namespace BudgetTracker.Api.DTOs.Requests;

public class CreatePointBudgetRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "총액은 0보다 커야 합니다.")]
    public decimal TotalAmount { get; set; }
}
