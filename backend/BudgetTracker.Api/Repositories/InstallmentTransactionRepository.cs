using BudgetTracker.Api.Data;
using BudgetTracker.Api.Models.Entities;
using BudgetTracker.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.Api.Repositories;

public class InstallmentTransactionRepository : IInstallmentTransactionRepository
{
    private readonly BudgetTrackerDbContext _db;

    public InstallmentTransactionRepository(BudgetTrackerDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<InstallmentTransaction>> GetAllAsync()
    {
        return await _db.InstallmentTransactions
            .Include(i => i.Category)
            .Include(i => i.PaymentMethod)
            .OrderByDescending(i => i.IsActive)
            .ThenByDescending(i => i.StartDate)
            .ToListAsync();
    }

    public async Task<InstallmentTransaction?> GetByIdAsync(int id)
    {
        return await _db.InstallmentTransactions
            .Include(i => i.Category)
            .Include(i => i.PaymentMethod)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<InstallmentTransaction> CreateAsync(InstallmentTransaction installment)
    {
        _db.InstallmentTransactions.Add(installment);
        await _db.SaveChangesAsync();

        await _db.Entry(installment).Reference(i => i.Category).LoadAsync();
        await _db.Entry(installment).Reference(i => i.PaymentMethod).LoadAsync();

        return installment;
    }

    public async Task DeleteAsync(InstallmentTransaction installment)
    {
        _db.InstallmentTransactions.Remove(installment);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> CategoryExistsAsync(int categoryId)
    {
        return await _db.Categories.AnyAsync(c => c.Id == categoryId);
    }

    public async Task<bool> PaymentMethodExistsAsync(int paymentMethodId)
    {
        return await _db.PaymentMethods.AnyAsync(p => p.Id == paymentMethodId);
    }

    // 할부 원부에 속한 모든 거래 조회
    public async Task<IEnumerable<Transaction>> GetTransactionsByMasterAsync(int masterId)
    {
        return await _db.Transactions
            .Include(t => t.PaymentMethod)
                .ThenInclude(p => p.PointBudget)
            .Where(t => t.InstallmentTransactionId == masterId)
            .OrderBy(t => t.InstallmentSequence)
            .ToListAsync();
    }

    // N회차 이상 거래 조회
    public async Task<IEnumerable<Transaction>> GetTransactionsFromSeqAsync(int masterId, int fromSeq)
    {
        return await _db.Transactions
            .Include(t => t.PaymentMethod)
                .ThenInclude(p => p.PointBudget)
            .Where(t => t.InstallmentTransactionId == masterId && t.InstallmentSequence >= fromSeq)
            .OrderBy(t => t.InstallmentSequence)
            .ToListAsync();
    }

    // 특정 회차 거래 조회
    public async Task<Transaction?> GetTransactionBySeqAsync(int masterId, int seq)
    {
        return await _db.Transactions
            .Include(t => t.PaymentMethod)
                .ThenInclude(p => p.PointBudget)
            .FirstOrDefaultAsync(t => t.InstallmentTransactionId == masterId && t.InstallmentSequence == seq);
    }

    public async Task DeleteTransactionsAsync(IEnumerable<Transaction> transactions)
    {
        _db.Transactions.RemoveRange(transactions);
        await _db.SaveChangesAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}
