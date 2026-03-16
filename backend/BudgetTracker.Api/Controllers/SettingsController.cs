using BudgetTracker.Api.DTOs.Requests;
using BudgetTracker.Api.DTOs.Responses;
using BudgetTracker.Api.Helpers;
using BudgetTracker.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BudgetTracker.Api.Controllers;

[ApiController]
[Route("api/settings")]
public class SettingsController : ControllerBase
{
    private readonly ISettingsService _service;

    public SettingsController(ISettingsService service)
    {
        _service = service;
    }

    // GET /api/settings
    [HttpGet]
    public async Task<ActionResult<UserSettingsResponse>> Get()
    {
        var result = await _service.GetAsync();

        if (result is null)
            return NotFound(new { code = "NOT_FOUND", message = ErrorMessages.GetMessage("NOT_FOUND") });

        return Ok(result);
    }

    // PUT /api/settings
    [HttpPut]
    public async Task<ActionResult<UserSettingsResponse>> Update([FromBody] UpdateUserSettingsRequest request)
    {
        var result = await _service.UpdateAsync(request.MonthStartDay);

        if (result is null)
            return NotFound(new { code = "NOT_FOUND", message = ErrorMessages.GetMessage("NOT_FOUND") });

        return Ok(result);
    }
}
