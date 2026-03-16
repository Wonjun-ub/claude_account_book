using BudgetTracker.Api.Data;
using BudgetTracker.Api.Models.Entities;
using BudgetTracker.Api.Models.Enums;
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

    // 원부 저장 + N건 거래 생성을 하나의 DB 트랜잭션으로 원자적 처리
    // transactionDrafts: Type·InstallmentTransactionId 미설정 상태로 전달
    public async Task<InstallmentTransaction> BatchCreateAsync(
        InstallmentTransaction master,
        IEnumerable<Transaction> transactionDrafts)
    {
        // 카테고리 타입 조회 (거래 유형 결정용)
        var category = await _db.Categories.FindAsync(master.CategoryId)
            ?? throw new InvalidOperationException($"카테고리를 찾을 수 없습니다. Id={master.CategoryId}");
        var txType = category.Type == CategoryType.Income
            ? TransactionType.Income
            : TransactionType.Expense;

        await using var dbTx = await _db.Database.BeginTransactionAsync();
        try
        {
            // 1단계: 원부 저장 → DB에서 master.Id 확보
            _db.InstallmentTransactions.Add(master);
            await _db.SaveChangesAsync();

            // 2단계: 회차별 거래 일괄 추가 (원부 Id·거래 유형 연결)
            foreach (var draft in transactionDrafts)
            {
                draft.InstallmentTransactionId = master.Id;
                draft.Type = txType;
                _db.Transactions.Add(draft);
            }

            await _db.SaveChangesAsync();
            await dbTx.CommitAsync();

            // 탐색 속성 로드
            await _db.Entry(master).Reference(i => i.Category).LoadAsync();
            await _db.Entry(master).Reference(i => i.PaymentMethod).LoadAsync();

            return master;
        }
        catch
        {
            await dbTx.RollbackAsync();
            throw;
        }
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
