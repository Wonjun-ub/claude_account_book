using BudgetTracker.Api.DTOs.Requests;
using BudgetTracker.Api.DTOs.Responses;
using BudgetTracker.Api.Models.Entities;
using BudgetTracker.Api.Models.Enums;
using BudgetTracker.Api.Repositories.Interfaces;
using BudgetTracker.Api.Services.Interfaces;

namespace BudgetTracker.Api.Services;

public class PaymentMethodService : IPaymentMethodService
{
    private readonly IPaymentMethodRepository _paymentMethodRepo;

    public PaymentMethodService(IPaymentMethodRepository paymentMethodRepo)
    {
        _paymentMethodRepo = paymentMethodRepo;
    }

    // 목록 조회 (타입 필터 옵션)
    public async Task<IEnumerable<PaymentMethodResponse>> GetAllAsync(PaymentMethodType? type)
    {
        var methods = await _paymentMethodRepo.GetAllAsync(type);

        return methods.Select(MapToResponse);
    }

    // 생성 (Point 타입 시 pointBudgetId 필수 체크)
    public async Task<(PaymentMethodResponse? Response, string? ErrorMessage)> CreateAsync(CreatePaymentMethodRequest request)
    {
        // Point 타입이면 PointBudgetId 필수
        if (request.Type == PaymentMethodType.Point && request.PointBudgetId is null)
            return (null, "포인트 결제수단 생성 시 PointBudgetId가 필요합니다.");

        // PointBudgetId 유효성 확인
        if (request.PointBudgetId.HasValue)
        {
            bool budgetExists = await _paymentMethodRepo.PointBudgetExistsAsync(request.PointBudgetId.Value);
            if (!budgetExists)
                return (null, "존재하지 않는 포인트 예산입니다.");

            bool alreadyLinked = await _paymentMethodRepo.IsPointBudgetLinkedAsync(request.PointBudgetId.Value);
            if (alreadyLinked)
                return (null, "이미 다른 결제수단에 연결된 포인트 예산입니다.");
        }

        // 엔티티 생성
        var method = new PaymentMethod
        {
            Name = request.Name,
            Type = request.Type,
            IsDefault = false,
            PointBudgetId = request.Type == PaymentMethodType.Point ? request.PointBudgetId : null
        };

        // DB 저장 (포인트 예산 로드 포함)
        var created = await _paymentMethodRepo.CreateAsync(method);

        return (MapToResponse(created), null);
    }

    // 수정
    public async Task<(PaymentMethodResponse? Response, string? ErrorMessage)> UpdateAsync(int id, UpdatePaymentMethodRequest request)
    {
        // 결제수단 존재 확인
        var method = await _paymentMethodRepo.GetByIdAsync(id);
        if (method is null)
            return (null, "NOT_FOUND");

        // 필드 업데이트
        method.Name = request.Name;
        method.Type = request.Type;
        method.PointBudgetId = request.Type == PaymentMethodType.Point ? request.PointBudgetId : null;

        // DB 저장 (포인트 예산 재로드 포함)
        await _paymentMethodRepo.UpdateAsync(method);

        return (MapToResponse(method), null);
    }

    // 삭제 (isDefault 체크, 연결 거래 체크)
    public async Task<string?> DeleteAsync(int id)
    {
        // 결제수단 존재 확인
        var method = await _paymentMethodRepo.GetByIdAsync(id);
        if (method is null)
            return "NOT_FOUND";

        // 기본 결제수단 삭제 불가
        if (method.IsDefault)
            return "기본 결제수단은 삭제할 수 없습니다.";

        // 연결된 거래가 있으면 삭제 불가
        bool hasTransactions = await _paymentMethodRepo.HasTransactionsAsync(id);
        if (hasTransactions)
            return "이 결제수단에 연결된 거래가 있어 삭제할 수 없습니다.";

        // DB 삭제
        await _paymentMethodRepo.DeleteAsync(method);

        return null;
    }

    // 응답 DTO 매핑
    private static PaymentMethodResponse MapToResponse(PaymentMethod p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Type = p.Type.ToString(),
        IsDefault = p.IsDefault,
        PointBudgetId = p.PointBudgetId,
        PointBudget = p.PointBudget == null ? null : new PointBudgetResponse
        {
            Id = p.PointBudget.Id,
            Name = p.PointBudget.Name,
            TotalAmount = p.PointBudget.TotalAmount,
            RemainingAmount = p.PointBudget.RemainingAmount
        }
    };
}
