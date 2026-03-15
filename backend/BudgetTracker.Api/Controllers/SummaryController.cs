using BudgetTracker.Api.DTOs.Responses;
using BudgetTracker.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BudgetTracker.Api.Controllers;

[ApiController]
[Route("api/summary")]
public class SummaryController : ControllerBase
{
    private readonly ISummaryService _service;

    public SummaryController(ISummaryService service)
    {
        _service = service;
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

        var result = await _service.GetMonthlyAsync(targetYear, targetMonth);

        return Ok(result);
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

        var result = await _service.GetByCategoryAsync(targetYear, targetMonth, type);

        return Ok(result);
    }

    // GET /api/summary/trend?months=6
    [HttpGet("trend")]
    public async Task<ActionResult<IEnumerable<MonthlyTrendResponse>>> GetTrend([FromQuery] int months = 6)
    {
        var result = await _service.GetTrendAsync(months);

        return Ok(result);
    }
}
