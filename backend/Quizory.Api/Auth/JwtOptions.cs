namespace Quizory.Api.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "Quizory";
    public string Audience { get; set; } = "Quizory";
    public int ExpirationMinutes { get; set; } = 60;
}
