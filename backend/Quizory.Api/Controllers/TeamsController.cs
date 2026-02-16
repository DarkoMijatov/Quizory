using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quizory.Api.Data;
using Quizory.Api.Domain;
using Quizory.Api.Dtos;
using Quizory.Api.Services;

namespace Quizory.Api.Controllers;

[ApiController]
[Route("api/teams")]
public class TeamsController(AppDbContext db, IRequestContextAccessor context, IOrgAuthorizationService auth) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? query, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        auth.EnsureAtLeast(OrganizationRole.User);
        var ctx = context.Get();
        var teams = db.Teams.Where(x => x.OrganizationId == ctx.OrganizationId);
        if (!string.IsNullOrWhiteSpace(query))
        {
            var aliasTeamIds = db.TeamAliases.Where(x => x.OrganizationId == ctx.OrganizationId && x.Alias.Contains(query)).Select(x => x.TeamId);
            teams = teams.Where(t => t.Name.Contains(query) || aliasTeamIds.Contains(t.Id));
        }
        var total = await teams.CountAsync();
        var result = await teams.Include(x => x.Aliases).OrderBy(t => t.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new PaginatedResponse<Team>(result, total, page, pageSize));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        auth.EnsureAtLeast(OrganizationRole.User);
        var ctx = context.Get();
        var team = await db.Teams.Include(x => x.Aliases).FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == ctx.OrganizationId);
        if (team == null) return NotFound();
        return Ok(team);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTeamRequest request)
    {
        auth.EnsureAtLeast(OrganizationRole.Admin);
        var ctx = context.Get();
        var team = new Team { OrganizationId = ctx.OrganizationId, Name = request.Name };
        db.Teams.Add(team);
        await db.SaveChangesAsync();
        return Ok(team);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTeamRequest request)
    {
        auth.EnsureAtLeast(OrganizationRole.Admin);
        var ctx = context.Get();
        var team = await db.Teams.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == ctx.OrganizationId);
        if (team == null) return NotFound();
        team.Name = request.Name;
        await db.SaveChangesAsync();
        return Ok(team);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDelete(Guid id)
    {
        auth.EnsureAtLeast(OrganizationRole.Admin);
        var ctx = context.Get();
        var team = await db.Teams.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == ctx.OrganizationId);
        if (team == null) return NotFound();
        team.IsDeleted = true;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:guid}/aliases")]
    public async Task<IActionResult> AddAlias(Guid id, [FromBody] AddTeamAliasRequest request)
    {
        auth.EnsureAtLeast(OrganizationRole.Admin);
        var ctx = context.Get();
        var team = await db.Teams.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == ctx.OrganizationId);
        if (team == null) return NotFound();
        var alias = new TeamAlias { OrganizationId = ctx.OrganizationId, TeamId = id, QuizId = request.QuizId, Alias = request.Alias };
        db.TeamAliases.Add(alias);
        await db.SaveChangesAsync();
        return Ok(alias);
    }
}
