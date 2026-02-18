using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quizory.Api.Data;
using Quizory.Api.Domain;
using Quizory.Api.Dtos;
using Quizory.Api.Services;

namespace Quizory.Api.Controllers;


[ApiController]
[Route("api/organizations")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]

public class OrganizationsController(AppDbContext db, IRequestContextAccessor context, IOrgAuthorizationService auth, ISubscriptionService subscriptions) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrent()
    {
        var ctx = context.Get();
        var org = await db.Organizations.FindAsync(ctx.OrganizationId);
        var members = await db.Memberships.Where(x => x.OrganizationId == ctx.OrganizationId).ToListAsync();
        var userIds = members.Select(m => m.UserId).Distinct().ToList();
        var users = await db.Users.Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id);
        var list = members.Select(m => new { m.Id, m.UserId, m.Role, m.CreatedAtUtc, User = users.GetValueOrDefault(m.UserId) }).ToList();
        return Ok(new { org, members = list });
    }

    [HttpGet("members")]
    public async Task<IActionResult> GetMembers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        auth.EnsureAtLeast(OrganizationRole.Admin);
        var ctx = context.Get();
        var query = db.Memberships.Where(m => m.OrganizationId == ctx.OrganizationId);
        var total = await query.CountAsync();
        var members = await query.OrderBy(m => m.Role).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        var userIds = members.Select(m => m.UserId).Distinct().ToList();
        var users = await db.Users.Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id);
        var items = members.Select(m => new { m.Id, m.UserId, m.Role, m.CreatedAtUtc, User = users.GetValueOrDefault(m.UserId) }).ToList();
        return Ok(new PaginatedResponse<object>(items, total, page, pageSize));
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateOrganization([FromBody] UpdateOrganizationRequest request)
    {
        auth.EnsureAtLeast(OrganizationRole.Owner);
        var ctx = context.Get();
        var org = await db.Organizations.FindAsync(ctx.OrganizationId);
        if (org == null) return NotFound();
        if (request.Name != null) org.Name = request.Name;
        if (request.PrimaryColor != null) org.PrimaryColor = request.PrimaryColor;
        org.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(org);
    }

    [HttpPost("members/invite")]
    public async Task<IActionResult> InviteMember([FromBody] InviteMemberRequest request)
    {
        auth.EnsureAtLeast(OrganizationRole.Admin);
        subscriptions.EnforceFeature("members");
        subscriptions.EnforceMemberLimit();

        var ctx = context.Get();
        var role = Enum.Parse<OrganizationRole>(request.Role, true);
        auth.EnsureAdminCap(ctx.OrganizationId, role);

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null)
        {
            user = new User { Email = request.Email, DisplayName = request.DisplayName ?? request.Email, PasswordHash = "pending", IsEmailVerified = false };
            db.Users.Add(user);
        }
        if (await db.Memberships.AnyAsync(m => m.UserId == user.Id && m.OrganizationId == ctx.OrganizationId))
            return BadRequest(new { errorCode = "AlreadyMember" });
        db.Memberships.Add(new Membership { UserId = user.Id, OrganizationId = ctx.OrganizationId, Role = role });
        await db.SaveChangesAsync();
        return Ok(new { user.Id, user.Email, user.DisplayName, Role = role.ToString() });
    }

    [HttpPut("members/{userId:guid}/role")]
    public async Task<IActionResult> UpdateMemberRole(Guid userId, [FromBody] UpdateMemberRoleRequest request)
    {
        auth.EnsureAtLeast(OrganizationRole.Owner);
        var ctx = context.Get();
        var membership = await db.Memberships.FirstOrDefaultAsync(m => m.UserId == userId && m.OrganizationId == ctx.OrganizationId);
        if (membership == null) return NotFound();
        if (membership.Role == OrganizationRole.Owner) return BadRequest(new { errorCode = "CannotChangeOwnerRole" });
        var newRole = Enum.Parse<OrganizationRole>(request.Role, true);
        if (newRole == OrganizationRole.Owner) return BadRequest(new { errorCode = "CannotAssignOwner" });
        auth.EnsureAdminCap(ctx.OrganizationId, newRole);
        membership.Role = newRole;
        await db.SaveChangesAsync();
        return Ok(membership);
    }

    [HttpDelete("members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid userId)
    {
        auth.EnsureAtLeast(OrganizationRole.Admin);
        var ctx = context.Get();
        var membership = await db.Memberships.FirstOrDefaultAsync(m => m.UserId == userId && m.OrganizationId == ctx.OrganizationId);
        if (membership == null) return NotFound();
        if (membership.Role == OrganizationRole.Owner) return BadRequest(new { errorCode = "CannotRemoveOwner" });
        var current = context.Get().Role;
        if (current == OrganizationRole.Admin && membership.Role == OrganizationRole.Admin)
            return BadRequest(new { errorCode = "AdminCannotRemoveAdmin" });
        db.Memberships.Remove(membership);
        await db.SaveChangesAsync();
        return NoContent();
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
