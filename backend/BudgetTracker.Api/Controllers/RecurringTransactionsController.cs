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

    // POST /api/recurring-transactions
    [HttpPost]
    public async Task<ActionResult<RecurringTransactionResponse>> Create([FromBody] CreateRecurringTransactionRequest request)
    {
        var (response, error) = await _service.CreateAsync(request);

        if (error is not null)
            return BadRequest(new { message = error });

        return CreatedAtAction(nameof(GetAll), new { id = response!.Id }, response);
    }

    // DELETE /api/recurring-transactions/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var error = await _service.DeleteAsync(id);

        if (error == "NOT_FOUND")
            return NotFound(new { message = "반복 지출을 찾을 수 없습니다." });

        return NoContent();
    }
}
