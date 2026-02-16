using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quizory.Api.Data;
using Quizory.Api.Domain;
using Quizory.Api.Dtos;
using Quizory.Api.Services;

namespace Quizory.Api.Controllers;

[ApiController]
[Route("api/help-types")]
public class HelpTypesController(AppDbContext db, IRequestContextAccessor context, IOrgAuthorizationService auth) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        auth.EnsureAtLeast(OrganizationRole.User);
        var ctx = context.Get();
        var items = await db.HelpTypes.Where(h => h.OrganizationId == ctx.OrganizationId).OrderBy(h => h.Name).ToListAsync();
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateHelpTypeRequest request)
    {
        auth.EnsureAtLeast(OrganizationRole.Admin);
        var ctx = context.Get();
        var behavior = Enum.Parse<HelpBehavior>(request.Behavior.Replace(" ", ""), true);
        var help = new HelpType { OrganizationId = ctx.OrganizationId, Name = request.Name, Behavior = behavior };
        db.HelpTypes.Add(help);
        await db.SaveChangesAsync();
        return Ok(help);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateHelpTypeRequest request)
    {
        auth.EnsureAtLeast(OrganizationRole.Admin);
        var ctx = context.Get();
        var help = await db.HelpTypes.IgnoreQueryFilters().FirstOrDefaultAsync(h => h.Id == id && h.OrganizationId == ctx.OrganizationId);
        if (help == null) return NotFound();
        if (request.Name != null) help.Name = request.Name;
        if (request.Behavior != null) help.Behavior = Enum.Parse<HelpBehavior>(request.Behavior.Replace(" ", ""), true);
        await db.SaveChangesAsync();
        return Ok(help);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDelete(Guid id)
    {
        auth.EnsureAtLeast(OrganizationRole.Admin);
        var ctx = context.Get();
        var help = await db.HelpTypes.IgnoreQueryFilters().FirstOrDefaultAsync(h => h.Id == id && h.OrganizationId == ctx.OrganizationId);
        if (help == null) return NotFound();
        help.IsDeleted = true;
        await db.SaveChangesAsync();
        return NoContent();
    }
}
