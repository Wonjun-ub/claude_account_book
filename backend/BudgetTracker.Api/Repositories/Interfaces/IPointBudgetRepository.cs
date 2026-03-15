using BudgetTracker.Api.Models.Entities;

namespace BudgetTracker.Api.Repositories.Interfaces;

public interface IPointBudgetRepository
{
    // 전체 목록 조회
    Task<IEnumerable<PointBudget>> GetAllAsync();

    // 단건 조회
    Task<PointBudget?> GetByIdAsync(int id);

    // 생성
    Task<PointBudget> CreateAsync(PointBudget pointBudget);

    // 수정
    Task UpdateAsync(PointBudget pointBudget);
}
