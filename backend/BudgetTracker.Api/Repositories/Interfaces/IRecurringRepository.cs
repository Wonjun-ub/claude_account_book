using BudgetTracker.Api.Models.Entities;

namespace BudgetTracker.Api.Repositories.Interfaces;

public interface IRecurringRepository
{
    // 모든 활성 반복 지출 조회 (포인트 예산 포함)
    Task<IEnumerable<RecurringTransaction>> GetAllActiveAsync();

    // 전체 목록 조회 (카테고리, 결제수단 포함)
    Task<IEnumerable<RecurringTransaction>> GetAllAsync();

    // 단건 조회
    Task<RecurringTransaction?> GetByIdAsync(int id);

    // 생성
    Task<RecurringTransaction> CreateAsync(RecurringTransaction recurring);

    // 수정
    Task UpdateAsync(RecurringTransaction recurring);

    // 삭제
    Task DeleteAsync(RecurringTransaction recurring);

    // 카테고리 존재 여부 확인
    Task<bool> CategoryExistsAsync(int categoryId);

    // 결제수단 존재 여부 확인
    Task<bool> PaymentMethodExistsAsync(int paymentMethodId);
}
