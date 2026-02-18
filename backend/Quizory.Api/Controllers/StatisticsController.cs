using Microsoft.AspNetCore.Mvc;
using Quizory.Api.Domain;
using Quizory.Api.Dtos;
using Quizory.Api.Services;

namespace Quizory.Api.Controllers;

[ApiController]
[Route("api/statistics")]
public class StatisticsController(IRequestContextAccessor context, IStatisticsService stats, IOrgAuthorizationService auth) : ControllerBase
{
    [HttpGet("quizzes")]
    public async Task<IActionResult> GetQuizSummaries([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] Guid? leagueId, [FromQuery] Guid? teamId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        auth.EnsureAtLeast(OrganizationRole.User);
        var ctx = context.Get();
        var (items, total) = await stats.GetQuizSummariesAsync(ctx.OrganizationId, from, to, leagueId, teamId, page, pageSize);
        return Ok(new PaginatedResponse<QuizSummaryDto>(items, total, page, pageSize));
    }

    [HttpGet("leagues/{leagueId:guid}")]
    public async Task<IActionResult> GetLeagueSummary(Guid leagueId)
    {
        auth.EnsureAtLeast(OrganizationRole.User);
        var ctx = context.Get();
        var summary = await stats.GetLeagueSummaryAsync(ctx.OrganizationId, leagueId);
        if (summary == null) return NotFound();
        return Ok(summary);
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategoryStats([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] Guid? leagueId)
    {
        auth.EnsureAtLeast(OrganizationRole.User);
        var ctx = context.Get();
        var items = await stats.GetCategoryStatsAsync(ctx.OrganizationId, from, to, leagueId);
        return Ok(items);
    }

    [HttpGet("teams/{teamId:guid}/history")]
    public async Task<IActionResult> GetTeamHistory(Guid teamId, [FromQuery] Guid? leagueId, [FromQuery] int limit = 20)
    {
        auth.EnsureAtLeast(OrganizationRole.User);
        var ctx = context.Get();
        var items = await stats.GetTeamHistoryAsync(ctx.OrganizationId, teamId, leagueId, limit);
        return Ok(items);
    }
}
