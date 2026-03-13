using BudgetTracker.Api.Data;
using BudgetTracker.Api.DTOs.Requests;
using BudgetTracker.Api.DTOs.Responses;
using BudgetTracker.Api.Models.Entities;
using BudgetTracker.Api.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.Api.Controllers;

[ApiController]
[Route("api/payment-methods")]
public class PaymentMethodsController : ControllerBase
{
    private readonly BudgetTrackerDbContext _db;

    public PaymentMethodsController(BudgetTrackerDbContext db)
    {
        _db = db;
    }

    // GET /api/payment-methods?type=Card
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PaymentMethodResponse>>> GetAll([FromQuery] PaymentMethodType? type)
    {
        var query = _db.PaymentMethods
            .Include(p => p.PointBudget)
            .AsQueryable();

        if (type.HasValue)
            query = query.Where(p => p.Type == type.Value);

        var methods = await query
            .OrderBy(p => p.Type)
            .ThenBy(p => p.Name)
            .Select(p => new PaymentMethodResponse
            {
                Id = p.Id,
                Name = p.Name,
                Type = p.Type.ToString(),
                IsDefault = p.IsDefault,
                PointBudgetId = p.PointBudgetId,
                PointBudget = p.PointBudget == null ? null : new PointBudgetResponse
                {
                    Id = p.PointBudget.Id,
                    Name = p.PointBudget.Name,
                    TotalAmount = p.PointBudget.TotalAmount,
                    RemainingAmount = p.PointBudget.RemainingAmount
                }
            })
            .ToListAsync();

        return Ok(methods);
    }

    // POST /api/payment-methods
    [HttpPost]
    public async Task<ActionResult<PaymentMethodResponse>> Create([FromBody] CreatePaymentMethodRequest request)
    {
        // Point 타입이면 PointBudgetId 필수
        if (request.Type == PaymentMethodType.Point && request.PointBudgetId is null)
            return BadRequest(new { message = "포인트 결제수단 생성 시 PointBudgetId가 필요합니다." });

        // PointBudgetId 유효성 확인
        if (request.PointBudgetId.HasValue)
        {
            bool budgetExists = await _db.PointBudgets.AnyAsync(p => p.Id == request.PointBudgetId.Value);
            if (!budgetExists)
                return BadRequest(new { message = "존재하지 않는 포인트 예산입니다." });

            // 이미 연결된 결제수단이 있는지 확인
            bool alreadyLinked = await _db.PaymentMethods.AnyAsync(p => p.PointBudgetId == request.PointBudgetId.Value);
            if (alreadyLinked)
                return BadRequest(new { message = "이미 다른 결제수단에 연결된 포인트 예산입니다." });
        }

        var method = new PaymentMethod
        {
            Name = request.Name,
            Type = request.Type,
            IsDefault = false,
            PointBudgetId = request.Type == PaymentMethodType.Point ? request.PointBudgetId : null
        };

        _db.PaymentMethods.Add(method);
        await _db.SaveChangesAsync();

        // 포인트 예산 정보 로드
        await _db.Entry(method).Reference(m => m.PointBudget).LoadAsync();

        return CreatedAtAction(nameof(GetAll), new { id = method.Id }, new PaymentMethodResponse
        {
            Id = method.Id,
            Name = method.Name,
            Type = method.Type.ToString(),
            IsDefault = method.IsDefault,
            PointBudgetId = method.PointBudgetId,
            PointBudget = method.PointBudget == null ? null : new PointBudgetResponse
            {
                Id = method.PointBudget.Id,
                Name = method.PointBudget.Name,
                TotalAmount = method.PointBudget.TotalAmount,
                RemainingAmount = method.PointBudget.RemainingAmount
            }
        });
    }

    // PUT /api/payment-methods/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult<PaymentMethodResponse>> Update(int id, [FromBody] UpdatePaymentMethodRequest request)
    {
        var method = await _db.PaymentMethods
            .Include(p => p.PointBudget)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (method is null)
            return NotFound(new { message = "결제수단을 찾을 수 없습니다." });

        method.Name = request.Name;
        method.Type = request.Type;
        method.PointBudgetId = request.Type == PaymentMethodType.Point ? request.PointBudgetId : null;

        await _db.SaveChangesAsync();

        await _db.Entry(method).Reference(m => m.PointBudget).LoadAsync();

        return Ok(new PaymentMethodResponse
        {
            Id = method.Id,
            Name = method.Name,
            Type = method.Type.ToString(),
            IsDefault = method.IsDefault,
            PointBudgetId = method.PointBudgetId,
            PointBudget = method.PointBudget == null ? null : new PointBudgetResponse
            {
                Id = method.PointBudget.Id,
                Name = method.PointBudget.Name,
                TotalAmount = method.PointBudget.TotalAmount,
                RemainingAmount = method.PointBudget.RemainingAmount
            }
        });
    }

    // DELETE /api/payment-methods/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var method = await _db.PaymentMethods.FindAsync(id);
        if (method is null)
            return NotFound(new { message = "결제수단을 찾을 수 없습니다." });

        // 기본 결제수단 삭제 불가
        if (method.IsDefault)
            return BadRequest(new { message = "기본 결제수단은 삭제할 수 없습니다." });

        // 연결된 거래가 있으면 삭제 불가
        bool hasTransactions = await _db.Transactions.AnyAsync(t => t.PaymentMethodId == id);
        if (hasTransactions)
            return BadRequest(new { message = "이 결제수단에 연결된 거래가 있어 삭제할 수 없습니다." });

        _db.PaymentMethods.Remove(method);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
