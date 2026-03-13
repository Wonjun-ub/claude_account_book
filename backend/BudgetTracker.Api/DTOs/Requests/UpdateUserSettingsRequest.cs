using System.ComponentModel.DataAnnotations;

namespace BudgetTracker.Api.DTOs.Requests;

public class UpdateUserSettingsRequest
{
    [Required]
    [Range(1, 31, ErrorMessage = "월 시작일은 1~31 사이여야 합니다.")]
    public int MonthStartDay { get; set; }
}
