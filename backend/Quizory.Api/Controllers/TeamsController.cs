using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quizory.Api.Data;
using Quizory.Api.Domain;
using Quizory.Api.Services;

namespace Quizory.Api.Controllers;

[ApiController]
[Route("api/teams")]
public class TeamsController(AppDbContext db, IRequestContextAccessor context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? query)
    {
        var ctx = context.Get();
        var teams = db.Teams.Where(x => x.OrganizationId == ctx.OrganizationId && !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(query))
        {
            var aliasTeamIds = db.TeamAliases.Where(x => x.OrganizationId == ctx.OrganizationId && x.Alias.Contains(query)).Select(x => x.TeamId);
            teams = teams.Where(t => t.Name.Contains(query) || aliasTeamIds.Contains(t.Id));
        }

        var result = await teams.Include(x => x.Aliases).ToListAsync();
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Team request)
    {
        var ctx = context.Get();
        request.OrganizationId = ctx.OrganizationId;
        db.Teams.Add(request);
        await db.SaveChangesAsync();
        return Ok(request);
    }
}
