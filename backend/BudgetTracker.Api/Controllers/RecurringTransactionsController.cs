using BudgetTracker.Api.DTOs.Requests;
using BudgetTracker.Api.DTOs.Responses;
using BudgetTracker.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BudgetTracker.Api.Controllers;

[ApiController]
[Route("api/recurring-transactions")]
public class RecurringTransactionsController : ControllerBase
{
    private readonly IRecurringService _service;

    public RecurringTransactionsController(IRecurringService service)
    {
        _service = service;
    }

    // GET /api/recurring-transactions
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RecurringTransactionResponse>>> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(result);
    }

    // GET /api/recurring-transactions/pending?year=Y&month=M
    [HttpGet("pending")]
    public async Task<ActionResult<IEnumerable<RecurringTransactionResponse>>> GetPending(
        [FromQuery] int year, [FromQuery] int month)
    {
        if (year <= 0 || month < 1 || month > 12)
            return BadRequest(new { message = "유효하지 않은 year/month 파라미터입니다." });

        var result = await _service.GetPendingAsync(year, month);
        return Ok(result);
    }

    // POST /api/recurring-transactions
    [HttpPost]
    public async Task<ActionResult<RecurringTransactionResponse>> Create(
        [FromBody] CreateRecurringTransactionRequest request)
    {
        var (response, error) = await _service.CreateAsync(request);

        if (error is not null)
            return BadRequest(new { message = error });

        return CreatedAtAction(nameof(GetAll), new { id = response!.Id }, response);
    }

    // DELETE /api/recurring-transactions/{id}?mode=all|fromHere|skipMonth&date=...&year=...&month=...
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(
        int id,
        [FromQuery] string mode = "all",
        [FromQuery] DateTime? date = null,
        [FromQuery] int? year = null,
        [FromQuery] int? month = null)
    {
        var error = await _service.DeleteAsync(id, mode, date, year, month);

        return error switch
        {
            "NOT_FOUND" => NotFound(new { message = "반복 지출을 찾을 수 없습니다." }),
            "MISSING_DATE" => BadRequest(new { message = "date 파라미터가 필요합니다." }),
            "MISSING_YEAR_MONTH" => BadRequest(new { message = "year, month 파라미터가 필요합니다." }),
            "INVALID_MODE" => BadRequest(new { message = "유효하지 않은 mode입니다. (all|fromHere|skipMonth)" }),
            null => NoContent(),
            _ => StatusCode(500, new { message = error })
        };
    }
}
