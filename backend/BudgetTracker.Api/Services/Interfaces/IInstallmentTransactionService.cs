using BudgetTracker.Api.DTOs.Requests;
using BudgetTracker.Api.DTOs.Responses;

namespace BudgetTracker.Api.Services.Interfaces;

public interface IInstallmentTransactionService
{
    Task<IEnumerable<InstallmentTransactionResponse>> GetAllAsync();

    Task<(InstallmentTransactionResponse? Response, string? ErrorMessage)> CreateAsync(CreateInstallmentTransactionRequest request);

    // mode: "all" | "fromHere" | "single", seq: 회차 번호 (fromHere/single 필수)
    Task<string?> DeleteAsync(int id, string mode, int? seq);
}
