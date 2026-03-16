using BudgetTracker.Api.Models.Entities;
using BudgetTracker.Api.Models.Enums;

namespace BudgetTracker.Api.Repositories.Interfaces;

public interface ISummaryRepository
{
    // 기간 내 전체 거래 (건수 집계용)
    Task<IEnumerable<Transaction>> GetByPeriodAsync(DateTime periodStart, DateTime periodEnd);

    // 기간 내 합산 포함 거래 (수입/지출 합계, 월별 추이용)
    Task<IEnumerable<Transaction>> GetIncludedByPeriodAsync(DateTime periodStart, DateTime periodEnd);

    // 기간 내 지출 합계 (전월 대비 계산용)
    Task<decimal> GetExpenseSumAsync(DateTime periodStart, DateTime periodEnd);

    // 기간 내 카테고리별 거래 (카테고리 집계용, Category 탐색 속성 포함)
    Task<IEnumerable<Transaction>> GetIncludedWithCategoryByPeriodAsync(DateTime periodStart, DateTime periodEnd, TransactionType? typeFilter);
}
