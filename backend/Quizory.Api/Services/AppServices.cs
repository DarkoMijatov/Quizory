using Microsoft.EntityFrameworkCore;
using Quizory.Api.Data;
using Quizory.Api.Domain;

namespace Quizory.Api.Services;

public record RequestContext(Guid UserId, Guid OrganizationId, OrganizationRole Role, string Language);

public interface IRequestContextAccessor
{
    RequestContext Get();
}

public class JwtRequestContextAccessor(IHttpContextAccessor accessor, AppDbContext db) : IRequestContextAccessor
{
    public RequestContext Get()
    {
        var http = accessor.HttpContext ?? throw new InvalidOperationException("No HTTP context");
        var userIdClaim = http.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? http.User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("User not authenticated.");
        var orgHeader = http.Request.Headers["X-Organization-Id"].FirstOrDefault();
        Membership membership;
        if (!string.IsNullOrEmpty(orgHeader) && Guid.TryParse(orgHeader, out var orgId))
        {
            membership = db.Memberships.FirstOrDefault(x => x.UserId == userId && x.OrganizationId == orgId)
                ?? throw new UnauthorizedAccessException("User is not a member of the selected organization.");
        }
        else
        {
            membership = db.Memberships.Where(x => x.UserId == userId).OrderBy(x => x.Role == OrganizationRole.Owner ? 0 : 1).FirstOrDefault()
                ?? throw new UnauthorizedAccessException("User has no organization.");
        }
        var preferredLang = http.User.FindFirst("preferred_language")?.Value ?? "sr";
        var acceptLang = http.Request.Headers["Accept-Language"].ToString();
        var language = acceptLang.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? "en" : (preferredLang == "en" ? "en" : "sr");
        return new RequestContext(userId, membership.OrganizationId, membership.Role, language);
    }
}

public interface IOrgAuthorizationService
{
    void EnsureAtLeast(OrganizationRole role);
    void EnsureAdminCap(Guid organizationId, OrganizationRole targetRole);
}

public class AuthorizationService(IRequestContextAccessor context, AppDbContext db) : IOrgAuthorizationService
{
    public void EnsureAtLeast(OrganizationRole role)
    {
        var ranks = new Dictionary<OrganizationRole, int>
        {
            [OrganizationRole.User] = 1,
            [OrganizationRole.Admin] = 2,
            [OrganizationRole.Owner] = 3
        };
        var current = context.Get().Role;
        if (ranks[current] < ranks[role]) throw new UnauthorizedAccessException("Insufficient permissions.");
    }

    public void EnsureAdminCap(Guid organizationId, OrganizationRole targetRole)
    {
        if (targetRole is OrganizationRole.Admin or OrganizationRole.Owner)
        {
            var count = db.Memberships.Count(x => x.OrganizationId == organizationId && (x.Role == OrganizationRole.Owner || x.Role == OrganizationRole.Admin));
            if (count >= 3) throw new InvalidOperationException("Admin cap reached (max 3 admin-level accounts).");
        }
    }
}

public interface ISubscriptionService
{
    void EnforceFeature(string feature);
    void EnforceQuizMonthlyLimit();
    void EnforceMemberLimit();
}

public class SubscriptionService(IRequestContextAccessor context, AppDbContext db) : ISubscriptionService
{
    public void EnforceFeature(string feature)
    {
        var ctx = context.Get();
        var org = db.Organizations.Find(ctx.OrganizationId)!;
        if (org.SubscriptionPlan == SubscriptionPlan.Free && (feature is "leagues" or "questionBank" or "members" or "share"))
            throw new InvalidOperationException($"Feature '{feature}' requires premium.");
    }

    public void EnforceQuizMonthlyLimit()
    {
        var ctx = context.Get();
        var org = db.Organizations.Find(ctx.OrganizationId)!;
        if (org.SubscriptionPlan == SubscriptionPlan.Free)
        {
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var count = db.Quizzes.Count(q => q.OrganizationId == ctx.OrganizationId && !q.IsDeleted && q.CreatedAtUtc >= startOfMonth);
            if (count >= 10) throw new InvalidOperationException("Free plan monthly quiz limit reached.");
        }
    }

    public void EnforceMemberLimit()
    {
        var ctx = context.Get();
        var org = db.Organizations.Find(ctx.OrganizationId)!;
        if (org.SubscriptionPlan == SubscriptionPlan.Free)
        {
            var memberCount = db.Memberships.Count(m => m.OrganizationId == ctx.OrganizationId);
            if (memberCount >= 1) throw new InvalidOperationException("Free plan allows only the owner; add members requires premium.");
        }
    }
}

public interface IScoringService
{
    Task<Dictionary<Guid, int>> ComputeRankingAsync(Guid quizId, Guid organizationId);
}

public class ScoringService(AppDbContext db) : IScoringService
{
    public async Task<Dictionary<Guid, int>> ComputeRankingAsync(Guid quizId, Guid organizationId)
    {
        var entries = await db.ScoreEntries
            .Where(x => x.OrganizationId == organizationId && x.QuizId == quizId)
            .ToListAsync();
        var jokers = await db.HelpUsages.Where(x => x.OrganizationId == organizationId && x.QuizId == quizId)
            .Join(db.HelpTypes, u => u.HelpTypeId, h => h.Id, (u, h) => new { u.TeamId, h.Behavior })
            .Where(x => x.Behavior == HelpBehavior.DoubleScore)
            .Select(x => x.TeamId)
            .Distinct()
            .ToListAsync();

        return entries.GroupBy(x => x.TeamId).ToDictionary(
            g => g.Key,
            g =>
            {
                var baseTotal = g.Sum(x => x.Points + x.BonusPoints);
                return jokers.Contains(g.Key) ? baseTotal * 2 : baseTotal;
            });
    }
}

public interface ITextLocalizer
{
    string T(string key);
}

public class DictionaryTextLocalizer(IRequestContextAccessor context) : ITextLocalizer
{
    private static readonly Dictionary<string, (string sr, string en)> Messages = new()
    {
        ["QuizCreated"] = ("Kviz je uspešno kreiran.", "Quiz created successfully."),
        ["ValidationRequired"] = ("Polje je obavezno.", "Field is required."),
        ["Forbidden"] = ("Nemate dozvolu za ovu akciju.", "You are not allowed to perform this action."),
        ["CategoryLocked"] = ("Kategorija je zaključana.", "Category score is locked."),
        ["HelpAlreadyUsed"] = ("Pomoć je već iskorišćena za ovaj tim u ovom kvizu.", "Help already used for this team in this quiz.")
    };

    public string T(string key)
    {
        var lang = context.Get().Language;
        return Messages.TryGetValue(key, out var value)
            ? (lang == "en" ? value.en : value.sr)
            : key;
    }
}

public static class SeedData
{
    public static async Task InitializeAsync(AppDbContext db)
    {
        if (await db.Users.AnyAsync()) return;
        var owner = new User { Email = "owner@quizory.local", DisplayName = "Owner", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1!", BCrypt.Net.BCrypt.GenerateSalt(12)), IsEmailVerified = true };
        var org = new Organization { Name = "Demo Organization", SubscriptionPlan = SubscriptionPlan.Trial, TrialEndsAtUtc = DateTime.UtcNow.AddDays(14) };
        var membership = new Membership { UserId = owner.Id, OrganizationId = org.Id, Role = OrganizationRole.Owner };
        db.Users.Add(owner);
        db.Organizations.Add(org);
        db.Memberships.Add(membership);
        db.HelpTypes.AddRange(
            new HelpType { OrganizationId = org.Id, Name = "Joker", Behavior = HelpBehavior.DoubleScore },
            new HelpType { OrganizationId = org.Id, Name = "Double Chance", Behavior = HelpBehavior.MarkerOnly });
        await db.SaveChangesAsync();
    }
}
