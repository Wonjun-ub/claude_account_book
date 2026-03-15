using BudgetTracker.Api.Data;
using BudgetTracker.Api.Models.Entities;
using BudgetTracker.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.Api.Repositories;

public class RecurringRepository : IRecurringRepository
{
    private readonly BudgetTrackerDbContext _db;

    public RecurringRepository(BudgetTrackerDbContext db)
    {
        _db = db;
    }

    // 모든 활성 반복 지출 조회 (포인트 예산 포함)
    public async Task<IEnumerable<RecurringTransaction>> GetAllActiveAsync()
    {
        return await _db.RecurringTransactions
            .Include(r => r.PaymentMethod)
                .ThenInclude(p => p.PointBudget)
            .Where(r => r.IsActive)
            .ToListAsync();
    }

    // 전체 목록 조회 (카테고리, 결제수단 포함)
    public async Task<IEnumerable<RecurringTransaction>> GetAllAsync()
    {
        return await _db.RecurringTransactions
            .Include(r => r.Category)
            .Include(r => r.PaymentMethod)
            .OrderByDescending(r => r.IsActive)
            .ThenBy(r => r.DayOfMonth)
            .ToListAsync();
    }

    // 단건 조회
    public async Task<RecurringTransaction?> GetByIdAsync(int id)
    {
        return await _db.RecurringTransactions.FindAsync(id);
    }

    // 생성
    public async Task<RecurringTransaction> CreateAsync(RecurringTransaction recurring)
    {
        _db.RecurringTransactions.Add(recurring);
        await _db.SaveChangesAsync();

        // 탐색 속성 로드
        await _db.Entry(recurring).Reference(r => r.Category).LoadAsync();
        await _db.Entry(recurring).Reference(r => r.PaymentMethod).LoadAsync();

        return recurring;
    }

    // 수정
    public async Task UpdateAsync(RecurringTransaction recurring)
    {
        await _db.SaveChangesAsync();
    }

    // 삭제 (연결된 거래는 SetNull이므로 그냥 삭제)
    public async Task DeleteAsync(RecurringTransaction recurring)
    {
        _db.RecurringTransactions.Remove(recurring);
        await _db.SaveChangesAsync();
    }

    // 카테고리 존재 여부 확인
    public async Task<bool> CategoryExistsAsync(int categoryId)
    {
        return await _db.Categories.AnyAsync(c => c.Id == categoryId);
    }

    // 결제수단 존재 여부 확인
    public async Task<bool> PaymentMethodExistsAsync(int paymentMethodId)
    {
        return await _db.PaymentMethods.AnyAsync(p => p.Id == paymentMethodId);
    }
}
