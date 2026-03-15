using BudgetTracker.Api.DTOs.Requests;
using BudgetTracker.Api.DTOs.Responses;

namespace BudgetTracker.Api.Services.Interfaces;

public interface IRecurringService
{
    // 전체 목록 조회
    Task<IEnumerable<RecurringTransactionResponse>> GetAllAsync();

    // 생성
    Task<(RecurringTransactionResponse? Response, string? ErrorMessage)> CreateAsync(CreateRecurringTransactionRequest request);

    // 삭제
    Task<string?> DeleteAsync(int id);

    // 특정 월의 반복 지출 자동 생성 (on-demand)
    Task ApplyRecurringTransactionsAsync(int year, int month);
}
