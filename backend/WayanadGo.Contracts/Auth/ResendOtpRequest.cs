using System.ComponentModel.DataAnnotations;

namespace WayanadGo.Contracts.Auth;

public sealed class ResendOtpRequest
{
    [Required, EmailAddress, StringLength(254)]
    public string Email { get; init; } = string.Empty;
}
