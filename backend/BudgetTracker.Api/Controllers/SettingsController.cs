using BudgetTracker.Api.Data;
using BudgetTracker.Api.DTOs.Requests;
using BudgetTracker.Api.DTOs.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.Api.Controllers;

[ApiController]
[Route("api/settings")]
public class SettingsController : ControllerBase
{
    private readonly BudgetTrackerDbContext _db;

    public SettingsController(BudgetTrackerDbContext db)
    {
        _db = db;
    }

    // GET /api/settings
    [HttpGet]
    public async Task<ActionResult<UserSettingsResponse>> Get()
    {
        var settings = await _db.UserSettings.FirstOrDefaultAsync();
        if (settings is null)
            return NotFound(new { message = "사용자 설정이 없습니다." });

        return Ok(new UserSettingsResponse
        {
            Id = settings.Id,
            MonthStartDay = settings.MonthStartDay
        });
    }

    // PUT /api/settings
    [HttpPut]
    public async Task<ActionResult<UserSettingsResponse>> Update([FromBody] UpdateUserSettingsRequest request)
    {
        // 설정 존재 확인
        var settings = await _db.UserSettings.FirstOrDefaultAsync();
        if (settings is null)
            return NotFound(new { message = "사용자 설정이 없습니다." });

        // 비즈니스 로직: 필드 업데이트
        settings.MonthStartDay = request.MonthStartDay;

        // DB 저장
        await _db.SaveChangesAsync();

        return Ok(new UserSettingsResponse
        {
            Id = settings.Id,
            MonthStartDay = settings.MonthStartDay
        });
    }
}
