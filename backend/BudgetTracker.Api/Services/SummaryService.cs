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

    // 월별 요약 (반복 지출 자동 반영 포함) — 단일 쿼리로 최적화
    public async Task<MonthlySummaryResponse> GetMonthlyAsync(int year, int month)
    {
        // 반복 지출 자동 반영 (on-demand)
        await _recurringService.ApplyRecurringTransactionsAsync(year, month);

        // 사용자 설정 및 기간 계산
        var settings = await _settingsRepo.GetAsync();
        int monthStartDay = settings?.MonthStartDay ?? 1;

        var (periodStart, periodEnd) = DateRangeHelper.GetMonthRange(year, month, monthStartDay);

        // 전체 거래를 한 번에 조회 (건수 + 합계 모두 처리)
        var allTx = await _summaryRepo.GetByPeriodAsync(periodStart, periodEnd);
        var allTxList = allTx.ToList();

        // IsIncludedInTotal 기준 필터링은 메모리에서 수행
        var includedTx = allTxList.Where(t => t.IsIncludedInTotal).ToList();
        decimal totalIncome = includedTx.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        decimal totalExpense = includedTx.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);

        // 전체 거래 건수 (IsIncludedInTotal 무관)
        int incomeCount = allTxList.Count(t => t.Type == TransactionType.Income);
        int expenseCount = allTxList.Count(t => t.Type == TransactionType.Expense);

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

    // 월별 추이 — 전체 기간을 단일 쿼리로 조회 후 메모리에서 그룹핑
    public async Task<IEnumerable<MonthlyTrendResponse>> GetTrendAsync(int months)
    {
        if (months < 1 || months > 24)
            months = 6;

        var settings = await _settingsRepo.GetAsync();
        int monthStartDay = settings?.MonthStartDay ?? 1;

        var now = DateTime.UtcNow;

        // 전체 기간의 시작/종료 계산
        var earliestDate = now.AddMonths(-(months - 1));
        var (earliestFrom, _) = DateRangeHelper.GetMonthRange(earliestDate.Year, earliestDate.Month, monthStartDay);
        var (_, latestTo) = DateRangeHelper.GetMonthRange(now.Year, now.Month, monthStartDay);

        // 전체 기간 거래를 한 번에 조회
        var allTransactions = (await _summaryRepo.GetIncludedByPeriodAsync(earliestFrom, latestTo)).ToList();

        // 메모리에서 각 월별로 필터링하여 집계
        var result = new List<MonthlyTrendResponse>();
        for (int i = months - 1; i >= 0; i--)
        {
            var targetDate = now.AddMonths(-i);
            int targetYear = targetDate.Year;
            int targetMonth = targetDate.Month;

            var (periodStart, periodEnd) = DateRangeHelper.GetMonthRange(targetYear, targetMonth, monthStartDay);
            var monthTx = allTransactions.Where(t => t.Date >= periodStart && t.Date <= periodEnd).ToList();

            result.Add(new MonthlyTrendResponse
            {
                Year = targetYear,
                Month = targetMonth,
                Income = monthTx.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
                Expense = monthTx.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount)
            });
        }

        return result;
    }
}
