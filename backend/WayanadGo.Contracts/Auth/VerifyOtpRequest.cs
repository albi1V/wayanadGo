using System.ComponentModel.DataAnnotations;

namespace WayanadGo.Contracts.Auth;

public sealed class VerifyOtpRequest
{
    [Required, EmailAddress, StringLength(254)]
    public string Email { get; init; } = string.Empty;

    [Required, RegularExpression("^\\d{6}$")]
    public string Otp { get; init; } = string.Empty;
}
