namespace Quizory.Api.Auth;

public interface IEmailSender
{
    Task SendVerificationEmailAsync(string email, string displayName, string token, string language);
    Task SendPasswordResetEmailAsync(string email, string displayName, string token, string language);
    Task SendTrialReminderEmailAsync(string email, string displayName, int daysLeft, string language);
}
