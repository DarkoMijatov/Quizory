using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quizory.Api.Data;
using Quizory.Api.Domain;
using Quizory.Api.Dtos;
using Quizory.Api.Services;

namespace Quizory.Api.Controllers;

[ApiController]
[Route("api/leagues")]
public class LeaguesController(AppDbContext db, IRequestContextAccessor context, IOrgAuthorizationService auth, ISubscriptionService subscriptions) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        auth.EnsureAtLeast(OrganizationRole.User);
        subscriptions.EnforceFeature("leagues");
        var ctx = context.Get();
        var query = db.Leagues.Where(l => l.OrganizationId == ctx.OrganizationId);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(l => l.Name.Contains(search));
        var total = await query.CountAsync();
        var items = await query.OrderBy(l => l.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new PaginatedResponse<League>(items, total, page, pageSize));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        auth.EnsureAtLeast(OrganizationRole.User);
        subscriptions.EnforceFeature("leagues");
        var ctx = context.Get();
        var league = await db.Leagues.FirstOrDefaultAsync(l => l.Id == id && l.OrganizationId == ctx.OrganizationId);
        if (league == null) return NotFound();
        return Ok(league);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLeagueRequest request)
    {
        auth.EnsureAtLeast(OrganizationRole.Admin);
        subscriptions.EnforceFeature("leagues");
        var ctx = context.Get();
        var league = new League { OrganizationId = ctx.OrganizationId, Name = request.Name };
        db.Leagues.Add(league);
        await db.SaveChangesAsync();
        return Ok(league);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLeagueRequest request)
    {
        auth.EnsureAtLeast(OrganizationRole.Admin);
        subscriptions.EnforceFeature("leagues");
        var ctx = context.Get();
        var league = await db.Leagues.IgnoreQueryFilters().FirstOrDefaultAsync(l => l.Id == id && l.OrganizationId == ctx.OrganizationId);
        if (league == null) return NotFound();
        league.Name = request.Name;
        await db.SaveChangesAsync();
        return Ok(league);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDelete(Guid id)
    {
        auth.EnsureAtLeast(OrganizationRole.Admin);
        subscriptions.EnforceFeature("leagues");
        var ctx = context.Get();
        var league = await db.Leagues.IgnoreQueryFilters().FirstOrDefaultAsync(l => l.Id == id && l.OrganizationId == ctx.OrganizationId);
        if (league == null) return NotFound();
        league.IsDeleted = true;
        await db.SaveChangesAsync();
        return NoContent();
    }
}
