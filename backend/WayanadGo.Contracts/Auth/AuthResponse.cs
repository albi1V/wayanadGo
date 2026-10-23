namespace WayanadGo.Contracts.Auth;

public sealed record AuthResponse(string AccessToken, DateTime ExpiresAtUtc, string Email, string Role);
