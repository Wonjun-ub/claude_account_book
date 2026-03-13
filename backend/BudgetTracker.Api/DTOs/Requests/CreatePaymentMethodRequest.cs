using System.ComponentModel.DataAnnotations;
using BudgetTracker.Api.Models.Enums;

namespace BudgetTracker.Api.DTOs.Requests;

public class CreatePaymentMethodRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public PaymentMethodType Type { get; set; }

    // Type == Point일 때 필수
    public int? PointBudgetId { get; set; }
}
