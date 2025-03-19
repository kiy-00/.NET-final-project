using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixelPerfect.Core.Models;
using PixelPerfect.Services.Services;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PixelPerfect.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<PaymentResponse>> CreatePayment(CreatePaymentRequest request)
    {
        try
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var payment = await _paymentService.CreatePayment(userId, request);
            return Ok(payment);
        }
        catch (System.Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<PaymentResponse>> GetPayment(int id)
    {
        var payment = await _paymentService.GetPaymentById(id);
        if (payment == null)
        {
            return NotFound();
        }
        return Ok(payment);
    }

    [HttpGet("user")]
    [Authorize]
    public async Task<ActionResult<PaymentResponse>> GetUserPayments()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var payments = await _paymentService.GetPaymentsByUserId(userId);
        return Ok(payments);
    }

    [HttpGet("order/{orderType}/{orderId}")]
    [Authorize]
    public async Task<ActionResult<PaymentResponse>> GetOrderPayments(string orderType, int orderId)
    {
        var payments = await _paymentService.GetPaymentsByOrder(orderType, orderId);
        return Ok(payments);
    }

    [HttpPut("{id}/status")]
    [Authorize] // 在实际应用中，这可能需要管理员权限或支付回调
    public async Task<ActionResult<PaymentResponse>> UpdatePaymentStatus(int id, PaymentStatusUpdateRequest request)
    {
        try
        {
            var payment = await _paymentService.UpdatePaymentStatus(id, request);
            return Ok(payment);
        }
        catch (System.Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}