using BudgetTracker.Api.Data;
using BudgetTracker.Api.DTOs.Requests;
using BudgetTracker.Api.DTOs.Responses;
using BudgetTracker.Api.Helpers;
using BudgetTracker.Api.Models.Entities;
using BudgetTracker.Api.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.Api.Controllers;

[ApiController]
[Route("api/recurring-transactions")]
public class RecurringTransactionsController : ControllerBase
{
    private readonly BudgetTrackerDbContext _db;

    public RecurringTransactionsController(BudgetTrackerDbContext db)
    {
        _db = db;
    }

    // GET /api/recurring-transactions
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RecurringTransactionResponse>>> GetAll()
    {
        var items = await _db.RecurringTransactions
            .Include(r => r.Category)
            .Include(r => r.PaymentMethod)
            .OrderByDescending(r => r.IsActive)
            .ThenBy(r => r.DayOfMonth)
            .Select(r => new RecurringTransactionResponse
            {
                Id = r.Id,
                Amount = r.Amount,
                CategoryId = r.CategoryId,
                CategoryName = r.Category.Name,
                PaymentMethodId = r.PaymentMethodId,
                PaymentMethodName = r.PaymentMethod.Name,
                Type = r.Type.ToString(),
                DayOfMonth = r.DayOfMonth,
                TotalInstallments = r.TotalInstallments,
                RemainingInstallments = r.RemainingInstallments,
                Memo = r.Memo,
                IsActive = r.IsActive
            })
            .ToListAsync();

        return Ok(items);
    }

    // POST /api/recurring-transactions
    [HttpPost]
    public async Task<ActionResult<RecurringTransactionResponse>> Create([FromBody] CreateRecurringTransactionRequest request)
    {
        // 카테고리 존재 확인
        bool categoryExists = await _db.Categories.AnyAsync(c => c.Id == request.CategoryId);
        if (!categoryExists)
            return BadRequest(new { message = "존재하지 않는 카테고리입니다." });

        // 결제수단 존재 확인
        bool methodExists = await _db.PaymentMethods.AnyAsync(p => p.Id == request.PaymentMethodId);
        if (!methodExists)
            return BadRequest(new { message = "존재하지 않는 결제수단입니다." });

        // Installment 타입이면 TotalInstallments 필수
        if (request.Type == RecurringType.Installment && (!request.TotalInstallments.HasValue || request.TotalInstallments.Value <= 0))
            return BadRequest(new { message = "할부 타입은 총 할부 횟수(TotalInstallments)가 필요합니다." });

        var recurring = new RecurringTransaction
        {
            Amount = request.Amount,
            CategoryId = request.CategoryId,
            PaymentMethodId = request.PaymentMethodId,
            Type = request.Type,
            DayOfMonth = request.DayOfMonth,
            TotalInstallments = request.Type == RecurringType.Installment ? request.TotalInstallments : null,
            RemainingInstallments = request.Type == RecurringType.Installment ? request.TotalInstallments : null,
            Memo = request.Memo,
            IsActive = true
        };

        _db.RecurringTransactions.Add(recurring);
        await _db.SaveChangesAsync();

        await _db.Entry(recurring).Reference(r => r.Category).LoadAsync();
        await _db.Entry(recurring).Reference(r => r.PaymentMethod).LoadAsync();

        return CreatedAtAction(nameof(GetAll), new { id = recurring.Id }, new RecurringTransactionResponse
        {
            Id = recurring.Id,
            Amount = recurring.Amount,
            CategoryId = recurring.CategoryId,
            CategoryName = recurring.Category.Name,
            PaymentMethodId = recurring.PaymentMethodId,
            PaymentMethodName = recurring.PaymentMethod.Name,
            Type = recurring.Type.ToString(),
            DayOfMonth = recurring.DayOfMonth,
            TotalInstallments = recurring.TotalInstallments,
            RemainingInstallments = recurring.RemainingInstallments,
            Memo = recurring.Memo,
            IsActive = recurring.IsActive
        });
    }

    // DELETE /api/recurring-transactions/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var recurring = await _db.RecurringTransactions.FindAsync(id);
        if (recurring is null)
            return NotFound(new { message = "반복 지출을 찾을 수 없습니다." });

        // 연결된 거래는 SetNull이므로 그냥 삭제
        _db.RecurringTransactions.Remove(recurring);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// 특정 월의 반복 지출을 자동 반영합니다 (on-demand 방식).
    /// 월별 요약 API에서 내부적으로 호출됩니다.
    /// </summary>
    [NonAction]
    public async Task ApplyRecurringTransactionsAsync(int year, int month)
    {
        var settings = await _db.UserSettings.FirstOrDefaultAsync();
        int monthStartDay = settings?.MonthStartDay ?? 1;

        var (periodStart, periodEnd) = DateRangeHelper.GetMonthRange(year, month, monthStartDay);

        // 활성화된 반복 지출 조회
        var recurringList = await _db.RecurringTransactions
            .Include(r => r.PaymentMethod)
                .ThenInclude(p => p.PointBudget)
            .Where(r => r.IsActive)
            .ToListAsync();

        foreach (var recurring in recurringList)
        {
            // 이 월의 실제 거래 날짜 계산
            DateTime transactionDate = DateRangeHelper.GetTransactionDate(year, month, recurring.DayOfMonth);

            // 해당 날짜가 이 월의 기간 내에 있는지 확인
            if (transactionDate < periodStart || transactionDate > periodEnd)
                continue;

            // 이미 이 월에 이 반복 지출로 생성된 거래가 있는지 확인 (멱등성)
            bool alreadyCreated = await _db.Transactions.AnyAsync(t =>
                t.RecurringTransactionId == recurring.Id &&
                t.Date.Year == transactionDate.Year &&
                t.Date.Month == transactionDate.Month);

            if (alreadyCreated)
                continue;

            // 포인트 잔액 확인
            if (recurring.PaymentMethod.Type == PaymentMethodType.Point
                && recurring.PaymentMethod.PointBudget is not null)
            {
                if (recurring.PaymentMethod.PointBudget.RemainingAmount < recurring.Amount)
                    continue; // 잔액 부족 시 스킵

                recurring.PaymentMethod.PointBudget.RemainingAmount -= recurring.Amount;
            }

            // 새 거래 생성
            var transaction = new Transaction
            {
                Amount = recurring.Amount,
                Date = transactionDate,
                Memo = recurring.Memo,
                Type = TransactionType.Expense,
                CategoryId = recurring.CategoryId,
                PaymentMethodId = recurring.PaymentMethodId,
                IsIncludedInTotal = true,
                RecurringTransactionId = recurring.Id
            };

            _db.Transactions.Add(transaction);

            // 할부 처리: 남은 횟수 차감
            if (recurring.Type == RecurringType.Installment && recurring.RemainingInstallments.HasValue)
            {
                recurring.RemainingInstallments--;
                if (recurring.RemainingInstallments <= 0)
                    recurring.IsActive = false;
            }
        }

        await _db.SaveChangesAsync();
    }
}
