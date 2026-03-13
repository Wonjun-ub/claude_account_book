using BudgetTracker.Api.Models.Enums;

namespace BudgetTracker.Api.Models.Entities;

public class PaymentMethod
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public PaymentMethodType Type { get; set; }
    public bool IsDefault { get; set; }

    // 포인트 타입일 때만 연결되는 FK (nullable)
    public int? PointBudgetId { get; set; }
    public PointBudget? PointBudget { get; set; }

    // 탐색 속성
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<RecurringTransaction> RecurringTransactions { get; set; } = new List<RecurringTransaction>();
}
