using Microsoft.AspNetCore.Mvc;
using Quizory.Api.Domain;
using Quizory.Api.Dtos;
using Quizory.Api.Services;

namespace Quizory.Api.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentController(IPaymentService paymentService, IRequestContextAccessor context, IOrgAuthorizationService auth) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest request)
    {
        auth.EnsureAtLeast(OrganizationRole.Owner);
        var ctx = context.Get();
        var dto = await paymentService.CreatePaymentAsync(ctx.OrganizationId, request);
        return Ok(dto);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPayment(Guid id)
    {
        auth.EnsureAtLeast(OrganizationRole.Owner);
        var ctx = context.Get();
        var dto = await paymentService.GetPaymentAsync(id, ctx.OrganizationId);
        if (dto == null) return NotFound();
        return Ok(dto);
    }

    [HttpGet]
    public async Task<IActionResult> ListPayments([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        auth.EnsureAtLeast(OrganizationRole.Owner);
        var ctx = context.Get();
        var (items, total) = await paymentService.ListPaymentsAsync(ctx.OrganizationId, page, pageSize);
        return Ok(new PaginatedResponse<PaymentDto>(items, total, page, pageSize));
    }

    [HttpPost("confirm")]
    public async Task<IActionResult> ConfirmPayment([FromBody] ConfirmPaymentRequest request)
    {
        auth.EnsureAtLeast(OrganizationRole.Owner);
        var ctx = context.Get();
        var dto = await paymentService.ConfirmPaymentAsync(ctx.OrganizationId, null, request.ExternalPaymentId);
        if (dto == null) return NotFound();
        return Ok(dto);
    }

    [HttpPost("{id:guid}/confirm")]
    public async Task<IActionResult> ConfirmPaymentById(Guid id)
    {
        auth.EnsureAtLeast(OrganizationRole.Owner);
        var ctx = context.Get();
        var dto = await paymentService.ConfirmPaymentAsync(ctx.OrganizationId, id, null);
        if (dto == null) return NotFound();
        return Ok(dto);
    }
}
