using BudgetTracker.Api.Data;
using BudgetTracker.Api.Models.Entities;
using BudgetTracker.Api.Models.Enums;
using BudgetTracker.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.Api.Repositories;

public class SummaryRepository : ISummaryRepository
{
    private readonly BudgetTrackerDbContext _db;

    public SummaryRepository(BudgetTrackerDbContext db)
    {
        _db = db;
    }

    // 기간 내 전체 거래 (건수 집계용)
    public async Task<IEnumerable<Transaction>> GetByPeriodAsync(DateTime periodStart, DateTime periodEnd)
    {
        return await _db.Transactions
            .Where(t => t.Date >= periodStart && t.Date <= periodEnd)
            .ToListAsync();
    }

    // 기간 내 합산 포함 거래 (수입/지출 합계, 월별 추이용)
    public async Task<IEnumerable<Transaction>> GetIncludedByPeriodAsync(DateTime periodStart, DateTime periodEnd)
    {
        return await _db.Transactions
            .Where(t => t.Date >= periodStart && t.Date <= periodEnd && t.IsIncludedInTotal)
            .ToListAsync();
    }

    // 기간 내 지출 합계 (전월 대비 계산용)
    public async Task<decimal> GetExpenseSumAsync(DateTime periodStart, DateTime periodEnd)
    {
        return await _db.Transactions
            .Where(t => t.Date >= periodStart && t.Date <= periodEnd
                     && t.Type == TransactionType.Expense
                     && t.IsIncludedInTotal)
            .SumAsync(t => t.Amount);
    }

    // 기간 내 카테고리별 거래 (카테고리 집계용, Category 탐색 속성 포함)
    public async Task<IEnumerable<Transaction>> GetIncludedWithCategoryByPeriodAsync(
        DateTime periodStart, DateTime periodEnd, TransactionType? typeFilter)
    {
        var query = _db.Transactions
            .Include(t => t.Category)
            .Where(t => t.Date >= periodStart && t.Date <= periodEnd && t.IsIncludedInTotal);

        if (typeFilter.HasValue)
            query = query.Where(t => t.Type == typeFilter.Value);

        return await query.ToListAsync();
    }
}
