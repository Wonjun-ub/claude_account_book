using System.ComponentModel.DataAnnotations;
using BudgetTracker.Api.Models.Enums;

namespace BudgetTracker.Api.DTOs.Requests;

public class UpdatePaymentMethodRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public PaymentMethodType Type { get; set; }

    public int? PointBudgetId { get; set; }
}
