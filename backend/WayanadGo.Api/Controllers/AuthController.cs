using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Net.Mail;
using WayanadGo.Api.Services;
using WayanadGo.Application.Services;
using WayanadGo.Contracts.Auth;

namespace WayanadGo.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AuthService service, EmailOtpService otpService) : ControllerBase
{
    [HttpGet("me"), Authorize]
    public async Task<IActionResult> CurrentUser(CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId)) return Unauthorized();

        var user = await service.GetCurrentUserAsync(userId, ct);
        return user is null ? Unauthorized() : Ok(user);
    }

    [HttpPost("register")]
    public IActionResult Register() => StatusCode(StatusCodes.Status410Gone,
        new { message = "Email verification is required. Start with POST /api/auth/request-otp." });

    [HttpPost("request-otp"), EnableRateLimiting("otp-request")]
    public async Task<IActionResult> RequestOtp(RequestOtpRequest request, CancellationToken ct)
    {
        OtpRequestOutcome outcome;
        try { outcome = await otpService.RequestAsync(request, ct); }
        catch (SmtpException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "Email delivery is temporarily unavailable. Please try again." });
        }
        if (outcome == OtpRequestOutcome.InvalidRegistration)
            return BadRequest(new { message = "Enter a valid full name." });
        if (outcome == OtpRequestOutcome.EmailNotConfigured)
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "Email verification is not configured on this server." });
        return Ok(new { message = "If this email can be registered, a verification code has been sent." });
    }

    [HttpPost("verify-otp"), EnableRateLimiting("otp-verify")]
    public async Task<IActionResult> VerifyOtp(VerifyOtpRequest request, CancellationToken ct)
    {
        var outcome = await otpService.VerifyAsync(request, ct);
        return outcome switch
        {
            OtpVerificationOutcome.Verified => Ok(new { message = "Email verified. You can now sign in." }),
            OtpVerificationOutcome.Expired => BadRequest(new { message = "This verification code has expired. Request a new code." }),
            OtpVerificationOutcome.AttemptsExceeded => StatusCode(StatusCodes.Status429TooManyRequests,
                new { message = "Too many incorrect attempts. Request a new code." }),
            _ => BadRequest(new { message = "The verification code is invalid or no longer available." })
        };
    }

    [HttpPost("resend-otp"), EnableRateLimiting("otp-request")]
    public async Task<IActionResult> ResendOtp(ResendOtpRequest request, CancellationToken ct)
    {
        OtpRequestOutcome outcome;
        try { outcome = await otpService.ResendAsync(request.Email, ct); }
        catch (SmtpException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "Email delivery is temporarily unavailable. Please try again." });
        }
        if (outcome == OtpRequestOutcome.EmailNotConfigured)
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "Email verification is not configured on this server." });
        return Ok(new { message = "If a registration is pending, a verification code has been sent." });
    }

    [HttpPost("login"), EnableRateLimiting("otp-verify")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await service.LoginAsync(request, ct);
        return result is null
            ? Unauthorized(new { message = "Invalid email or password." })
            : Ok(result);
    }
}
