using BudgetTracker.Api.Models.Enums;

namespace BudgetTracker.Api.Models.Entities;

public class Transaction
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string? Memo { get; set; }
    public TransactionType Type { get; set; }
    public int CategoryId { get; set; }
    public int PaymentMethodId { get; set; }
    public bool IsIncludedInTotal { get; set; } = true;

    // 반복 지출로 자동 생성된 거래인 경우 연결 (nullable)
    public int? RecurringTransactionId { get; set; }

    // 탐색 속성
    public Category Category { get; set; } = null!;
    public PaymentMethod PaymentMethod { get; set; } = null!;
    public RecurringTransaction? RecurringTransaction { get; set; }
}
