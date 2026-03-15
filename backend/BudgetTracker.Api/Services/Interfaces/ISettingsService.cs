using BudgetTracker.Api.DTOs.Responses;

namespace BudgetTracker.Api.Services.Interfaces;

public interface ISettingsService
{
    // 설정 조회
    Task<UserSettingsResponse?> GetAsync();

    // 설정 수정
    Task<UserSettingsResponse?> UpdateAsync(int monthStartDay);
}
