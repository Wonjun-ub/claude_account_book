using BudgetTracker.Api.Data;
using BudgetTracker.Api.DTOs.Requests;
using BudgetTracker.Api.DTOs.Responses;
using BudgetTracker.Api.Models.Entities;
using BudgetTracker.Api.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.Api.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly BudgetTrackerDbContext _db;

    public CategoriesController(BudgetTrackerDbContext db)
    {
        _db = db;
    }

    // GET /api/categories?type=Expense
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryResponse>>> GetAll([FromQuery] CategoryType? type)
    {
        var query = _db.Categories.AsQueryable();

        if (type.HasValue)
            query = query.Where(c => c.Type == type.Value);

        var categories = await query
            .OrderBy(c => c.Type)
            .ThenBy(c => c.Name)
            .Select(c => new CategoryResponse
            {
                Id = c.Id,
                Name = c.Name,
                Type = c.Type.ToString(),
                IsDefault = c.IsDefault
            })
            .ToListAsync();

        return Ok(categories);
    }

    // POST /api/categories
    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> Create([FromBody] CreateCategoryRequest request)
    {
        var category = new Category
        {
            Name = request.Name,
            Type = request.Type,
            IsDefault = false
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { id = category.Id }, new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Type = category.Type.ToString(),
            IsDefault = category.IsDefault
        });
    }

    // PUT /api/categories/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult<CategoryResponse>> Update(int id, [FromBody] UpdateCategoryRequest request)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category is null)
            return NotFound(new { message = "카테고리를 찾을 수 없습니다." });

        category.Name = request.Name;
        category.Type = request.Type;

        await _db.SaveChangesAsync();

        return Ok(new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Type = category.Type.ToString(),
            IsDefault = category.IsDefault
        });
    }

    // DELETE /api/categories/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category is null)
            return NotFound(new { message = "카테고리를 찾을 수 없습니다." });

        // 기본 카테고리는 삭제 불가
        if (category.IsDefault)
            return BadRequest(new { message = "기본 카테고리는 삭제할 수 없습니다." });

        // 연결된 거래가 있으면 삭제 불가
        bool hasTransactions = await _db.Transactions.AnyAsync(t => t.CategoryId == id);
        if (hasTransactions)
            return BadRequest(new { message = "이 카테고리에 연결된 거래가 있어 삭제할 수 없습니다." });

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
