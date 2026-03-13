namespace BudgetTracker.Api.Models.Entities;

public class UserSettings
{
    public int Id { get; set; }

    // 월 시작일 (기본값: 1일)
    public int MonthStartDay { get; set; } = 1;
}
