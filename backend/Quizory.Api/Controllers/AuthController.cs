using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quizory.Api.Auth;
using Quizory.Api.Dtos;

namespace Quizory.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService auth) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, [FromQuery] string? lang)
    {
        var result = await auth.RegisterAsync(request, lang ?? "sr");
        if (!result.Success)
            return BadRequest(new { errorCode = result.ErrorCode });
        return Ok(new
        {
            result.Token,
            result.UserId,
            result.Email,
            result.DisplayName,
            result.PreferredLanguage,
            result.OrganizationId,
            result.Role
        });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await auth.LoginAsync(request);
        if (result == null)
            return Unauthorized(new { errorCode = "InvalidCredentials" });
        if (!result.Success)
            return BadRequest(new { errorCode = result.ErrorCode });
        return Ok(new
        {
            result.Token,
            result.UserId,
            result.Email,
            result.DisplayName,
            result.PreferredLanguage,
            result.OrganizationId,
            result.Role
        });
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        var ok = await auth.VerifyEmailAsync(token);
        if (!ok) return BadRequest(new { errorCode = "InvalidOrExpiredToken" });
        return NoContent();
    }

    [HttpPost("password-reset/request")]
    [AllowAnonymous]
    public async Task<IActionResult> RequestPasswordReset([FromBody] RequestPasswordResetRequest req)
    {
        await auth.RequestPasswordResetAsync(req.Email);
        return NoContent(); // Always 204 to not reveal existence
    }

    [HttpPost("password-reset/confirm")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var ok = await auth.ResetPasswordAsync(request.Token, request.NewPassword);
        if (!ok) return BadRequest(new { errorCode = "InvalidOrExpiredToken" });
        return NoContent();
    }
}

public record RequestPasswordResetRequest(string Email);
