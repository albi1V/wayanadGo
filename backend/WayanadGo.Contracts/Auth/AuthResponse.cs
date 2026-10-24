namespace WayanadGo.Contracts.Auth;

public sealed record AuthResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    string Email,
    string Role,
    [property: System.Text.Json.Serialization.JsonPropertyName("fullName")] string FullName);
