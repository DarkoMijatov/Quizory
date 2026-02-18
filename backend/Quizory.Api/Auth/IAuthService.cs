using Quizory.Api.Dtos;

namespace Quizory.Api.Auth;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request, string preferredLanguage = "sr");
    Task<AuthResult?> LoginAsync(LoginRequest request);
    Task<bool> VerifyEmailAsync(string token);
    Task<bool> RequestPasswordResetAsync(string email);
    Task<bool> ResetPasswordAsync(string token, string newPassword);
    string GenerateJwt(Guid userId, string email, string? preferredLanguage);
}
