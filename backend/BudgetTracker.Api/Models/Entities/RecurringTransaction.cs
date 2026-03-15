using BudgetTracker.Api.Models.Enums;

namespace BudgetTracker.Api.Models.Entities;

public class RecurringTransaction
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public int CategoryId { get; set; }
    public int PaymentMethodId { get; set; }
    public RecurringType Type { get; set; }
    public int DayOfMonth { get; set; }
    public string? Memo { get; set; }

    // 할부 완료 또는 수동 비활성화 시 false
    public bool IsActive { get; set; } = true;

    // 반복 시작일 (null이면 생성일 기준)
    public DateTime? StartDate { get; set; }

    // 반복 종료일 (null이면 무기한)
    public DateTime? EndDate { get; set; }

    // 탐색 속성
    public Category Category { get; set; } = null!;
    public PaymentMethod PaymentMethod { get; set; } = null!;
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<RecurringSkip> RecurringSkips { get; set; } = new List<RecurringSkip>();
}
