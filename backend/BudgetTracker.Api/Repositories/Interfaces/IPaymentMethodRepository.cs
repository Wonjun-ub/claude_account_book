using BudgetTracker.Api.Models.Entities;
using BudgetTracker.Api.Models.Enums;

namespace BudgetTracker.Api.Repositories.Interfaces;

public interface IPaymentMethodRepository
{
    // 목록 조회 (타입 필터 옵션)
    Task<IEnumerable<PaymentMethod>> GetAllAsync(PaymentMethodType? type);

    // 단건 조회 (PointBudget 포함)
    Task<PaymentMethod?> GetByIdAsync(int id);

    // 생성
    Task<PaymentMethod> CreateAsync(PaymentMethod paymentMethod);

    // 수정
    Task UpdateAsync(PaymentMethod paymentMethod);

    // 삭제
    Task DeleteAsync(PaymentMethod paymentMethod);

    // 연결된 거래 존재 여부 확인
    Task<bool> HasTransactionsAsync(int id);

    // 포인트 예산이 이미 다른 결제수단에 연결되어 있는지 확인
    Task<bool> IsPointBudgetLinkedAsync(int pointBudgetId);

    // 포인트 예산 존재 여부 확인
    Task<bool> PointBudgetExistsAsync(int pointBudgetId);
}
