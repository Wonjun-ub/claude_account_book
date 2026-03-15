using BudgetTracker.Api.Data;
using BudgetTracker.Api.Models.Entities;
using BudgetTracker.Api.Models.Enums;
using BudgetTracker.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.Api.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly BudgetTrackerDbContext _db;

    public CategoryRepository(BudgetTrackerDbContext db)
    {
        _db = db;
    }

    // 전체 목록 조회 (타입 필터 옵션)
    public async Task<IEnumerable<Category>> GetAllAsync(CategoryType? type)
    {
        var query = _db.Categories.AsQueryable();

        if (type.HasValue)
            query = query.Where(c => c.Type == type.Value);

        return await query
            .OrderBy(c => c.Type)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    // 단건 조회
    public async Task<Category?> GetByIdAsync(int id)
    {
        return await _db.Categories.FindAsync(id);
    }

    // 생성
    public async Task<Category> CreateAsync(Category category)
    {
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return category;
    }

    // 수정
    public async Task UpdateAsync(Category category)
    {
        await _db.SaveChangesAsync();
    }

    // 삭제
    public async Task DeleteAsync(Category category)
    {
        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
    }

    // 연결된 거래 존재 여부 확인
    public async Task<bool> HasTransactionsAsync(int id)
    {
        return await _db.Transactions.AnyAsync(t => t.CategoryId == id);
    }
}
