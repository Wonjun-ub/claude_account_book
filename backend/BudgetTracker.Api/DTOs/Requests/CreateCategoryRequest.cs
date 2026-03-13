using System.ComponentModel.DataAnnotations;
using BudgetTracker.Api.Models.Enums;

namespace BudgetTracker.Api.DTOs.Requests;

public class CreateCategoryRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public CategoryType Type { get; set; }
}
