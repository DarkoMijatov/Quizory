using Microsoft.EntityFrameworkCore;
using Quizory.Api.Auth;
using Quizory.Api.Data;
using Quizory.Api.Domain;

namespace Quizory.Api.Services;

public class TrialReminderService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<TrialReminderService> _log;

    public TrialReminderService(IServiceProvider sp, ILogger<TrialReminderService> log)
    {
        _sp = sp;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DoCheckAsync();
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Trial reminder check failed.");
            }
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task DoCheckAsync()
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var inFiveDays = DateTime.UtcNow.Date.AddDays(5);
        var inFiveDaysEnd = inFiveDays.AddDays(1);
        var orgs = await db.Organizations
            .Where(o => o.SubscriptionPlan == SubscriptionPlan.Trial && o.TrialEndsAtUtc >= inFiveDays && o.TrialEndsAtUtc < inFiveDaysEnd)
            .ToListAsync();
        foreach (var org in orgs)
        {
            var ownerMembership = await db.Memberships.FirstOrDefaultAsync(m => m.OrganizationId == org.Id && m.Role == OrganizationRole.Owner);
            if (ownerMembership == null) continue;
            var user = await db.Users.FindAsync(ownerMembership.UserId);
            if (user == null) continue;
            var daysLeft = (int)(org.TrialEndsAtUtc!.Value - DateTime.UtcNow).TotalDays;
            await emailSender.SendTrialReminderEmailAsync(user.Email, user.DisplayName, daysLeft, user.PreferredLanguage);
        }
    }
}
