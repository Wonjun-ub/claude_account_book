using BudgetTracker.Api.Models.Entities;

namespace BudgetTracker.Api.Repositories.Interfaces;

public interface ISettingsRepository
{
    // 설정 조회 (레코드가 하나뿐임)
    Task<UserSettings?> GetAsync();

    // 설정 수정
    Task UpdateAsync(UserSettings settings);
}
