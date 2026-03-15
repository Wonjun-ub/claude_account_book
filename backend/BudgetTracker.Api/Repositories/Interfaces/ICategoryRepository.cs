using BudgetTracker.Api.Models.Entities;
using BudgetTracker.Api.Models.Enums;

namespace BudgetTracker.Api.Repositories.Interfaces;

public interface ICategoryRepository
{
    // 목록 조회 (타입 필터 옵션)
    Task<IEnumerable<Category>> GetAllAsync(CategoryType? type);

    // 단건 조회
    Task<Category?> GetByIdAsync(int id);

    // 생성
    Task<Category> CreateAsync(Category category);

    // 수정
    Task UpdateAsync(Category category);

    // 삭제
    Task DeleteAsync(Category category);

    // 연결된 거래 존재 여부 확인
    Task<bool> HasTransactionsAsync(int id);
}
