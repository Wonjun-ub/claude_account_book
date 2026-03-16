using BudgetTracker.Api.Models.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace BudgetTracker.Api.Repositories.Interfaces;

public interface IRecurringRepository
{
    // 모든 활성 반복 지출 조회 (포인트 예산 포함)
    Task<IEnumerable<RecurringTransaction>> GetAllActiveAsync();

    // 전체 목록 조회 (카테고리, 결제수단 포함)
    Task<IEnumerable<RecurringTransaction>> GetAllAsync();

    // 단건 조회
    Task<RecurringTransaction?> GetByIdAsync(int id);

    // 생성
    Task<RecurringTransaction> CreateAsync(RecurringTransaction recurring);

    // 수정
    Task UpdateAsync(RecurringTransaction recurring);

    // 삭제
    Task DeleteAsync(RecurringTransaction recurring);

    // 카테고리 존재 여부 확인
    Task<bool> CategoryExistsAsync(int categoryId);

    // 결제수단 존재 여부 확인
    Task<bool> PaymentMethodExistsAsync(int paymentMethodId);

    // 특정 반복의 거래 삭제 (해당 날짜 이후, 포인트 복구를 위해 거래 목록 반환)
    Task<IEnumerable<Transaction>> DeleteTransactionsFromDateAsync(int recurringId, DateTime fromDate);

    // 특정 반복의 거래 삭제 (기간 내, 포인트 복구를 위해 거래 목록 반환)
    Task<IEnumerable<Transaction>> DeleteTransactionsInPeriodAsync(int recurringId, DateTime periodStart, DateTime periodEnd);

    // 특정 반복의 모든 거래 삭제 (포인트 복구를 위해 거래 목록 반환)
    Task<IEnumerable<Transaction>> DeleteAllTransactionsAsync(int recurringId);

    // 스킵 등록
    Task AddSkipAsync(RecurringSkip skip);

    // 스킵 존재 여부 확인
    Task<bool> IsSkippedAsync(int recurringId, int year, int month);

    // 특정 월에 이미 등록된 반복 거래 존재 여부
    Task<bool> HasTransactionInPeriodAsync(int recurringId, DateTime periodStart, DateTime periodEnd);

    // DB 트랜잭션 시작 (원자적 처리용)
    Task<IDbContextTransaction> BeginTransactionAsync();

    // 변경사항 저장
    Task SaveChangesAsync();
}
