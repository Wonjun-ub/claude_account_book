using BudgetTracker.Api.DTOs.Requests;
using BudgetTracker.Api.DTOs.Responses;

namespace BudgetTracker.Api.Services.Interfaces;

public interface IRecurringService
{
    // 전체 목록 조회
    Task<IEnumerable<RecurringTransactionResponse>> GetAllAsync();

    // 생성
    Task<(RecurringTransactionResponse? Response, string? ErrorMessage)> CreateAsync(CreateRecurringTransactionRequest request);

    // 삭제 (mode: "all" | "fromHere" | "skipMonth")
    // fromHere: date 파라미터 필요 (해당일 이후 삭제 + 비활성화)
    // skipMonth: year, month 파라미터 필요 (RecurringSkip 생성)
    Task<string?> DeleteAsync(int id, string mode, DateTime? date, int? year, int? month);

    // 특정 월의 반복 지출 자동 생성 (on-demand)
    Task ApplyRecurringTransactionsAsync(int year, int month);

    // 특정 월에 미등록된 반복 목록 (예정 배너용)
    Task<IEnumerable<RecurringTransactionResponse>> GetPendingAsync(int year, int month);
}
