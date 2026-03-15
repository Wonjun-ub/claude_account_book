using BudgetTracker.Api.Models.Entities;

namespace BudgetTracker.Api.Repositories.Interfaces;

public interface IInstallmentTransactionRepository
{
    Task<IEnumerable<InstallmentTransaction>> GetAllAsync();
    Task<InstallmentTransaction?> GetByIdAsync(int id);
    Task<InstallmentTransaction> CreateAsync(InstallmentTransaction installment);
    Task DeleteAsync(InstallmentTransaction installment);
    Task<bool> CategoryExistsAsync(int categoryId);
    Task<bool> PaymentMethodExistsAsync(int paymentMethodId);

    // 할부 원부에 속한 거래 조회 (회차 기준 필터 포함)
    Task<IEnumerable<Transaction>> GetTransactionsByMasterAsync(int masterId);
    Task<IEnumerable<Transaction>> GetTransactionsFromSeqAsync(int masterId, int fromSeq);
    Task<Transaction?> GetTransactionBySeqAsync(int masterId, int seq);
    Task DeleteTransactionsAsync(IEnumerable<Transaction> transactions);
    Task SaveChangesAsync();
}
