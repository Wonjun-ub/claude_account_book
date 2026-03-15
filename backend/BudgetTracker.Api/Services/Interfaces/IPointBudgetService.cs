using BudgetTracker.Api.DTOs.Requests;
using BudgetTracker.Api.DTOs.Responses;

namespace BudgetTracker.Api.Services.Interfaces;

public interface IPointBudgetService
{
    // 전체 목록 조회
    Task<IEnumerable<PointBudgetResponse>> GetAllAsync();

    // 단건 조회
    Task<PointBudgetResponse?> GetByIdAsync(int id);

    // 생성 (remainingAmount = totalAmount 초기화)
    Task<PointBudgetResponse> CreateAsync(CreatePointBudgetRequest request);
}
