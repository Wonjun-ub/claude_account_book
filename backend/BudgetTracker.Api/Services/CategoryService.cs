using BudgetTracker.Api.DTOs.Requests;
using BudgetTracker.Api.DTOs.Responses;
using BudgetTracker.Api.Models.Entities;
using BudgetTracker.Api.Models.Enums;
using BudgetTracker.Api.Repositories.Interfaces;
using BudgetTracker.Api.Services.Interfaces;

namespace BudgetTracker.Api.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepo;

    public CategoryService(ICategoryRepository categoryRepo)
    {
        _categoryRepo = categoryRepo;
    }

    // 목록 조회 (타입 필터 옵션)
    public async Task<IEnumerable<CategoryResponse>> GetAllAsync(CategoryType? type)
    {
        var categories = await _categoryRepo.GetAllAsync(type);

        return categories.Select(c => new CategoryResponse
        {
            Id = c.Id,
            Name = c.Name,
            Type = c.Type.ToString(),
            IsDefault = c.IsDefault
        });
    }

    // 생성
    public async Task<CategoryResponse> CreateAsync(CreateCategoryRequest request)
    {
        // 엔티티 생성
        var category = new Category
        {
            Name = request.Name,
            Type = request.Type,
            IsDefault = false
        };

        // DB 저장
        var created = await _categoryRepo.CreateAsync(category);

        return new CategoryResponse
        {
            Id = created.Id,
            Name = created.Name,
            Type = created.Type.ToString(),
            IsDefault = created.IsDefault
        };
    }

    // 수정
    public async Task<(CategoryResponse? Response, string? ErrorMessage)> UpdateAsync(int id, UpdateCategoryRequest request)
    {
        // 카테고리 존재 확인
        var category = await _categoryRepo.GetByIdAsync(id);
        if (category is null)
            return (null, "NOT_FOUND");

        // 필드 업데이트
        category.Name = request.Name;
        category.Type = request.Type;

        // DB 저장
        await _categoryRepo.UpdateAsync(category);

        return (new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Type = category.Type.ToString(),
            IsDefault = category.IsDefault
        }, null);
    }

    // 삭제 (isDefault 체크, 연결 거래 체크)
    public async Task<string?> DeleteAsync(int id)
    {
        // 카테고리 존재 확인
        var category = await _categoryRepo.GetByIdAsync(id);
        if (category is null)
            return "NOT_FOUND";

        // 기본 카테고리는 삭제 불가
        if (category.IsDefault)
            return "기본 카테고리는 삭제할 수 없습니다.";

        // 연결된 거래가 있으면 삭제 불가
        bool hasTransactions = await _categoryRepo.HasTransactionsAsync(id);
        if (hasTransactions)
            return "이 카테고리에 연결된 거래가 있어 삭제할 수 없습니다.";

        // DB 삭제
        await _categoryRepo.DeleteAsync(category);

        return null;
    }
}
