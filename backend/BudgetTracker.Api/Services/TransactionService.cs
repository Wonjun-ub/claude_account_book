using BudgetTracker.Api.DTOs.Requests;
using BudgetTracker.Api.DTOs.Responses;
using BudgetTracker.Api.Helpers;
using BudgetTracker.Api.Models.Entities;
using BudgetTracker.Api.Models.Enums;
using BudgetTracker.Api.Repositories.Interfaces;
using BudgetTracker.Api.Services.Interfaces;

namespace BudgetTracker.Api.Services;

public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _transactionRepo;
    private readonly IPaymentMethodRepository _paymentMethodRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly ISettingsRepository _settingsRepo;

    public TransactionService(
        ITransactionRepository transactionRepo,
        IPaymentMethodRepository paymentMethodRepo,
        ICategoryRepository categoryRepo,
        ISettingsRepository settingsRepo)
    {
        _transactionRepo = transactionRepo;
        _paymentMethodRepo = paymentMethodRepo;
        _categoryRepo = categoryRepo;
        _settingsRepo = settingsRepo;
    }

    // 목록 조회 + 검색/필터
    public async Task<IEnumerable<TransactionResponse>> GetAllAsync(
        int? year,
        int? month,
        string? keyword,
        int? categoryId,
        int? paymentMethodId,
        DateTime? from,
        DateTime? to,
        decimal? minAmount,
        decimal? maxAmount)
    {
        // year/month 지정 시 monthStartDay 기준으로 from/to 변환
        // (from/to 직접 지정한 경우는 기존 동작 유지)
        if (year.HasValue && month.HasValue && !from.HasValue && !to.HasValue)
        {
            var settings = await _settingsRepo.GetAsync();
            var monthStartDay = settings?.MonthStartDay ?? 1;
            (from, to) = DateRangeHelper.GetMonthRange(year.Value, month.Value, monthStartDay);
            year = null;
            month = null;
        }

        var transactions = await _transactionRepo.GetAllAsync(
            year, month, keyword, categoryId, paymentMethodId, from, to, minAmount, maxAmount);

        return transactions.Select(MapToResponse);
    }

    // 단건 조회
    public async Task<TransactionResponse?> GetByIdAsync(int id)
    {
        var transaction = await _transactionRepo.GetByIdAsync(id);

        if (transaction is null)
            return null;

        return MapToResponse(transaction);
    }

    // 생성 (포인트 차감 로직 포함)
    public async Task<(TransactionResponse? Response, string? ErrorMessage)> CreateAsync(CreateTransactionRequest request)
    {
        // 카테고리 존재 확인
        var category = await _categoryRepo.GetByIdAsync(request.CategoryId);
        if (category is null)
            return (null, "존재하지 않는 카테고리입니다.");

        // 결제수단 존재 확인 + 포인트 결제수단인 경우 잔액 확인
        var paymentMethod = await _paymentMethodRepo.GetByIdAsync(request.PaymentMethodId);
        if (paymentMethod is null)
            return (null, "존재하지 않는 결제수단입니다.");

        // 포인트 차감 처리
        if (paymentMethod.Type == PaymentMethodType.Point && paymentMethod.PointBudget is not null)
        {
            if (paymentMethod.PointBudget.RemainingAmount < request.Amount)
                return (null, $"포인트 잔액이 부족합니다. (잔액: {paymentMethod.PointBudget.RemainingAmount:N0}원)");

            paymentMethod.PointBudget.RemainingAmount -= request.Amount;
        }

        // 엔티티 생성
        var transaction = new Transaction
        {
            Amount = request.Amount,
            Date = request.Date.ToUniversalTime(),
            Memo = request.Memo,
            Type = request.Type,
            CategoryId = request.CategoryId,
            PaymentMethodId = request.PaymentMethodId,
            IsIncludedInTotal = request.IsIncludedInTotal
        };

        // DB 저장 (탐색 속성 로드 포함)
        var created = await _transactionRepo.CreateAsync(transaction);

        return (MapToResponse(created), null);
    }

    // 수정 (이전 포인트 복구 후 새 포인트 차감)
    public async Task<(TransactionResponse? Response, string? ErrorMessage)> UpdateAsync(int id, UpdateTransactionRequest request)
    {
        // 거래 존재 확인
        var transaction = await _transactionRepo.GetByIdAsync(id);
        if (transaction is null)
            return (null, "NOT_FOUND");

        // 카테고리 존재 확인
        var category = await _categoryRepo.GetByIdAsync(request.CategoryId);
        if (category is null)
            return (null, "존재하지 않는 카테고리입니다.");

        // 새 결제수단 로드
        var newPaymentMethod = await _paymentMethodRepo.GetByIdAsync(request.PaymentMethodId);
        if (newPaymentMethod is null)
            return (null, "존재하지 않는 결제수단입니다.");

        // 이전 포인트 복구
        var oldPaymentMethod = transaction.PaymentMethod;
        if (oldPaymentMethod.Type == PaymentMethodType.Point && oldPaymentMethod.PointBudget is not null)
        {
            oldPaymentMethod.PointBudget.RemainingAmount += transaction.Amount;
        }

        // 새 포인트 차감
        if (newPaymentMethod.Type == PaymentMethodType.Point && newPaymentMethod.PointBudget is not null)
        {
            if (newPaymentMethod.PointBudget.RemainingAmount < request.Amount)
                return (null, $"포인트 잔액이 부족합니다. (잔액: {newPaymentMethod.PointBudget.RemainingAmount:N0}원)");

            newPaymentMethod.PointBudget.RemainingAmount -= request.Amount;
        }

        // 엔티티 필드 업데이트
        transaction.Amount = request.Amount;
        transaction.Date = request.Date.ToUniversalTime();
        transaction.Memo = request.Memo;
        transaction.Type = request.Type;
        transaction.CategoryId = request.CategoryId;
        transaction.PaymentMethodId = request.PaymentMethodId;
        transaction.IsIncludedInTotal = request.IsIncludedInTotal;

        // DB 저장 (탐색 속성 재로드 포함)
        await _transactionRepo.UpdateAsync(transaction);

        return (MapToResponse(transaction), null);
    }

    // 삭제 (포인트 복구 후 삭제)
    public async Task<string?> DeleteAsync(int id)
    {
        // 거래 존재 확인 (결제수단 → 포인트 예산 포함)
        var transaction = await _transactionRepo.GetByIdAsync(id);
        if (transaction is null)
            return "NOT_FOUND";

        // 포인트 결제였으면 잔액 복구
        if (transaction.PaymentMethod.Type == PaymentMethodType.Point
            && transaction.PaymentMethod.PointBudget is not null)
        {
            transaction.PaymentMethod.PointBudget.RemainingAmount += transaction.Amount;
        }

        // DB 삭제
        await _transactionRepo.DeleteAsync(transaction);

        return null;
    }

    // 응답 DTO 매핑
    private static TransactionResponse MapToResponse(Transaction t) => new()
    {
        Id = t.Id,
        Amount = t.Amount,
        Date = t.Date,
        Memo = t.Memo,
        Type = t.Type.ToString(),
        CategoryId = t.CategoryId,
        CategoryName = t.Category.Name,
        PaymentMethodId = t.PaymentMethodId,
        PaymentMethodName = t.PaymentMethod.Name,
        IsIncludedInTotal = t.IsIncludedInTotal,
        RecurringTransactionId = t.RecurringTransactionId
    };
}
