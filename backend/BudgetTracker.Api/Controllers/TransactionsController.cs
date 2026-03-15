using BudgetTracker.Api.Data;
using BudgetTracker.Api.DTOs.Requests;
using BudgetTracker.Api.DTOs.Responses;
using BudgetTracker.Api.Models.Entities;
using BudgetTracker.Api.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.Api.Controllers;

[ApiController]
[Route("api/transactions")]
public class TransactionsController : ControllerBase
{
    private readonly BudgetTrackerDbContext _db;

    public TransactionsController(BudgetTrackerDbContext db)
    {
        _db = db;
    }

    // GET /api/transactions — 목록 조회 + 검색/필터 (T10 통합)
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TransactionResponse>>> GetAll(
        [FromQuery] int? categoryId,
        [FromQuery] int? paymentMethodId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? keyword,
        [FromQuery] decimal? minAmount,
        [FromQuery] decimal? maxAmount,
        [FromQuery] int? year,
        [FromQuery] int? month)
    {
        var query = _db.Transactions
            .Include(t => t.Category)
            .Include(t => t.PaymentMethod)
            .AsQueryable();

        // 날짜 범위 기본값: 현재 월 (파라미터 없을 때)
        if (from.HasValue)
        {
            query = query.Where(t => t.Date >= from.Value);
        }
        else if (!to.HasValue && !year.HasValue)
        {
            // 기본: 이번 달 1일부터
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            query = query.Where(t => t.Date >= startOfMonth);
        }

        if (to.HasValue)
            query = query.Where(t => t.Date <= to.Value);

        // year/month 파라미터 (달력 기반 필터)
        if (year.HasValue && month.HasValue)
        {
            var startOfMonth = new DateTime(year.Value, month.Value, 1, 0, 0, 0, DateTimeKind.Utc);
            var endOfMonth = startOfMonth.AddMonths(1).AddTicks(-1);
            query = query.Where(t => t.Date >= startOfMonth && t.Date <= endOfMonth);
        }

        // 카테고리 필터
        if (categoryId.HasValue)
            query = query.Where(t => t.CategoryId == categoryId.Value);

        // 결제수단 필터
        if (paymentMethodId.HasValue)
            query = query.Where(t => t.PaymentMethodId == paymentMethodId.Value);

        // 금액 범위 필터
        if (minAmount.HasValue)
            query = query.Where(t => t.Amount >= minAmount.Value);
        if (maxAmount.HasValue)
            query = query.Where(t => t.Amount <= maxAmount.Value);

        // 키워드 검색 (메모, 대소문자 무시 — PostgreSQL ILike)
        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(t => t.Memo != null && EF.Functions.ILike(t.Memo, $"%{keyword}%"));

        var transactions = await query
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Id)
            .Select(t => new TransactionResponse
            {
                Id = t.Id,
                Amount = t.Amount,
                Date = t.Date,
                Memo = t.Memo,
                Type = t.Type.ToString(),
                CategoryId = t.CategoryId,
                CategoryName = t.Category.Name,
                PaymentMethodId = t.PaymentMethodId,
                PaymentMethodName = t.PaymentMethod.Name,
                IsIncludedInTotal = t.IsIncludedInTotal,
                RecurringTransactionId = t.RecurringTransactionId
            })
            .ToListAsync();

        return Ok(transactions);
    }

    // GET /api/transactions/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<TransactionResponse>> GetById(int id)
    {
        var transaction = await _db.Transactions
            .Include(t => t.Category)
            .Include(t => t.PaymentMethod)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (transaction is null)
            return NotFound(new { message = "거래를 찾을 수 없습니다." });

        return Ok(MapToResponse(transaction));
    }

    // POST /api/transactions
    [HttpPost]
    public async Task<ActionResult<TransactionResponse>> Create([FromBody] CreateTransactionRequest request)
    {
        // 카테고리 존재 확인
        bool categoryExists = await _db.Categories.AnyAsync(c => c.Id == request.CategoryId);
        if (!categoryExists)
            return BadRequest(new { message = "존재하지 않는 카테고리입니다." });

        // 결제수단 존재 확인 + 포인트 결제수단인 경우 잔액 확인
        var paymentMethod = await _db.PaymentMethods
            .Include(p => p.PointBudget)
            .FirstOrDefaultAsync(p => p.Id == request.PaymentMethodId);

        if (paymentMethod is null)
            return BadRequest(new { message = "존재하지 않는 결제수단입니다." });

        // 포인트 차감 처리
        if (paymentMethod.Type == PaymentMethodType.Point && paymentMethod.PointBudget is not null)
        {
            if (paymentMethod.PointBudget.RemainingAmount < request.Amount)
                return BadRequest(new { message = $"포인트 잔액이 부족합니다. (잔액: {paymentMethod.PointBudget.RemainingAmount:N0}원)" });

            paymentMethod.PointBudget.RemainingAmount -= request.Amount;
        }

        // 엔티티 생성
        var transaction = new Transaction
        {
            Amount = request.Amount,
            Date = request.Date.ToUniversalTime(),
            Memo = request.Memo,
            Type = request.Type,
            CategoryId = request.CategoryId,
            PaymentMethodId = request.PaymentMethodId,
            IsIncludedInTotal = request.IsIncludedInTotal
        };

        // DB 저장
        _db.Transactions.Add(transaction);
        await _db.SaveChangesAsync();

        // 탐색 속성 로드
        await _db.Entry(transaction).Reference(t => t.Category).LoadAsync();
        await _db.Entry(transaction).Reference(t => t.PaymentMethod).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = transaction.Id }, MapToResponse(transaction));
    }

    // PUT /api/transactions/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult<TransactionResponse>> Update(int id, [FromBody] UpdateTransactionRequest request)
    {
        // 거래 존재 확인
        var transaction = await _db.Transactions
            .Include(t => t.PaymentMethod)
                .ThenInclude(p => p.PointBudget)
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (transaction is null)
            return NotFound(new { message = "거래를 찾을 수 없습니다." });

        // 카테고리 존재 확인
        bool categoryExists = await _db.Categories.AnyAsync(c => c.Id == request.CategoryId);
        if (!categoryExists)
            return BadRequest(new { message = "존재하지 않는 카테고리입니다." });

        // 새 결제수단 로드
        var newPaymentMethod = await _db.PaymentMethods
            .Include(p => p.PointBudget)
            .FirstOrDefaultAsync(p => p.Id == request.PaymentMethodId);

        if (newPaymentMethod is null)
            return BadRequest(new { message = "존재하지 않는 결제수단입니다." });

        // 이전 포인트 복구
        var oldPaymentMethod = transaction.PaymentMethod;
        if (oldPaymentMethod.Type == PaymentMethodType.Point && oldPaymentMethod.PointBudget is not null)
        {
            oldPaymentMethod.PointBudget.RemainingAmount += transaction.Amount;
        }

        // 새 포인트 차감
        if (newPaymentMethod.Type == PaymentMethodType.Point && newPaymentMethod.PointBudget is not null)
        {
            if (newPaymentMethod.PointBudget.RemainingAmount < request.Amount)
                return BadRequest(new { message = $"포인트 잔액이 부족합니다. (잔액: {newPaymentMethod.PointBudget.RemainingAmount:N0}원)" });

            newPaymentMethod.PointBudget.RemainingAmount -= request.Amount;
        }

        // 엔티티 필드 업데이트
        transaction.Amount = request.Amount;
        transaction.Date = request.Date.ToUniversalTime();
        transaction.Memo = request.Memo;
        transaction.Type = request.Type;
        transaction.CategoryId = request.CategoryId;
        transaction.PaymentMethodId = request.PaymentMethodId;
        transaction.IsIncludedInTotal = request.IsIncludedInTotal;

        // DB 저장
        await _db.SaveChangesAsync();

        // 최신 탐색 속성 재로드
        await _db.Entry(transaction).Reference(t => t.Category).LoadAsync();
        await _db.Entry(transaction).Reference(t => t.PaymentMethod).LoadAsync();

        return Ok(MapToResponse(transaction));
    }

    // DELETE /api/transactions/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        // 거래 존재 확인
        var transaction = await _db.Transactions
            .Include(t => t.PaymentMethod)
                .ThenInclude(p => p.PointBudget)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (transaction is null)
            return NotFound(new { message = "거래를 찾을 수 없습니다." });

        // 포인트 결제였으면 잔액 복구
        if (transaction.PaymentMethod.Type == PaymentMethodType.Point
            && transaction.PaymentMethod.PointBudget is not null)
        {
            transaction.PaymentMethod.PointBudget.RemainingAmount += transaction.Amount;
        }

        // DB 삭제
        _db.Transactions.Remove(transaction);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private static TransactionResponse MapToResponse(Transaction t) => new()
    {
        Id = t.Id,
        Amount = t.Amount,
        Date = t.Date,
        Memo = t.Memo,
        Type = t.Type.ToString(),
        CategoryId = t.CategoryId,
        CategoryName = t.Category.Name,
        PaymentMethodId = t.PaymentMethodId,
        PaymentMethodName = t.PaymentMethod.Name,
        IsIncludedInTotal = t.IsIncludedInTotal,
        RecurringTransactionId = t.RecurringTransactionId
    };
}
