using WayanadGo.Application.Interfaces;
using WayanadGo.Contracts.Auth;
using WayanadGo.Application.Entities;

namespace WayanadGo.Application.Services;

public sealed class AuthService(IUserRepository users, ITokenService tokens)
{
    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await users.GetByEmailAsync(email, ct);
        if (user is null || !user.IsEmailVerified || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash)) return null;
        return CreateResponse(user);
    }

    private AuthResponse CreateResponse(User user)
    {
        var (token, expires) = tokens.CreateToken(user);
        return new AuthResponse(token, expires, user.Email, user.Role);
    }
}
