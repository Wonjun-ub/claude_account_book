using BudgetTracker.Api.DTOs.Requests;
using BudgetTracker.Api.DTOs.Responses;
using BudgetTracker.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BudgetTracker.Api.Controllers;

[ApiController]
[Route("api/transactions")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _service;

    public TransactionsController(ITransactionService service)
    {
        _service = service;
    }

    // GET /api/transactions — 목록 조회 + 검색/필터
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TransactionResponse>>> GetAll(
        [FromQuery] int? categoryId,
        [FromQuery] int? paymentMethodId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? keyword,
        [FromQuery] decimal? minAmount,
        [FromQuery] decimal? maxAmount,
        [FromQuery] int? year,
        [FromQuery] int? month)
    {
        var result = await _service.GetAllAsync(
            year, month, keyword, categoryId, paymentMethodId, from, to, minAmount, maxAmount);

        return Ok(result);
    }

    // GET /api/transactions/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<TransactionResponse>> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);

        if (result is null)
            return NotFound(new { message = "거래를 찾을 수 없습니다." });

        return Ok(result);
    }

    // POST /api/transactions
    [HttpPost]
    public async Task<ActionResult<TransactionResponse>> Create([FromBody] CreateTransactionRequest request)
    {
        var (response, error) = await _service.CreateAsync(request);

        if (error is not null)
            return BadRequest(new { message = error });

        return CreatedAtAction(nameof(GetById), new { id = response!.Id }, response);
    }

    // PUT /api/transactions/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult<TransactionResponse>> Update(int id, [FromBody] UpdateTransactionRequest request)
    {
        var (response, error) = await _service.UpdateAsync(id, request);

        if (error == "NOT_FOUND")
            return NotFound(new { message = "거래를 찾을 수 없습니다." });

        if (error is not null)
            return BadRequest(new { message = error });

        return Ok(response);
    }

    // DELETE /api/transactions/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var error = await _service.DeleteAsync(id);

        if (error == "NOT_FOUND")
            return NotFound(new { message = "거래를 찾을 수 없습니다." });

        return NoContent();
    }
}
