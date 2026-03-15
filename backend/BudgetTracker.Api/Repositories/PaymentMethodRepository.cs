using BudgetTracker.Api.Data;
using BudgetTracker.Api.Models.Entities;
using BudgetTracker.Api.Models.Enums;
using BudgetTracker.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.Api.Repositories;

public class PaymentMethodRepository : IPaymentMethodRepository
{
    private readonly BudgetTrackerDbContext _db;

    public PaymentMethodRepository(BudgetTrackerDbContext db)
    {
        _db = db;
    }

    // 전체 목록 조회 (포인트 예산 포함, 타입 필터 옵션)
    public async Task<IEnumerable<PaymentMethod>> GetAllAsync(PaymentMethodType? type)
    {
        var query = _db.PaymentMethods
            .Include(p => p.PointBudget)
            .AsQueryable();

        if (type.HasValue)
            query = query.Where(p => p.Type == type.Value);

        return await query
            .OrderBy(p => p.Type)
            .ThenBy(p => p.Name)
            .ToListAsync();
    }

    // 단건 조회 (포인트 예산 포함)
    public async Task<PaymentMethod?> GetByIdAsync(int id)
    {
        return await _db.PaymentMethods
            .Include(p => p.PointBudget)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    // 생성
    public async Task<PaymentMethod> CreateAsync(PaymentMethod paymentMethod)
    {
        _db.PaymentMethods.Add(paymentMethod);
        await _db.SaveChangesAsync();

        // 포인트 예산 정보 로드
        await _db.Entry(paymentMethod).Reference(m => m.PointBudget).LoadAsync();

        return paymentMethod;
    }

    // 수정
    public async Task UpdateAsync(PaymentMethod paymentMethod)
    {
        await _db.SaveChangesAsync();

        // 포인트 예산 정보 재로드
        await _db.Entry(paymentMethod).Reference(m => m.PointBudget).LoadAsync();
    }

    // 삭제
    public async Task DeleteAsync(PaymentMethod paymentMethod)
    {
        _db.PaymentMethods.Remove(paymentMethod);
        await _db.SaveChangesAsync();
    }

    // 연결된 거래 존재 여부 확인
    public async Task<bool> HasTransactionsAsync(int id)
    {
        return await _db.Transactions.AnyAsync(t => t.PaymentMethodId == id);
    }

    // 포인트 예산이 이미 다른 결제수단에 연결되어 있는지 확인
    public async Task<bool> IsPointBudgetLinkedAsync(int pointBudgetId)
    {
        return await _db.PaymentMethods.AnyAsync(p => p.PointBudgetId == pointBudgetId);
    }

    // 포인트 예산 존재 여부 확인
    public async Task<bool> PointBudgetExistsAsync(int pointBudgetId)
    {
        return await _db.PointBudgets.AnyAsync(p => p.Id == pointBudgetId);
    }
}
