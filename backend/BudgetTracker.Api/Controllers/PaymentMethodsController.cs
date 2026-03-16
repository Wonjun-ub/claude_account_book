using BudgetTracker.Api.DTOs.Requests;
using BudgetTracker.Api.DTOs.Responses;
using BudgetTracker.Api.Helpers;
using BudgetTracker.Api.Models.Enums;
using BudgetTracker.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BudgetTracker.Api.Controllers;

[ApiController]
[Route("api/payment-methods")]
public class PaymentMethodsController : ControllerBase
{
    private readonly IPaymentMethodService _service;

    public PaymentMethodsController(IPaymentMethodService service)
    {
        _service = service;
    }

    // GET /api/payment-methods?type=Card
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PaymentMethodResponse>>> GetAll([FromQuery] PaymentMethodType? type)
    {
        var result = await _service.GetAllAsync(type);

        return Ok(result);
    }

    // POST /api/payment-methods
    [HttpPost]
    public async Task<ActionResult<PaymentMethodResponse>> Create([FromBody] CreatePaymentMethodRequest request)
    {
        var (response, error) = await _service.CreateAsync(request);

        if (error is not null)
            return BadRequest(new { code = error, message = ErrorMessages.GetMessage(error) });

        return CreatedAtAction(nameof(GetAll), new { id = response!.Id }, response);
    }

    // PUT /api/payment-methods/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult<PaymentMethodResponse>> Update(int id, [FromBody] UpdatePaymentMethodRequest request)
    {
        var (response, error) = await _service.UpdateAsync(id, request);

        if (error == "NOT_FOUND")
            return NotFound(new { code = error, message = ErrorMessages.GetMessage(error) });

        if (error is not null)
            return BadRequest(new { code = error, message = ErrorMessages.GetMessage(error) });

        return Ok(response);
    }

    // DELETE /api/payment-methods/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var error = await _service.DeleteAsync(id);

        if (error == "NOT_FOUND")
            return NotFound(new { code = error, message = ErrorMessages.GetMessage(error) });

        if (error is not null)
            return BadRequest(new { code = error, message = ErrorMessages.GetMessage(error) });

        return NoContent();
    }
}
