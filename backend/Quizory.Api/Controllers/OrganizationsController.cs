using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quizory.Api.Data;
using Quizory.Api.Domain;
using Quizory.Api.Dtos;
using Quizory.Api.Services;

namespace Quizory.Api.Controllers;

[ApiController]
[Route("api/organizations")]
public class OrganizationsController(AppDbContext db, IRequestContextAccessor context, IAuthorizationService auth, ISubscriptionService subscriptions) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrent()
    {
        var ctx = context.Get();
        var org = await db.Organizations.FindAsync(ctx.OrganizationId);
        var members = await db.Memberships.Where(x => x.OrganizationId == ctx.OrganizationId).ToListAsync();
        return Ok(new { org, members });
    }

    [HttpPost("members/invite")]
    public async Task<IActionResult> InviteMember([FromBody] InviteMemberRequest request)
    {
        auth.EnsureAtLeast(OrganizationRole.Admin);
        subscriptions.EnforceFeature("members");

        var ctx = context.Get();
        var role = Enum.Parse<OrganizationRole>(request.Role, true);
        auth.EnsureAdminCap(ctx.OrganizationId, role);

        var user = new User { Email = request.Email, DisplayName = request.DisplayName, PasswordHash = "pending", IsEmailVerified = false };
        db.Users.Add(user);
        db.Memberships.Add(new Membership { UserId = user.Id, OrganizationId = ctx.OrganizationId, Role = role });
        await db.SaveChangesAsync();
        return Ok(user);
    }

    [HttpPost("language")]
    public async Task<IActionResult> SetPreferredLanguage([FromBody] OrganizationLanguageRequest request)
    {
        var ctx = context.Get();
        var user = await db.Users.FindAsync(ctx.UserId);
        if (user is null) return NotFound();
        user.PreferredLanguage = request.PreferredLanguage;
        await db.SaveChangesAsync();
        return NoContent();
    }
}
