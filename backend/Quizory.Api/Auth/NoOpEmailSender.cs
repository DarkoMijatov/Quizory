namespace Quizory.Api.Auth;

public class NoOpEmailSender : IEmailSender
{
    public Task SendVerificationEmailAsync(string email, string displayName, string token, string language) => Task.CompletedTask;
    public Task SendPasswordResetEmailAsync(string email, string displayName, string token, string language) => Task.CompletedTask;
    public Task SendTrialReminderEmailAsync(string email, string displayName, int daysLeft, string language) => Task.CompletedTask;
}
