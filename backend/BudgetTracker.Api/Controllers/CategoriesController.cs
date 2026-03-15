using BudgetTracker.Api.DTOs.Requests;
using BudgetTracker.Api.DTOs.Responses;
using BudgetTracker.Api.Models.Enums;
using BudgetTracker.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BudgetTracker.Api.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _service;

    public CategoriesController(ICategoryService service)
    {
        _service = service;
    }

    // GET /api/categories?type=Expense
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryResponse>>> GetAll([FromQuery] CategoryType? type)
    {
        var result = await _service.GetAllAsync(type);

        return Ok(result);
    }

    // POST /api/categories
    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> Create([FromBody] CreateCategoryRequest request)
    {
        var result = await _service.CreateAsync(request);

        return CreatedAtAction(nameof(GetAll), new { id = result.Id }, result);
    }

    // PUT /api/categories/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult<CategoryResponse>> Update(int id, [FromBody] UpdateCategoryRequest request)
    {
        var (response, error) = await _service.UpdateAsync(id, request);

        if (error == "NOT_FOUND")
            return NotFound(new { message = "카테고리를 찾을 수 없습니다." });

        if (error is not null)
            return BadRequest(new { message = error });

        return Ok(response);
    }

    // DELETE /api/categories/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var error = await _service.DeleteAsync(id);

        if (error == "NOT_FOUND")
            return NotFound(new { message = "카테고리를 찾을 수 없습니다." });

        if (error is not null)
            return BadRequest(new { message = error });

        return NoContent();
    }
}
