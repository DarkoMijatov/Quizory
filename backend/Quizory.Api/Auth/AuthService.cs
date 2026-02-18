using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Quizory.Api.Data;
using Quizory.Api.Domain;
using Quizory.Api.Dtos;

namespace Quizory.Api.Auth;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly JwtOptions _jwt;
    private readonly IEmailSender _emailSender;

    public AuthService(AppDbContext db, IOptions<JwtOptions> jwt, IEmailSender emailSender)
    {
        _db = db;
        _jwt = jwt.Value;
        _emailSender = emailSender;
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request, string preferredLanguage = "sr")
    {
        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
            return AuthResultFactory.Fail("EmailAlreadyExists");

        var hash = BCrypt.Net.BCrypt.HashPassword(request.Password, BCrypt.Net.BCrypt.GenerateSalt(12));
        var user = new User
        {
            Email = request.Email,
            PasswordHash = hash,
            DisplayName = request.DisplayName ?? request.Email,
            PreferredLanguage = preferredLanguage,
            IsEmailVerified = false
        };
        _db.Users.Add(user);

        var org = new Organization
        {
            Name = request.OrganizationName ?? "My Organization",
            SubscriptionPlan = SubscriptionPlan.Free,
            TrialEndsAtUtc = null
        };
        _db.Organizations.Add(org);

        _db.Memberships.Add(new Membership { UserId = user.Id, OrganizationId = org.Id, Role = OrganizationRole.Owner });

        var verifyToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        _db.EmailVerificationTokens.Add(new EmailVerificationToken
        {
            UserId = user.Id,
            Token = verifyToken,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(24)
        });

        await _db.SaveChangesAsync();
        await _emailSender.SendVerificationEmailAsync(user.Email, user.DisplayName, verifyToken, preferredLanguage);

        var jwt = GenerateJwt(user.Id, user.Email, user.PreferredLanguage);
        return AuthResultFactory.Ok(jwt, user.Id, user.Email, user.DisplayName, user.PreferredLanguage, org.Id, OrganizationRole.Owner);
    }

    public async Task<AuthResult?> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null)
            return null;
        if (user.PasswordHash.StartsWith("pending") || string.IsNullOrEmpty(user.PasswordHash))
            return AuthResultFactory.Fail("AccountPendingInvite");
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return null;

        var membership = await _db.Memberships
            .Where(m => m.UserId == user.Id)
            .OrderBy(m => m.Role == OrganizationRole.Owner ? 0 : 1)
            .FirstOrDefaultAsync();
        if (membership == null)
            return AuthResultFactory.Fail("NoOrganization");

        var jwt = GenerateJwt(user.Id, user.Email, user.PreferredLanguage);
        return AuthResultFactory.Ok(jwt, user.Id, user.Email, user.DisplayName, user.PreferredLanguage, membership.OrganizationId, membership.Role);
    }

    public async Task<bool> VerifyEmailAsync(string token)
    {
        var record = await _db.EmailVerificationTokens
            .FirstOrDefaultAsync(x => x.Token == token && x.ExpiresAtUtc > DateTime.UtcNow);
        if (record == null) return false;
        var user = await _db.Users.FindAsync(record.UserId);
        if (user == null) return false;
        user.IsEmailVerified = true;
        _db.EmailVerificationTokens.Remove(record);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RequestPasswordResetAsync(string email)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return true; // Don't reveal existence
        if (user.PasswordHash.StartsWith("pending")) return true;

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        _db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            Token = token,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
        });
        await _db.SaveChangesAsync();
        await _emailSender.SendPasswordResetEmailAsync(user.Email, user.DisplayName, token, user.PreferredLanguage);
        return true;
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        var record = await _db.PasswordResetTokens
            .FirstOrDefaultAsync(x => x.Token == token && !x.Used && x.ExpiresAtUtc > DateTime.UtcNow);
        if (record == null) return false;
        var user = await _db.Users.FindAsync(record.UserId);
        if (user == null) return false;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, BCrypt.Net.BCrypt.GenerateSalt(12));
        record.Used = true;
        await _db.SaveChangesAsync();
        return true;
    }

    public string GenerateJwt(Guid userId, string email, string? preferredLanguage)
    {
        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(_jwt.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new("preferred_language", preferredLanguage ?? "sr")
        };
        var token = new JwtSecurityToken(
            _jwt.Issuer,
            _jwt.Audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(_jwt.ExpirationMinutes),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
