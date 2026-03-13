using BudgetTracker.Api.Data;
using BudgetTracker.Api.DTOs.Responses;
using BudgetTracker.Api.Helpers;
using BudgetTracker.Api.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.Api.Controllers;

[ApiController]
[Route("api/summary")]
public class SummaryController : ControllerBase
{
    private readonly BudgetTrackerDbContext _db;
    private readonly RecurringTransactionsController _recurringController;

    public SummaryController(BudgetTrackerDbContext db)
    {
        _db = db;
        _recurringController = new RecurringTransactionsController(db);
    }

    // GET /api/summary/monthly?year=2026&month=3
    [HttpGet("monthly")]
    public async Task<ActionResult<MonthlySummaryResponse>> GetMonthly(
        [FromQuery] int? year,
        [FromQuery] int? month)
    {
        var now = DateTime.UtcNow;
        int targetYear = year ?? now.Year;
        int targetMonth = month ?? now.Month;

        // 반복 지출 자동 반영 (on-demand)
        await _recurringController.ApplyRecurringTransactionsAsync(targetYear, targetMonth);

        // 사용자 설정 조회
        var settings = await _db.UserSettings.FirstOrDefaultAsync();
        int monthStartDay = settings?.MonthStartDay ?? 1;

        var (periodStart, periodEnd) = DateRangeHelper.GetMonthRange(targetYear, targetMonth, monthStartDay);

        // 해당 기간 거래 집계 (IsIncludedInTotal == true인 것만)
        var transactions = await _db.Transactions
            .Where(t => t.Date >= periodStart && t.Date <= periodEnd && t.IsIncludedInTotal)
            .ToListAsync();

        decimal totalIncome = transactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        decimal totalExpense = transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);

        // 전월 대비 지출 변화 계산
        var prevMonth = targetMonth == 1 ? 12 : targetMonth - 1;
        var prevYear = targetMonth == 1 ? targetYear - 1 : targetYear;
        var (prevStart, prevEnd) = DateRangeHelper.GetMonthRange(prevYear, prevMonth, monthStartDay);

        decimal prevExpense = await _db.Transactions
            .Where(t => t.Date >= prevStart && t.Date <= prevEnd
                     && t.Type == TransactionType.Expense
                     && t.IsIncludedInTotal)
            .SumAsync(t => t.Amount);

        return Ok(new MonthlySummaryResponse
        {
            Year = targetYear,
            Month = targetMonth,
            TotalIncome = totalIncome,
            TotalExpense = totalExpense,
            Balance = totalIncome - totalExpense,
            MonthOverMonthChange = totalExpense - prevExpense,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd
        });
    }

    // GET /api/summary/category?year=2026&month=3
    [HttpGet("category")]
    public async Task<ActionResult<IEnumerable<CategorySummaryResponse>>> GetByCategory(
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] string? type)
    {
        var now = DateTime.UtcNow;
        int targetYear = year ?? now.Year;
        int targetMonth = month ?? now.Month;

        var settings = await _db.UserSettings.FirstOrDefaultAsync();
        int monthStartDay = settings?.MonthStartDay ?? 1;

        var (periodStart, periodEnd) = DateRangeHelper.GetMonthRange(targetYear, targetMonth, monthStartDay);

        var query = _db.Transactions
            .Include(t => t.Category)
            .Where(t => t.Date >= periodStart && t.Date <= periodEnd && t.IsIncludedInTotal);

        // type 필터: Expense 또는 Income
        if (!string.IsNullOrEmpty(type) && Enum.TryParse<TransactionType>(type, true, out var txType))
            query = query.Where(t => t.Type == txType);

        var grouped = await query
            .GroupBy(t => new { t.CategoryId, t.Category.Name, t.Category.Type })
            .Select(g => new
            {
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.Name,
                CategoryType = g.Key.Type.ToString(),
                Amount = g.Sum(t => t.Amount)
            })
            .ToListAsync();

        decimal total = grouped.Sum(g => g.Amount);

        var result = grouped
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

        return Ok(result);
    }

    // GET /api/summary/trend?months=6
    [HttpGet("trend")]
    public async Task<ActionResult<IEnumerable<MonthlyTrendResponse>>> GetTrend([FromQuery] int months = 6)
    {
        if (months < 1 || months > 24)
            months = 6;

        var settings = await _db.UserSettings.FirstOrDefaultAsync();
        int monthStartDay = settings?.MonthStartDay ?? 1;

        var now = DateTime.UtcNow;
        var result = new List<MonthlyTrendResponse>();

        for (int i = months - 1; i >= 0; i--)
        {
            var targetDate = now.AddMonths(-i);
            int targetYear = targetDate.Year;
            int targetMonth = targetDate.Month;

            var (periodStart, periodEnd) = DateRangeHelper.GetMonthRange(targetYear, targetMonth, monthStartDay);

            var transactions = await _db.Transactions
                .Where(t => t.Date >= periodStart && t.Date <= periodEnd && t.IsIncludedInTotal)
                .ToListAsync();

            result.Add(new MonthlyTrendResponse
            {
                Year = targetYear,
                Month = targetMonth,
                Income = transactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
                Expense = transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount)
            });
        }

        return Ok(result);
    }
}
