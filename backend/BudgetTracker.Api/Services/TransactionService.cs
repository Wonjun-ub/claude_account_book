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
            return (null, "CATEGORY_NOT_FOUND");

        // 결제수단 존재 확인 + 포인트 결제수단인 경우 잔액 확인
        var paymentMethod = await _paymentMethodRepo.GetByIdAsync(request.PaymentMethodId);
        if (paymentMethod is null)
            return (null, "PAYMENT_METHOD_NOT_FOUND");

        // 포인트 차감 처리
        if (paymentMethod.Type == PaymentMethodType.Point && paymentMethod.PointBudget is not null)
        {
            if (paymentMethod.PointBudget.RemainingAmount < request.Amount)
                return (null, "POINT_INSUFFICIENT");

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
            IsIncludedInTotal = request.IsIncludedInTotal,
            RecurringTransactionId = request.RecurringTransactionId,
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
            return (null, "CATEGORY_NOT_FOUND");

        // 새 결제수단 로드
        var newPaymentMethod = await _paymentMethodRepo.GetByIdAsync(request.PaymentMethodId);
        if (newPaymentMethod is null)
            return (null, "PAYMENT_METHOD_NOT_FOUND");

        // 포인트 잔액 사전 검증 (엔티티 수정 전에 먼저 확인)
        // 같은 결제수단 변경(금액만 변경)이면 복구 예정 금액을 가용 잔액에 포함
        var oldPaymentMethod = transaction.PaymentMethod;
        if (newPaymentMethod.Type == PaymentMethodType.Point && newPaymentMethod.PointBudget is not null)
        {
            decimal availableBalance = newPaymentMethod.PointBudget.RemainingAmount;
            if (transaction.PaymentMethodId == request.PaymentMethodId
                && oldPaymentMethod.Type == PaymentMethodType.Point
                && oldPaymentMethod.PointBudget is not null)
            {
                availableBalance += transaction.Amount; // 복구 예정 금액 포함
            }

            if (availableBalance < request.Amount)
                return (null, "POINT_INSUFFICIENT");
        }

        // 검증 통과 후 안전하게 복구·차감 처리
        if (oldPaymentMethod.Type == PaymentMethodType.Point && oldPaymentMethod.PointBudget is not null)
        {
            oldPaymentMethod.PointBudget.RemainingAmount += transaction.Amount;
        }

        if (newPaymentMethod.Type == PaymentMethodType.Point && newPaymentMethod.PointBudget is not null)
        {
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
        RecurringTransactionId = t.RecurringTransactionId,
        InstallmentTransactionId = t.InstallmentTransactionId,
        InstallmentSequence = t.InstallmentSequence,
        InstallmentTotalInstallments = t.InstallmentTransaction?.TotalInstallments,
    };
}
