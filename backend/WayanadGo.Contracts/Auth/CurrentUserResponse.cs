namespace WayanadGo.Contracts.Auth;

public sealed record CurrentUserResponse(
    [property: System.Text.Json.Serialization.JsonPropertyName("fullName")] string FullName,
    string Email,
    string Role);
