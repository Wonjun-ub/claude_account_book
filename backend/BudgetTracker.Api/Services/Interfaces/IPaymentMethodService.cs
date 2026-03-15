using BudgetTracker.Api.DTOs.Requests;
using BudgetTracker.Api.DTOs.Responses;
using BudgetTracker.Api.Models.Enums;

namespace BudgetTracker.Api.Services.Interfaces;

public interface IPaymentMethodService
{
    // 목록 조회 (타입 필터 옵션)
    Task<IEnumerable<PaymentMethodResponse>> GetAllAsync(PaymentMethodType? type);

    // 생성 (Point 타입 시 pointBudgetId 필수 체크)
    Task<(PaymentMethodResponse? Response, string? ErrorMessage)> CreateAsync(CreatePaymentMethodRequest request);

    // 수정
    Task<(PaymentMethodResponse? Response, string? ErrorMessage)> UpdateAsync(int id, UpdatePaymentMethodRequest request);

    // 삭제 (isDefault 체크, 연결 거래 체크)
    Task<string?> DeleteAsync(int id);
}
