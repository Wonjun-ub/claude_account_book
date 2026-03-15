using BudgetTracker.Api.Data;
using BudgetTracker.Api.Models.Entities;
using BudgetTracker.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.Api.Repositories;

public class SettingsRepository : ISettingsRepository
{
    private readonly BudgetTrackerDbContext _db;

    public SettingsRepository(BudgetTrackerDbContext db)
    {
        _db = db;
    }

    // 설정 조회 (레코드가 하나뿐임)
    public async Task<UserSettings?> GetAsync()
    {
        return await _db.UserSettings.FirstOrDefaultAsync();
    }

    // 설정 수정
    public async Task UpdateAsync(UserSettings settings)
    {
        await _db.SaveChangesAsync();
    }
}
