using BudgetTracker.Api.DTOs.Requests;
using BudgetTracker.Api.DTOs.Responses;
using BudgetTracker.Api.Models.Entities;
using BudgetTracker.Api.Models.Enums;
using BudgetTracker.Api.Repositories.Interfaces;
using BudgetTracker.Api.Services.Interfaces;

namespace BudgetTracker.Api.Services;

public class InstallmentTransactionService : IInstallmentTransactionService
{
    private readonly IInstallmentTransactionRepository _repo;
    private readonly ITransactionRepository _transactionRepo;

    public InstallmentTransactionService(
        IInstallmentTransactionRepository repo,
        ITransactionRepository transactionRepo)
    {
        _repo = repo;
        _transactionRepo = transactionRepo;
    }

    public async Task<IEnumerable<InstallmentTransactionResponse>> GetAllAsync()
    {
        var items = await _repo.GetAllAsync();
        return items.Select(MapToResponse);
    }

    // 할부 원부 생성 + N건 Transaction 일괄 생성
    public async Task<(InstallmentTransactionResponse? Response, string? ErrorMessage)> CreateAsync(CreateInstallmentTransactionRequest request)
    {
        // 카테고리 존재 확인
        if (!await _repo.CategoryExistsAsync(request.CategoryId))
            return (null, "존재하지 않는 카테고리입니다.");

        // 결제수단 존재 확인
        if (!await _repo.PaymentMethodExistsAsync(request.PaymentMethodId))
            return (null, "존재하지 않는 결제수단입니다.");

        // 할부 금액 계산 (floor 방식, 나머지는 1회차에 합산)
        var (monthly, first) = CalcInstallment(request.TotalAmount, request.TotalInstallments);

        // 원부 생성 (CreateAsync가 SaveChanges + 탐색 속성 로드까지 처리)
        var master = new InstallmentTransaction
        {
            TotalAmount = request.TotalAmount,
            MonthlyAmount = monthly,
            FirstMonthAmount = first,
            TotalInstallments = request.TotalInstallments,
            StartDate = request.StartDate.ToUniversalTime(),
            CategoryId = request.CategoryId,
            PaymentMethodId = request.PaymentMethodId,
            Memo = request.Memo,
            IsActive = true,
        };

        var created = await _repo.CreateAsync(master);

        // 카테고리 타입으로 거래 유형 결정
        var txType = created.Category.Type == CategoryType.Income
            ? TransactionType.Income
            : TransactionType.Expense;

        // N건 Transaction 생성 (seq 1~N, 월 +1씩)
        var baseDate = request.StartDate.ToUniversalTime();
        for (int seq = 1; seq <= request.TotalInstallments; seq++)
        {
            var targetDate = AddMonthsSafe(baseDate, seq - 1);

            var transaction = new Transaction
            {
                Amount = seq == 1 ? first : monthly,
                Date = targetDate,
                Memo = request.Memo,
                Type = txType,
                CategoryId = request.CategoryId,
                PaymentMethodId = request.PaymentMethodId,
                IsIncludedInTotal = request.IsIncludedInTotal,
                InstallmentTransactionId = created.Id,
                InstallmentSequence = seq,
            };

            await _transactionRepo.CreateAsync(transaction);
        }

        return (MapToResponse(created), null);
    }

    // 3가지 삭제 모드
    public async Task<string?> DeleteAsync(int id, string mode, int? seq)
    {
        var master = await _repo.GetByIdAsync(id);
        if (master is null)
            return "NOT_FOUND";

        switch (mode.ToLower())
        {
            case "all":
            {
                // 모든 회차 거래 삭제 + 원부 삭제
                var txs = await _repo.GetTransactionsByMasterAsync(id);
                await _repo.DeleteTransactionsAsync(txs);
                await _repo.DeleteAsync(master);
                break;
            }
            case "fromhere":
            {
                if (!seq.HasValue)
                    return "MISSING_SEQ";
                var txs = await _repo.GetTransactionsFromSeqAsync(id, seq.Value);
                await _repo.DeleteTransactionsAsync(txs);
                break;
            }
            case "single":
            {
                if (!seq.HasValue)
                    return "MISSING_SEQ";
                var tx = await _repo.GetTransactionBySeqAsync(id, seq.Value);
                if (tx is not null)
                    await _repo.DeleteTransactionsAsync(new[] { tx });
                break;
            }
            default:
                return "INVALID_MODE";
        }

        return null;
    }

    // 할부 금액 계산: (monthlyAmount, firstMonthAmount)
    // floor 방식 — 나머지는 1회차에 합산
    public static (decimal monthly, decimal first) CalcInstallment(decimal total, int months)
    {
        decimal monthly = Math.Floor(total / months);
        decimal remainder = total - monthly * months;
        decimal first = monthly + remainder;
        return (monthly, first);
    }

    // 월 덧셈 (말일 초과 시 DateTime.AddMonths가 자동으로 처리)
    private static DateTime AddMonthsSafe(DateTime date, int months)
    {
        return date.AddMonths(months);
    }

    private static InstallmentTransactionResponse MapToResponse(InstallmentTransaction i) => new()
    {
        Id = i.Id,
        TotalAmount = i.TotalAmount,
        MonthlyAmount = i.MonthlyAmount,
        FirstMonthAmount = i.FirstMonthAmount,
        TotalInstallments = i.TotalInstallments,
        StartDate = i.StartDate,
        CategoryId = i.CategoryId,
        CategoryName = i.Category.Name,
        PaymentMethodId = i.PaymentMethodId,
        PaymentMethodName = i.PaymentMethod.Name,
        Memo = i.Memo,
        IsActive = i.IsActive,
    };
}
