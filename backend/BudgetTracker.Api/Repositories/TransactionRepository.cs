using BudgetTracker.Api.Data;
using BudgetTracker.Api.Models.Entities;
using BudgetTracker.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.Api.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly BudgetTrackerDbContext _db;

    public TransactionRepository(BudgetTrackerDbContext db)
    {
        _db = db;
    }

    // 목록 조회 + 검색/필터
    public async Task<IEnumerable<Transaction>> GetAllAsync(
        int? year,
        int? month,
        string? keyword,
        int? categoryId,
        int? paymentMethodId,
        DateTime? from,
        DateTime? to,
        decimal? minAmount,
        decimal? maxAmount)
    {
        var query = _db.Transactions
            .Include(t => t.Category)
            .Include(t => t.PaymentMethod)
            .Include(t => t.InstallmentTransaction)
            .AsQueryable();

        // 날짜 범위 기본값: 현재 월 (파라미터 없을 때)
        if (from.HasValue)
        {
            query = query.Where(t => t.Date >= from.Value);
        }
        else if (!to.HasValue && !year.HasValue)
        {
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            query = query.Where(t => t.Date >= startOfMonth);
        }

        if (to.HasValue)
            query = query.Where(t => t.Date <= to.Value);

        // year/month 파라미터 (달력 기반 필터)
        if (year.HasValue && month.HasValue)
        {
            var startOfMonth = new DateTime(year.Value, month.Value, 1, 0, 0, 0, DateTimeKind.Utc);
            var endOfMonth = startOfMonth.AddMonths(1).AddTicks(-1);
            query = query.Where(t => t.Date >= startOfMonth && t.Date <= endOfMonth);
        }

        // 카테고리 필터
        if (categoryId.HasValue)
            query = query.Where(t => t.CategoryId == categoryId.Value);

        // 결제수단 필터
        if (paymentMethodId.HasValue)
            query = query.Where(t => t.PaymentMethodId == paymentMethodId.Value);

        // 금액 범위 필터
        if (minAmount.HasValue)
            query = query.Where(t => t.Amount >= minAmount.Value);
        if (maxAmount.HasValue)
            query = query.Where(t => t.Amount <= maxAmount.Value);

        // 키워드 검색 (메모, 대소문자 무시 — PostgreSQL ILike)
        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(t => t.Memo != null && EF.Functions.ILike(t.Memo, $"%{keyword}%"));

        return await query
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Id)
            .ToListAsync();
    }

    // 단건 조회 (카테고리, 결제수단, 할부 원부 포함)
    public async Task<Transaction?> GetByIdAsync(int id)
    {
        return await _db.Transactions
            .Include(t => t.Category)
            .Include(t => t.PaymentMethod)
                .ThenInclude(p => p.PointBudget)
            .Include(t => t.InstallmentTransaction)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    // 포인트 복구를 위한 단건 조회 (결제수단 → 포인트 예산 포함)
    public async Task<Transaction> CreateAsync(Transaction transaction)
    {
        _db.Transactions.Add(transaction);
        await _db.SaveChangesAsync();

        // 탐색 속성 로드
        await _db.Entry(transaction).Reference(t => t.Category).LoadAsync();
        await _db.Entry(transaction).Reference(t => t.PaymentMethod).LoadAsync();

        return transaction;
    }

    // 수정 (변경 내용은 호출자가 트래킹된 엔티티에 적용 후 호출)
    public async Task UpdateAsync(Transaction transaction)
    {
        await _db.SaveChangesAsync();

        // 최신 탐색 속성 재로드
        await _db.Entry(transaction).Reference(t => t.Category).LoadAsync();
        await _db.Entry(transaction).Reference(t => t.PaymentMethod).LoadAsync();
        if (transaction.InstallmentTransactionId.HasValue)
            await _db.Entry(transaction).Reference(t => t.InstallmentTransaction).LoadAsync();
    }

    // 삭제
    public async Task DeleteAsync(Transaction transaction)
    {
        _db.Transactions.Remove(transaction);
        await _db.SaveChangesAsync();
    }

    // 반복 지출 중복 체크: 해당 월에 이미 생성된 거래가 있는지 확인
    public async Task<bool> GetByRecurringAndDateAsync(int recurringId, DateTime date)
    {
        return await _db.Transactions.AnyAsync(t =>
            t.RecurringTransactionId == recurringId &&
            t.Date.Year == date.Year &&
            t.Date.Month == date.Month);
    }
}
