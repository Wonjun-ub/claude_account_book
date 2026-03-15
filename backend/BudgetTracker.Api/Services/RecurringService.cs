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
        return items.Select(MapToResponse);
    }

    // 생성
    public async Task<(RecurringTransactionResponse? Response, string? ErrorMessage)> CreateAsync(CreateRecurringTransactionRequest request)
    {
        // 카테고리 존재 확인
        if (!await _recurringRepo.CategoryExistsAsync(request.CategoryId))
            return (null, "존재하지 않는 카테고리입니다.");

        // 결제수단 존재 확인
        if (!await _recurringRepo.PaymentMethodExistsAsync(request.PaymentMethodId))
            return (null, "존재하지 않는 결제수단입니다.");

        // 엔티티 생성
        var recurring = new RecurringTransaction
        {
            Amount = request.Amount,
            CategoryId = request.CategoryId,
            PaymentMethodId = request.PaymentMethodId,
            Type = RecurringType.Fixed,
            DayOfMonth = request.DayOfMonth,
            Memo = request.Memo,
            IsActive = true,
            StartDate = request.StartDate?.ToUniversalTime(),
            EndDate = request.EndDate?.ToUniversalTime(),
        };

        // DB 저장 (탐색 속성 로드 포함)
        var created = await _recurringRepo.CreateAsync(recurring);

        return (MapToResponse(created), null);
    }

    // 삭제 (mode별 처리)
    public async Task<string?> DeleteAsync(int id, string mode, DateTime? date, int? year, int? month)
    {
        var recurring = await _recurringRepo.GetByIdAsync(id);
        if (recurring is null)
            return "NOT_FOUND";

        switch (mode.ToLower())
        {
            case "all":
                // 모든 연결 거래 삭제 + 원부 삭제
                await _recurringRepo.DeleteAllTransactionsAsync(id);
                await _recurringRepo.DeleteAsync(recurring);
                break;

            case "fromhere":
                // 해당 날짜 이후 거래 삭제 + 비활성화
                if (!date.HasValue)
                    return "MISSING_DATE";
                await _recurringRepo.DeleteTransactionsFromDateAsync(id, date.Value.ToUniversalTime());
                recurring.IsActive = false;
                recurring.EndDate = date.Value.ToUniversalTime().AddDays(-1);
                await _recurringRepo.UpdateAsync(recurring);
                break;

            case "skipmonth":
                // 해당 월 스킵 등록 + 해당 월 거래 있으면 삭제
                if (!year.HasValue || !month.HasValue)
                    return "MISSING_YEAR_MONTH";

                // 이미 스킵되어 있으면 무시
                if (!await _recurringRepo.IsSkippedAsync(id, year.Value, month.Value))
                {
                    var skip = new RecurringSkip
                    {
                        RecurringTransactionId = id,
                        Year = year.Value,
                        Month = month.Value,
                    };
                    await _recurringRepo.AddSkipAsync(skip);
                }

                // 해당 월에 이미 생성된 거래가 있으면 삭제
                var settings = await _settingsRepo.GetAsync();
                int monthStartDay = settings?.MonthStartDay ?? 1;
                var (periodStart, periodEnd) = DateRangeHelper.GetMonthRange(year.Value, month.Value, monthStartDay);
                await _recurringRepo.DeleteTransactionsInPeriodAsync(id, periodStart, periodEnd);
                break;

            default:
                return "INVALID_MODE";
        }

        return null;
    }

    // 특정 월의 반복 지출 자동 생성 (on-demand)
    public async Task ApplyRecurringTransactionsAsync(int year, int month)
    {
        // 사용자 설정 및 기간 계산
        var settings = await _settingsRepo.GetAsync();
        int monthStartDay = settings?.MonthStartDay ?? 1;

        var (periodStart, periodEnd) = DateRangeHelper.GetMonthRange(year, month, monthStartDay);

        // 활성화된 반복 지출 조회 (스킵 포함)
        var recurringList = await _recurringRepo.GetAllActiveAsync();

        foreach (var recurring in recurringList)
        {
            // StartDate 이전이면 스킵
            if (recurring.StartDate.HasValue && recurring.StartDate.Value > periodEnd)
                continue;

            // EndDate 이후이면 스킵
            if (recurring.EndDate.HasValue && recurring.EndDate.Value < periodStart)
                continue;

            // 이 월 스킵 여부 확인
            bool isSkipped = recurring.RecurringSkips.Any(s => s.Year == year && s.Month == month);
            if (isSkipped)
                continue;

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

            // 카테고리 타입으로 거래 유형 결정 (Income/Expense)
            var transactionType = recurring.Category.Type == CategoryType.Income
                ? TransactionType.Income
                : TransactionType.Expense;

            var transaction = new Transaction
            {
                Amount = recurring.Amount,
                Date = transactionDate,
                Memo = recurring.Memo,
                Type = transactionType,
                CategoryId = recurring.CategoryId,
                PaymentMethodId = recurring.PaymentMethodId,
                IsIncludedInTotal = true,
                RecurringTransactionId = recurring.Id
            };

            await _transactionRepo.CreateAsync(transaction);
        }
    }

    // 특정 월에 미등록된 반복 목록 (예정 배너용)
    public async Task<IEnumerable<RecurringTransactionResponse>> GetPendingAsync(int year, int month)
    {
        var settings = await _settingsRepo.GetAsync();
        int monthStartDay = settings?.MonthStartDay ?? 1;
        var (periodStart, periodEnd) = DateRangeHelper.GetMonthRange(year, month, monthStartDay);

        var recurringList = await _recurringRepo.GetAllActiveAsync();

        var pending = new List<RecurringTransactionResponse>();
        foreach (var recurring in recurringList)
        {
            // StartDate 이전이면 스킵
            if (recurring.StartDate.HasValue && recurring.StartDate.Value > periodEnd)
                continue;

            // EndDate 이후이면 스킵
            if (recurring.EndDate.HasValue && recurring.EndDate.Value < periodStart)
                continue;

            // 이 월 스킵 여부 확인
            bool isSkipped = recurring.RecurringSkips.Any(s => s.Year == year && s.Month == month);
            if (isSkipped)
                continue;

            // 이 기간에 이미 거래가 있으면 제외
            bool hasTransaction = await _recurringRepo.HasTransactionInPeriodAsync(recurring.Id, periodStart, periodEnd);
            if (hasTransaction)
                continue;

            pending.Add(MapToResponse(recurring));
        }

        return pending;
    }

    private static RecurringTransactionResponse MapToResponse(RecurringTransaction r) => new()
    {
        Id = r.Id,
        Amount = r.Amount,
        CategoryId = r.CategoryId,
        CategoryName = r.Category.Name,
        PaymentMethodId = r.PaymentMethodId,
        PaymentMethodName = r.PaymentMethod.Name,
        Type = r.Type.ToString(),
        DayOfMonth = r.DayOfMonth,
        Memo = r.Memo,
        IsActive = r.IsActive,
        StartDate = r.StartDate,
        EndDate = r.EndDate,
    };
}
