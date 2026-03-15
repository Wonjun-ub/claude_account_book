using BudgetTracker.Api.DTOs.Requests;
using BudgetTracker.Api.DTOs.Responses;
using BudgetTracker.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BudgetTracker.Api.Controllers;

[ApiController]
[Route("api/installment-transactions")]
public class InstallmentTransactionsController : ControllerBase
{
    private readonly IInstallmentTransactionService _service;

    public InstallmentTransactionsController(IInstallmentTransactionService service)
    {
        _service = service;
    }

    // GET /api/installment-transactions
    [HttpGet]
    public async Task<ActionResult<IEnumerable<InstallmentTransactionResponse>>> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(result);
    }

    // POST /api/installment-transactions
    [HttpPost]
    public async Task<ActionResult<InstallmentTransactionResponse>> Create(
        [FromBody] CreateInstallmentTransactionRequest request)
    {
        var (response, error) = await _service.CreateAsync(request);

        if (error is not null)
            return BadRequest(new { message = error });

        return CreatedAtAction(nameof(GetAll), new { id = response!.Id }, response);
    }

    // DELETE /api/installment-transactions/{id}?mode=all|fromHere|single&seq=N
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] string mode = "all", [FromQuery] int? seq = null)
    {
        var error = await _service.DeleteAsync(id, mode, seq);

        return error switch
        {
            "NOT_FOUND" => NotFound(new { message = "할부 원부를 찾을 수 없습니다." }),
            "MISSING_SEQ" => BadRequest(new { message = "seq 파라미터가 필요합니다." }),
            "INVALID_MODE" => BadRequest(new { message = "유효하지 않은 mode입니다. (all|fromHere|single)" }),
            null => NoContent(),
            _ => StatusCode(500, new { message = error })
        };
    }
}
