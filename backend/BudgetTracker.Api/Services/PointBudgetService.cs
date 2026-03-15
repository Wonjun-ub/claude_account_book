using BudgetTracker.Api.DTOs.Requests;
using BudgetTracker.Api.DTOs.Responses;
using BudgetTracker.Api.Models.Entities;
using BudgetTracker.Api.Repositories.Interfaces;
using BudgetTracker.Api.Services.Interfaces;

namespace BudgetTracker.Api.Services;

public class PointBudgetService : IPointBudgetService
{
    private readonly IPointBudgetRepository _pointBudgetRepo;

    public PointBudgetService(IPointBudgetRepository pointBudgetRepo)
    {
        _pointBudgetRepo = pointBudgetRepo;
    }

    // 전체 목록 조회
    public async Task<IEnumerable<PointBudgetResponse>> GetAllAsync()
    {
        var budgets = await _pointBudgetRepo.GetAllAsync();

        return budgets.Select(p => new PointBudgetResponse
        {
            Id = p.Id,
            Name = p.Name,
            TotalAmount = p.TotalAmount,
            RemainingAmount = p.RemainingAmount
        });
    }

    // 단건 조회
    public async Task<PointBudgetResponse?> GetByIdAsync(int id)
    {
        var budget = await _pointBudgetRepo.GetByIdAsync(id);

        if (budget is null)
            return null;

        return new PointBudgetResponse
        {
            Id = budget.Id,
            Name = budget.Name,
            TotalAmount = budget.TotalAmount,
            RemainingAmount = budget.RemainingAmount
        };
    }

    // 생성 (RemainingAmount = TotalAmount로 초기화)
    public async Task<PointBudgetResponse> CreateAsync(CreatePointBudgetRequest request)
    {
        // 엔티티 생성 (RemainingAmount = TotalAmount로 초기화)
        var budget = new PointBudget
        {
            Name = request.Name,
            TotalAmount = request.TotalAmount,
            RemainingAmount = request.TotalAmount
        };

        // DB 저장
        var created = await _pointBudgetRepo.CreateAsync(budget);

        return new PointBudgetResponse
        {
            Id = created.Id,
            Name = created.Name,
            TotalAmount = created.TotalAmount,
            RemainingAmount = created.RemainingAmount
        };
    }
}
