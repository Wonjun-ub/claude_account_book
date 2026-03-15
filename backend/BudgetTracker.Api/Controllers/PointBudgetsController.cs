using BudgetTracker.Api.Data;
using BudgetTracker.Api.DTOs.Requests;
using BudgetTracker.Api.DTOs.Responses;
using BudgetTracker.Api.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.Api.Controllers;

[ApiController]
[Route("api/point-budgets")]
public class PointBudgetsController : ControllerBase
{
    private readonly BudgetTrackerDbContext _db;

    public PointBudgetsController(BudgetTrackerDbContext db)
    {
        _db = db;
    }

    // GET /api/point-budgets
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PointBudgetResponse>>> GetAll()
    {
        var budgets = await _db.PointBudgets
            .OrderBy(p => p.Name)
            .Select(p => new PointBudgetResponse
            {
                Id = p.Id,
                Name = p.Name,
                TotalAmount = p.TotalAmount,
                RemainingAmount = p.RemainingAmount
            })
            .ToListAsync();

        return Ok(budgets);
    }

    // GET /api/point-budgets/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<PointBudgetResponse>> GetById(int id)
    {
        var budget = await _db.PointBudgets.FindAsync(id);
        if (budget is null)
            return NotFound(new { message = "포인트 예산을 찾을 수 없습니다." });

        return Ok(new PointBudgetResponse
        {
            Id = budget.Id,
            Name = budget.Name,
            TotalAmount = budget.TotalAmount,
            RemainingAmount = budget.RemainingAmount
        });
    }

    // POST /api/point-budgets
    [HttpPost]
    public async Task<ActionResult<PointBudgetResponse>> Create([FromBody] CreatePointBudgetRequest request)
    {
        // 엔티티 생성 (RemainingAmount = TotalAmount로 초기화)
        var budget = new PointBudget
        {
            Name = request.Name,
            TotalAmount = request.TotalAmount,
            RemainingAmount = request.TotalAmount
        };

        // DB 저장
        _db.PointBudgets.Add(budget);
        await _db.SaveChangesAsync();

        // 응답 반환
        var response = new PointBudgetResponse
        {
            Id = budget.Id,
            Name = budget.Name,
            TotalAmount = budget.TotalAmount,
            RemainingAmount = budget.RemainingAmount
        };

        return CreatedAtAction(nameof(GetById), new { id = budget.Id }, response);
    }
}
