using BudgetTracker.Api.DTOs.Responses;
using BudgetTracker.Api.Repositories.Interfaces;
using BudgetTracker.Api.Services.Interfaces;

namespace BudgetTracker.Api.Services;

public class SettingsService : ISettingsService
{
    private readonly ISettingsRepository _settingsRepo;

    public SettingsService(ISettingsRepository settingsRepo)
    {
        _settingsRepo = settingsRepo;
    }

    // 설정 조회
    public async Task<UserSettingsResponse?> GetAsync()
    {
        var settings = await _settingsRepo.GetAsync();

        if (settings is null)
            return null;

        return new UserSettingsResponse
        {
            Id = settings.Id,
            MonthStartDay = settings.MonthStartDay
        };
    }

    // 설정 수정
    public async Task<UserSettingsResponse?> UpdateAsync(int monthStartDay)
    {
        // 설정 존재 확인
        var settings = await _settingsRepo.GetAsync();
        if (settings is null)
            return null;

        // 비즈니스 로직: 필드 업데이트
        settings.MonthStartDay = monthStartDay;

        // DB 저장
        await _settingsRepo.UpdateAsync(settings);

        return new UserSettingsResponse
        {
            Id = settings.Id,
            MonthStartDay = settings.MonthStartDay
        };
    }
}
