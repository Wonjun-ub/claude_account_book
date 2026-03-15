using BudgetTracker.Api.Data;
using BudgetTracker.Api.Models.Entities;
using BudgetTracker.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.Api.Repositories;

public class PointBudgetRepository : IPointBudgetRepository
{
    private readonly BudgetTrackerDbContext _db;

    public PointBudgetRepository(BudgetTrackerDbContext db)
    {
        _db = db;
    }

    // 전체 목록 조회
    public async Task<IEnumerable<PointBudget>> GetAllAsync()
    {
        return await _db.PointBudgets
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    // 단건 조회
    public async Task<PointBudget?> GetByIdAsync(int id)
    {
        return await _db.PointBudgets.FindAsync(id);
    }

    // 생성
    public async Task<PointBudget> CreateAsync(PointBudget pointBudget)
    {
        _db.PointBudgets.Add(pointBudget);
        await _db.SaveChangesAsync();

        return pointBudget;
    }

    // 수정
    public async Task UpdateAsync(PointBudget pointBudget)
    {
        await _db.SaveChangesAsync();
    }
}
