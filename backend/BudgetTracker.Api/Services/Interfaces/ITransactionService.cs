using BudgetTracker.Api.DTOs.Requests;
using BudgetTracker.Api.DTOs.Responses;
using Microsoft.AspNetCore.Mvc;

namespace BudgetTracker.Api.Services.Interfaces;

public interface ITransactionService
{
    // 목록 조회 + 검색/필터
    Task<IEnumerable<TransactionResponse>> GetAllAsync(
        int? year,
        int? month,
        string? keyword,
        int? categoryId,
        int? paymentMethodId,
        DateTime? from,
        DateTime? to,
        decimal? minAmount,
        decimal? maxAmount);

    // 단건 조회
    Task<TransactionResponse?> GetByIdAsync(int id);

    // 생성 (포인트 차감 로직 포함)
    Task<(TransactionResponse? Response, string? ErrorMessage)> CreateAsync(CreateTransactionRequest request);

    // 수정 (이전 포인트 복구 후 새 포인트 차감)
    Task<(TransactionResponse? Response, string? ErrorMessage)> UpdateAsync(int id, UpdateTransactionRequest request);

    // 삭제 (포인트 복구 후 삭제)
    Task<string?> DeleteAsync(int id);
}
