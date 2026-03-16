using BudgetTracker.Api.Data;
using BudgetTracker.Api.Models.Entities;
using BudgetTracker.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BudgetTracker.Api.Repositories;

public class RecurringRepository : IRecurringRepository
{
    private readonly BudgetTrackerDbContext _db;

    public RecurringRepository(BudgetTrackerDbContext db)
    {
        _db = db;
    }

    // 모든 활성 반복 지출 조회 (카테고리, 결제수단, 포인트 예산, 스킵 포함)
    public async Task<IEnumerable<RecurringTransaction>> GetAllActiveAsync()
    {
        return await _db.RecurringTransactions
            .Include(r => r.Category)
            .Include(r => r.PaymentMethod)
                .ThenInclude(p => p.PointBudget)
            .Include(r => r.RecurringSkips)
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

    // 해당 날짜 이후 거래 조회 + 삭제 (포인트 복구를 위해 거래 목록 반환)
    public async Task<IEnumerable<Transaction>> DeleteTransactionsFromDateAsync(int recurringId, DateTime fromDate)
    {
        var txs = await _db.Transactions
            .Include(t => t.PaymentMethod)
                .ThenInclude(p => p.PointBudget)
            .Where(t => t.RecurringTransactionId == recurringId && t.Date >= fromDate)
            .ToListAsync();
        _db.Transactions.RemoveRange(txs);
        await _db.SaveChangesAsync();
        return txs;
    }

    // 모든 거래 조회 + 삭제 (포인트 복구를 위해 거래 목록 반환)
    public async Task<IEnumerable<Transaction>> DeleteAllTransactionsAsync(int recurringId)
    {
        var txs = await _db.Transactions
            .Include(t => t.PaymentMethod)
                .ThenInclude(p => p.PointBudget)
            .Where(t => t.RecurringTransactionId == recurringId)
            .ToListAsync();
        _db.Transactions.RemoveRange(txs);
        await _db.SaveChangesAsync();
        return txs;
    }

    // 스킵 등록
    public async Task AddSkipAsync(RecurringSkip skip)
    {
        _db.RecurringSkips.Add(skip);
        await _db.SaveChangesAsync();
    }

    // 스킵 존재 여부
    public async Task<bool> IsSkippedAsync(int recurringId, int year, int month)
    {
        return await _db.RecurringSkips.AnyAsync(s =>
            s.RecurringTransactionId == recurringId &&
            s.Year == year &&
            s.Month == month);
    }

    // 기간 내 거래 조회 + 삭제 (포인트 복구를 위해 거래 목록 반환)
    public async Task<IEnumerable<Transaction>> DeleteTransactionsInPeriodAsync(int recurringId, DateTime periodStart, DateTime periodEnd)
    {
        var txs = await _db.Transactions
            .Include(t => t.PaymentMethod)
                .ThenInclude(p => p.PointBudget)
            .Where(t => t.RecurringTransactionId == recurringId
                     && t.Date >= periodStart
                     && t.Date <= periodEnd)
            .ToListAsync();
        _db.Transactions.RemoveRange(txs);
        await _db.SaveChangesAsync();
        return txs;
    }

    // DB 트랜잭션 시작 (ApplyRecurringTransactionsAsync 원자적 처리용)
    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return await _db.Database.BeginTransactionAsync();
    }

    // 변경사항 저장
    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }

    // 특정 기간에 해당 반복 거래 존재 여부
    public async Task<bool> HasTransactionInPeriodAsync(int recurringId, DateTime periodStart, DateTime periodEnd)
    {
        return await _db.Transactions.AnyAsync(t =>
            t.RecurringTransactionId == recurringId &&
            t.Date >= periodStart &&
            t.Date <= periodEnd);
    }
}
