using BudgetTracker.Api.DTOs.Responses;

namespace BudgetTracker.Api.Services.Interfaces;

public interface ISummaryService
{
    // 월별 요약
    Task<MonthlySummaryResponse> GetMonthlyAsync(int year, int month);

    // 카테고리별 집계
    Task<IEnumerable<CategorySummaryResponse>> GetByCategoryAsync(int year, int month, string? type);

    // 월별 추이
    Task<IEnumerable<MonthlyTrendResponse>> GetTrendAsync(int months);
}
