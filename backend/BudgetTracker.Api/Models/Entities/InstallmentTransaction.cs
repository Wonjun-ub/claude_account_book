namespace BudgetTracker.Api.Models.Entities;

public class InstallmentTransaction
{
    public int Id { get; set; }

    // 총 금액
    public decimal TotalAmount { get; set; }

    // floor(총금액 / 개월수)
    public decimal MonthlyAmount { get; set; }

    // MonthlyAmount + (총금액 mod 개월수) — 1회차에 적용
    public decimal FirstMonthAmount { get; set; }

    // 총 할부 개월수
    public int TotalInstallments { get; set; }

    // 첫 할부일
    public DateTime StartDate { get; set; }

    public int CategoryId { get; set; }
    public int PaymentMethodId { get; set; }
    public string? Memo { get; set; }

    // 모든 회차 삭제 시 false
    public bool IsActive { get; set; } = true;

    // 탐색 속성
    public Category Category { get; set; } = null!;
    public PaymentMethod PaymentMethod { get; set; } = null!;
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
