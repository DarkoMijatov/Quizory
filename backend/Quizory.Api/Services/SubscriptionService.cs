using Microsoft.EntityFrameworkCore;
using Quizory.Api.Data;
using Quizory.Api.Domain;
using Quizory.Api.Dtos;

namespace Quizory.Api.Services;

/// <summary>Subscription plan limits and behavior.</summary>
public static class SubscriptionLimits
{
    public const int FreeQuizzesPerMonth = 5;
    public const int FreeMemberLimit = 1; // owner only
    public const int TrialDays = 14;
    public const int PremiumMemberLimit = 1000; // effectively unlimited for DTO display
}

public interface ISubscriptionService
{
    /// <summary>Effective plan (Trial expired → Free).</summary>
    SubscriptionPlan GetEffectivePlan(Organization org);
    Task<SubscriptionDto> GetCurrentSubscriptionAsync(Guid organizationId);
    Task StartTrialAsync(Guid organizationId);
    Task SetPremiumAsync(Guid organizationId);
    Task DowngradeToFreeAsync(Guid organizationId);
    Task ExpireTrialsAsync();
    void EnforceFeature(string feature);
    void EnforceQuizMonthlyLimit();
    void EnforceMemberLimit();
}

public class SubscriptionService(IRequestContextAccessor context, AppDbContext db, ITextLocalizer localizer) : ISubscriptionService
{
    public SubscriptionPlan GetEffectivePlan(Organization org)
    {
        if (org.SubscriptionPlan == SubscriptionPlan.Trial && org.TrialEndsAtUtc.HasValue && org.TrialEndsAtUtc.Value < DateTime.UtcNow)
            return SubscriptionPlan.Free;
        return org.SubscriptionPlan;
    }

    public async Task<SubscriptionDto> GetCurrentSubscriptionAsync(Guid organizationId)
    {
        var org = await db.Organizations.FindAsync(organizationId);
        if (org == null) throw new InvalidOperationException(localizer.T("OrganizationNotFound"));
        var effective = GetEffectivePlan(org);
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var quizzesThisMonth = await db.Quizzes.CountAsync(q => q.OrganizationId == organizationId && !q.IsDeleted && q.CreatedAtUtc >= startOfMonth);
        var memberCount = await db.Memberships.CountAsync(m => m.OrganizationId == organizationId);
        var (quizzesLimit, memberLimit, features) = GetLimitsAndFeatures(effective);
        var isTrialActive = org.SubscriptionPlan == SubscriptionPlan.Trial && org.TrialEndsAtUtc.HasValue && org.TrialEndsAtUtc.Value >= now;
        return new SubscriptionDto(
            Plan: effective.ToString(),
            IsTrialActive: isTrialActive,
            TrialEndsAtUtc: org.TrialEndsAtUtc,
            QuizzesUsedThisMonth: quizzesThisMonth,
            QuizzesLimitPerMonth: quizzesLimit,
            MemberCount: memberCount,
            MemberLimit: memberLimit,
            Features: features);
    }

    public async Task StartTrialAsync(Guid organizationId)
    {
        var ctx = context.Get();
        if (ctx.OrganizationId != organizationId) throw new UnauthorizedAccessException(localizer.T("Forbidden"));
        if (ctx.Role != OrganizationRole.Owner) throw new UnauthorizedAccessException(localizer.T("OwnerOnly"));
        var org = await db.Organizations.FindAsync(organizationId);
        if (org == null) throw new InvalidOperationException(localizer.T("OrganizationNotFound"));
        var effective = GetEffectivePlan(org);
        if (effective != SubscriptionPlan.Free)
            throw new InvalidOperationException(localizer.T("TrialOnlyFromFree"));
        org.SubscriptionPlan = SubscriptionPlan.Trial;
        org.TrialEndsAtUtc = DateTime.UtcNow.AddDays(SubscriptionLimits.TrialDays);
        org.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task SetPremiumAsync(Guid organizationId)
    {
        var ctx = context.Get();
        if (ctx.OrganizationId != organizationId) throw new UnauthorizedAccessException(localizer.T("Forbidden"));
        if (ctx.Role != OrganizationRole.Owner) throw new UnauthorizedAccessException(localizer.T("OwnerOnly"));
        var org = await db.Organizations.FindAsync(organizationId);
        if (org == null) throw new InvalidOperationException(localizer.T("OrganizationNotFound"));
        org.SubscriptionPlan = SubscriptionPlan.Premium;
        org.TrialEndsAtUtc = null;
        org.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task DowngradeToFreeAsync(Guid organizationId)
    {
        var ctx = context.Get();
        if (ctx.OrganizationId != organizationId) throw new UnauthorizedAccessException(localizer.T("Forbidden"));
        if (ctx.Role != OrganizationRole.Owner) throw new UnauthorizedAccessException(localizer.T("OwnerOnly"));
        var org = await db.Organizations.FindAsync(organizationId);
        if (org == null) throw new InvalidOperationException(localizer.T("OrganizationNotFound"));
        var memberCount = await db.Memberships.CountAsync(m => m.OrganizationId == organizationId);
        if (memberCount > SubscriptionLimits.FreeMemberLimit)
            throw new InvalidOperationException(localizer.T("DowngradeRemoveMembersFirst"));
        org.SubscriptionPlan = SubscriptionPlan.Free;
        org.TrialEndsAtUtc = null;
        org.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task ExpireTrialsAsync()
    {
        var now = DateTime.UtcNow;
        var expired = await db.Organizations.Where(o => o.SubscriptionPlan == SubscriptionPlan.Trial && o.TrialEndsAtUtc.HasValue && o.TrialEndsAtUtc.Value < now).ToListAsync();
        foreach (var org in expired)
        {
            org.SubscriptionPlan = SubscriptionPlan.Free;
            org.TrialEndsAtUtc = null;
            org.UpdatedAtUtc = now;
        }
        if (expired.Count > 0)
            await db.SaveChangesAsync();
    }

    public void EnforceFeature(string feature)
    {
        var ctx = context.Get();
        var org = db.Organizations.Find(ctx.OrganizationId)!;
        var effective = GetEffectivePlan(org);
        if (effective == SubscriptionPlan.Free && (feature is "leagues" or "questionBank" or "members" or "share"))
            throw new InvalidOperationException(localizer.T("FeatureRequiresPremium").Replace("{feature}", feature));
    }

    public void EnforceQuizMonthlyLimit()
    {
        var ctx = context.Get();
        var org = db.Organizations.Find(ctx.OrganizationId)!;
        var effective = GetEffectivePlan(org);
        if (effective != SubscriptionPlan.Free) return;
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var count = db.Quizzes.Count(q => q.OrganizationId == ctx.OrganizationId && !q.IsDeleted && q.CreatedAtUtc >= startOfMonth);
        if (count >= SubscriptionLimits.FreeQuizzesPerMonth)
            throw new InvalidOperationException(localizer.T("FreeQuizLimitReached"));
    }

    public void EnforceMemberLimit()
    {
        var ctx = context.Get();
        var org = db.Organizations.Find(ctx.OrganizationId)!;
        var effective = GetEffectivePlan(org);
        if (effective == SubscriptionPlan.Free)
        {
            var memberCount = db.Memberships.Count(m => m.OrganizationId == ctx.OrganizationId);
            if (memberCount >= SubscriptionLimits.FreeMemberLimit)
                throw new InvalidOperationException(localizer.T("FreeMemberLimitReached"));
        }
    }

    private static (int QuizzesLimit, int MemberLimit, SubscriptionFeaturesDto Features) GetLimitsAndFeatures(SubscriptionPlan effective)
    {
        return effective switch
        {
            SubscriptionPlan.Free => (
                SubscriptionLimits.FreeQuizzesPerMonth,
                SubscriptionLimits.FreeMemberLimit,
                new SubscriptionFeaturesDto(Leagues: false, QuestionBank: false, Members: false, Share: false, CustomBranding: false)),
            SubscriptionPlan.Trial => (
                int.MaxValue,
                SubscriptionLimits.PremiumMemberLimit,
                new SubscriptionFeaturesDto(Leagues: true, QuestionBank: true, Members: true, Share: true, CustomBranding: true)),
            SubscriptionPlan.Premium => (
                int.MaxValue,
                SubscriptionLimits.PremiumMemberLimit,
                new SubscriptionFeaturesDto(Leagues: true, QuestionBank: true, Members: true, Share: true, CustomBranding: true)),
            _ => (SubscriptionLimits.FreeQuizzesPerMonth, SubscriptionLimits.FreeMemberLimit,
                new SubscriptionFeaturesDto(false, false, false, false, false))
        };
    }
}
