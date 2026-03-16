using BudgetTracker.Api.DTOs.Responses;
using BudgetTracker.Api.Helpers;
using BudgetTracker.Api.Models.Enums;
using BudgetTracker.Api.Repositories.Interfaces;
using BudgetTracker.Api.Services.Interfaces;

namespace BudgetTracker.Api.Services;

public class SummaryService : ISummaryService
{
    private readonly ISummaryRepository _summaryRepo;
    private readonly IRecurringService _recurringService;
    private readonly ISettingsRepository _settingsRepo;

    public SummaryService(
        ISummaryRepository summaryRepo,
        IRecurringService recurringService,
        ISettingsRepository settingsRepo)
    {
        _summaryRepo = summaryRepo;
        _recurringService = recurringService;
        _settingsRepo = settingsRepo;
    }

    // 월별 요약 (반복 지출 자동 반영 포함)
    public async Task<MonthlySummaryResponse> GetMonthlyAsync(int year, int month)
    {
        // 반복 지출 자동 반영 (on-demand)
        await _recurringService.ApplyRecurringTransactionsAsync(year, month);

        // 사용자 설정 및 기간 계산
        var settings = await _settingsRepo.GetAsync();
        int monthStartDay = settings?.MonthStartDay ?? 1;

        var (periodStart, periodEnd) = DateRangeHelper.GetMonthRange(year, month, monthStartDay);

        // 합산 포함 거래 (수입/지출 합계)
        var includedTx = await _summaryRepo.GetIncludedByPeriodAsync(periodStart, periodEnd);
        var incomeList = includedTx.Where(t => t.Type == TransactionType.Income).ToList();
        var expenseList = includedTx.Where(t => t.Type == TransactionType.Expense).ToList();
        decimal totalIncome = incomeList.Sum(t => t.Amount);
        decimal totalExpense = expenseList.Sum(t => t.Amount);

        // 전체 거래 건수 (IsIncludedInTotal 무관)
        var allTx = await _summaryRepo.GetByPeriodAsync(periodStart, periodEnd);
        int incomeCount = allTx.Count(t => t.Type == TransactionType.Income);
        int expenseCount = allTx.Count(t => t.Type == TransactionType.Expense);

        // 전월 대비 지출 변화 계산
        var prevMonth = month == 1 ? 12 : month - 1;
        var prevYear = month == 1 ? year - 1 : year;
        var (prevStart, prevEnd) = DateRangeHelper.GetMonthRange(prevYear, prevMonth, monthStartDay);
        decimal prevExpense = await _summaryRepo.GetExpenseSumAsync(prevStart, prevEnd);

        return new MonthlySummaryResponse
        {
            Year = year,
            Month = month,
            TotalIncome = totalIncome,
            TotalExpense = totalExpense,
            Balance = totalIncome - totalExpense,
            MonthOverMonthChange = totalExpense - prevExpense,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            IncomeCount = incomeCount,
            ExpenseCount = expenseCount,
        };
    }

    // 카테고리별 집계
    public async Task<IEnumerable<CategorySummaryResponse>> GetByCategoryAsync(int year, int month, string? type)
    {
        var settings = await _settingsRepo.GetAsync();
        int monthStartDay = settings?.MonthStartDay ?? 1;

        var (periodStart, periodEnd) = DateRangeHelper.GetMonthRange(year, month, monthStartDay);

        TransactionType? typeFilter = null;
        if (!string.IsNullOrEmpty(type) && Enum.TryParse<TransactionType>(type, true, out var txType))
            typeFilter = txType;

        var transactions = await _summaryRepo.GetIncludedWithCategoryByPeriodAsync(periodStart, periodEnd, typeFilter);

        var grouped = transactions
            .GroupBy(t => new { t.CategoryId, t.Category.Name, t.Category.Type })
            .Select(g => new
            {
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.Name,
                CategoryType = g.Key.Type.ToString(),
                Amount = g.Sum(t => t.Amount)
            })
            .ToList();

        decimal total = grouped.Sum(g => g.Amount);

        return grouped
            .OrderByDescending(g => g.Amount)
            .Select(g => new CategorySummaryResponse
            {
                CategoryId = g.CategoryId,
                CategoryName = g.CategoryName,
                CategoryType = g.CategoryType,
                Amount = g.Amount,
                Percentage = total > 0 ? Math.Round(g.Amount / total * 100, 1) : 0
            })
            .ToList();
    }

    // 월별 추이
    public async Task<IEnumerable<MonthlyTrendResponse>> GetTrendAsync(int months)
    {
        if (months < 1 || months > 24)
            months = 6;

        var settings = await _settingsRepo.GetAsync();
        int monthStartDay = settings?.MonthStartDay ?? 1;

        var now = DateTime.UtcNow;
        var result = new List<MonthlyTrendResponse>();

        for (int i = months - 1; i >= 0; i--)
        {
            var targetDate = now.AddMonths(-i);
            int targetYear = targetDate.Year;
            int targetMonth = targetDate.Month;

            var (periodStart, periodEnd) = DateRangeHelper.GetMonthRange(targetYear, targetMonth, monthStartDay);
            var transactions = await _summaryRepo.GetIncludedByPeriodAsync(periodStart, periodEnd);

            result.Add(new MonthlyTrendResponse
            {
                Year = targetYear,
                Month = targetMonth,
                Income = transactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
                Expense = transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount)
            });
        }

        return result;
    }
}
