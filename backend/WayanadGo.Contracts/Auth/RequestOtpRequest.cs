using System.ComponentModel.DataAnnotations;

namespace WayanadGo.Contracts.Auth;

public sealed class RequestOtpRequest
{
    [Required, StringLength(160, MinimumLength = 2)]
    public string FullName { get; init; } = string.Empty;

    [Required, EmailAddress, StringLength(254)]
    public string Email { get; init; } = string.Empty;

    [Required, MinLength(8), MaxLength(128)]
    public string Password { get; init; } = string.Empty;
}
