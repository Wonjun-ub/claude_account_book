namespace BudgetTracker.Api.Models.Entities;

// 반복 거래 특정 월 스킵 (단건 삭제)
public class RecurringSkip
{
    public int Id { get; set; }
    public int RecurringTransactionId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }

    // 탐색 속성
    public RecurringTransaction RecurringTransaction { get; set; } = null!;
}
