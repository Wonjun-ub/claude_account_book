using BudgetTracker.Api.DTOs.Requests;
using BudgetTracker.Api.DTOs.Responses;
using BudgetTracker.Api.Models.Enums;

namespace BudgetTracker.Api.Services.Interfaces;

public interface ICategoryService
{
    // 목록 조회 (타입 필터 옵션)
    Task<IEnumerable<CategoryResponse>> GetAllAsync(CategoryType? type);

    // 생성
    Task<CategoryResponse> CreateAsync(CreateCategoryRequest request);

    // 수정
    Task<(CategoryResponse? Response, string? ErrorMessage)> UpdateAsync(int id, UpdateCategoryRequest request);

    // 삭제 (isDefault 체크, 연결 거래 체크)
    Task<string?> DeleteAsync(int id);
}
