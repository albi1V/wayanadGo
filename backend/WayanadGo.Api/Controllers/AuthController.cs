using Microsoft.AspNetCore.Mvc;
using WayanadGo.Application.Services;
using WayanadGo.Contracts.Auth;

namespace WayanadGo.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AuthService service) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        var result = await service.RegisterAsync(request, ct);
        return result is null
            ? Conflict(new { message = "An account with this email already exists." })
            : Ok(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await service.LoginAsync(request, ct);
        return result is null
            ? Unauthorized(new { message = "Invalid email or password." })
            : Ok(result);
    }
}
