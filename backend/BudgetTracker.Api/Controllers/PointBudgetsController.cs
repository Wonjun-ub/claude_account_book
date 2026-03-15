using BudgetTracker.Api.DTOs.Requests;
using BudgetTracker.Api.DTOs.Responses;
using BudgetTracker.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BudgetTracker.Api.Controllers;

[ApiController]
[Route("api/point-budgets")]
public class PointBudgetsController : ControllerBase
{
    private readonly IPointBudgetService _service;

    public PointBudgetsController(IPointBudgetService service)
    {
        _service = service;
    }

    // GET /api/point-budgets
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PointBudgetResponse>>> GetAll()
    {
        var result = await _service.GetAllAsync();

        return Ok(result);
    }

    // GET /api/point-budgets/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<PointBudgetResponse>> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);

        if (result is null)
            return NotFound(new { message = "포인트 예산을 찾을 수 없습니다." });

        return Ok(result);
    }

    // POST /api/point-budgets
    [HttpPost]
    public async Task<ActionResult<PointBudgetResponse>> Create([FromBody] CreatePointBudgetRequest request)
    {
        var result = await _service.CreateAsync(request);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
}
