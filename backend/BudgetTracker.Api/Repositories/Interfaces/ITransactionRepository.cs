using BudgetTracker.Api.Models.Entities;

namespace BudgetTracker.Api.Repositories.Interfaces;

public interface ITransactionRepository
{
    // 목록 조회 + 검색/필터
    Task<IEnumerable<Transaction>> GetAllAsync(
        int? year,
        int? month,
        string? keyword,
        int? categoryId,
        int? paymentMethodId,
        DateTime? from,
        DateTime? to,
        decimal? minAmount,
        decimal? maxAmount);

    // 단건 조회
    Task<Transaction?> GetByIdAsync(int id);

    // 생성
    Task<Transaction> CreateAsync(Transaction transaction);

    // 수정
    Task UpdateAsync(Transaction transaction);

    // 삭제
    Task DeleteAsync(Transaction transaction);

    // 반복 지출 중복 체크용
    Task<bool> GetByRecurringAndDateAsync(int recurringId, DateTime date);
}
