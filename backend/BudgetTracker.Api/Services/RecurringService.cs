using BudgetTracker.Api.DTOs.Requests;
using BudgetTracker.Api.DTOs.Responses;
using BudgetTracker.Api.Helpers;
using BudgetTracker.Api.Models.Entities;
using BudgetTracker.Api.Models.Enums;
using BudgetTracker.Api.Repositories.Interfaces;
using BudgetTracker.Api.Services.Interfaces;

namespace BudgetTracker.Api.Services;

public class RecurringService : IRecurringService
{
    private readonly IRecurringRepository _recurringRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly ISettingsRepository _settingsRepo;

    public RecurringService(
        IRecurringRepository recurringRepo,
        ITransactionRepository transactionRepo,
        ISettingsRepository settingsRepo)
    {
        _recurringRepo = recurringRepo;
        _transactionRepo = transactionRepo;
        _settingsRepo = settingsRepo;
    }

    // 전체 목록 조회
    public async Task<IEnumerable<RecurringTransactionResponse>> GetAllAsync()
    {
        var items = await _recurringRepo.GetAllAsync();

        return items.Select(r => new RecurringTransactionResponse
        {
            Id = r.Id,
            Amount = r.Amount,
            CategoryId = r.CategoryId,
            CategoryName = r.Category.Name,
            PaymentMethodId = r.PaymentMethodId,
            PaymentMethodName = r.PaymentMethod.Name,
            Type = r.Type.ToString(),
            DayOfMonth = r.DayOfMonth,
            TotalInstallments = r.TotalInstallments,
            RemainingInstallments = r.RemainingInstallments,
            Memo = r.Memo,
            IsActive = r.IsActive
        });
    }

    // 생성
    public async Task<(RecurringTransactionResponse? Response, string? ErrorMessage)> CreateAsync(CreateRecurringTransactionRequest request)
    {
        // 카테고리 존재 확인
        bool categoryExists = await _recurringRepo.CategoryExistsAsync(request.CategoryId);
        if (!categoryExists)
            return (null, "존재하지 않는 카테고리입니다.");

        // 결제수단 존재 확인
        bool methodExists = await _recurringRepo.PaymentMethodExistsAsync(request.PaymentMethodId);
        if (!methodExists)
            return (null, "존재하지 않는 결제수단입니다.");

        // Installment 타입이면 TotalInstallments 필수
        if (request.Type == RecurringType.Installment && (!request.TotalInstallments.HasValue || request.TotalInstallments.Value <= 0))
            return (null, "할부 타입은 총 할부 횟수(TotalInstallments)가 필요합니다.");

        // 엔티티 생성
        var recurring = new RecurringTransaction
        {
            Amount = request.Amount,
            CategoryId = request.CategoryId,
            PaymentMethodId = request.PaymentMethodId,
            Type = request.Type,
            DayOfMonth = request.DayOfMonth,
            TotalInstallments = request.Type == RecurringType.Installment ? request.TotalInstallments : null,
            RemainingInstallments = request.Type == RecurringType.Installment ? request.TotalInstallments : null,
            Memo = request.Memo,
            IsActive = true
        };

        // DB 저장 (탐색 속성 로드 포함)
        var created = await _recurringRepo.CreateAsync(recurring);

        return (new RecurringTransactionResponse
        {
            Id = created.Id,
            Amount = created.Amount,
            CategoryId = created.CategoryId,
            CategoryName = created.Category.Name,
            PaymentMethodId = created.PaymentMethodId,
            PaymentMethodName = created.PaymentMethod.Name,
            Type = created.Type.ToString(),
            DayOfMonth = created.DayOfMonth,
            TotalInstallments = created.TotalInstallments,
            RemainingInstallments = created.RemainingInstallments,
            Memo = created.Memo,
            IsActive = created.IsActive
        }, null);
    }

    // 삭제
    public async Task<string?> DeleteAsync(int id)
    {
        // 반복 지출 존재 확인
        var recurring = await _recurringRepo.GetByIdAsync(id);
        if (recurring is null)
            return "NOT_FOUND";

        // 연결된 거래는 SetNull이므로 그냥 삭제
        await _recurringRepo.DeleteAsync(recurring);

        return null;
    }

    // 특정 월의 반복 지출 자동 생성 (on-demand)
    public async Task ApplyRecurringTransactionsAsync(int year, int month)
    {
        // 사용자 설정 및 기간 계산
        var settings = await _settingsRepo.GetAsync();
        int monthStartDay = settings?.MonthStartDay ?? 1;

        var (periodStart, periodEnd) = DateRangeHelper.GetMonthRange(year, month, monthStartDay);

        // 활성화된 반복 지출 조회 (포인트 예산 포함)
        var recurringList = await _recurringRepo.GetAllActiveAsync();

        foreach (var recurring in recurringList)
        {
            // 이 월의 실제 거래 날짜 계산
            DateTime transactionDate = DateRangeHelper.GetTransactionDate(year, month, recurring.DayOfMonth);

            // 해당 날짜가 이 월의 기간 내에 있는지 확인
            if (transactionDate < periodStart || transactionDate > periodEnd)
                continue;

            // 이미 이 월에 이 반복 지출로 생성된 거래가 있는지 확인 (멱등성)
            bool alreadyCreated = await _transactionRepo.GetByRecurringAndDateAsync(recurring.Id, transactionDate);
            if (alreadyCreated)
                continue;

            // 포인트 잔액 확인 및 차감
            if (recurring.PaymentMethod.Type == PaymentMethodType.Point
                && recurring.PaymentMethod.PointBudget is not null)
            {
                if (recurring.PaymentMethod.PointBudget.RemainingAmount < recurring.Amount)
                    continue; // 잔액 부족 시 스킵

                recurring.PaymentMethod.PointBudget.RemainingAmount -= recurring.Amount;
            }

            // 새 거래 생성 (Repository의 CreateAsync를 사용하지 않고 직접 처리 — 배치 저장을 위해)
            var transaction = new Transaction
            {
                Amount = recurring.Amount,
                Date = transactionDate,
                Memo = recurring.Memo,
                Type = TransactionType.Expense,
                CategoryId = recurring.CategoryId,
                PaymentMethodId = recurring.PaymentMethodId,
                IsIncludedInTotal = true,
                RecurringTransactionId = recurring.Id
            };

            await _transactionRepo.CreateAsync(transaction);

            // 할부 처리: 남은 횟수 차감
            if (recurring.Type == RecurringType.Installment && recurring.RemainingInstallments.HasValue)
            {
                recurring.RemainingInstallments--;
                if (recurring.RemainingInstallments <= 0)
                    recurring.IsActive = false;

                await _recurringRepo.UpdateAsync(recurring);
            }
        }
    }
}
