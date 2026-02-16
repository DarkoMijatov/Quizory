using Microsoft.AspNetCore.Mvc;
using Quizory.Api.Domain;
using Quizory.Api.Dtos;
using Quizory.Api.Services;

namespace Quizory.Api.Controllers;

[ApiController]
[Route("api/subscription")]
public class SubscriptionController(ISubscriptionService subscription, IRequestContextAccessor context, IOrgAuthorizationService auth) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCurrent()
    {
        auth.EnsureAtLeast(OrganizationRole.User);
        var ctx = context.Get();
        var dto = await subscription.GetCurrentSubscriptionAsync(ctx.OrganizationId);
        return Ok(dto);
    }

    [HttpPost("trial")]
    public async Task<IActionResult> StartTrial()
    {
        auth.EnsureAtLeast(OrganizationRole.Owner);
        var ctx = context.Get();
        await subscription.StartTrialAsync(ctx.OrganizationId);
        return NoContent();
    }

    [HttpPost("premium")]
    public async Task<IActionResult> SetPremium()
    {
        auth.EnsureAtLeast(OrganizationRole.Owner);
        var ctx = context.Get();
        await subscription.SetPremiumAsync(ctx.OrganizationId);
        return NoContent();
    }

    [HttpPost("downgrade")]
    public async Task<IActionResult> DowngradeToFree()
    {
        auth.EnsureAtLeast(OrganizationRole.Owner);
        var ctx = context.Get();
        await subscription.DowngradeToFreeAsync(ctx.OrganizationId);
        return NoContent();
    }
}
