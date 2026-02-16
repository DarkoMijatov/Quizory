using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quizory.Api.Dtos;
using Quizory.Api.Services;

namespace Quizory.Api.Controllers;

[ApiController]
[Route("api/share")]
public class ShareController(IShareService share, IRequestContextAccessor context, ISubscriptionService subscriptions) : ControllerBase
{
    [HttpPost("token")]
    [Authorize]
    public async Task<IActionResult> CreateToken([FromBody] CreateShareTokenRequest request)
    {
        subscriptions.EnforceFeature("share");
        var ctx = context.Get();
        var result = await share.CreateTokenAsync(request.QuizId, request.ExpiresAtUtc);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("leaderboard/{token}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetLeaderboard(string token)
    {
        var leaderboard = await share.GetByTokenAsync(token);
        if (leaderboard == null) return NotFound();
        return Ok(leaderboard);
    }
}
