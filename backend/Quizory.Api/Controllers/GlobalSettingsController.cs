using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quizory.Api.Data;
using Quizory.Api.Domain;
using Quizory.Api.Dtos;
using Quizory.Api.Services;

namespace Quizory.Api.Controllers;

[ApiController]
[Route("api/global-settings")]
public class GlobalSettingsController(AppDbContext db, IRequestContextAccessor context, IOrgAuthorizationService auth) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        auth.EnsureAtLeast(OrganizationRole.User);
        var ctx = context.Get();
        var settings = await db.GlobalSettings.FirstOrDefaultAsync(s => s.OrganizationId == ctx.OrganizationId);
        if (settings == null)
        {
            settings = new GlobalSettings { OrganizationId = ctx.OrganizationId };
            db.GlobalSettings.Add(settings);
            await db.SaveChangesAsync();
        }
        return Ok(settings);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateGlobalSettingsRequest request)
    {
        auth.EnsureAtLeast(OrganizationRole.Admin);
        var ctx = context.Get();
        var settings = await db.GlobalSettings.FirstOrDefaultAsync(s => s.OrganizationId == ctx.OrganizationId);
        if (settings == null)
        {
            settings = new GlobalSettings { OrganizationId = ctx.OrganizationId };
            db.GlobalSettings.Add(settings);
        }
        if (request.DefaultCategoriesCount.HasValue) settings.DefaultCategoriesCount = request.DefaultCategoriesCount.Value;
        if (request.DefaultQuestionsPerCategory.HasValue) settings.DefaultQuestionsPerCategory = request.DefaultQuestionsPerCategory.Value;
        settings.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(settings);
    }
}
