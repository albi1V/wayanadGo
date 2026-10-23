namespace WayanadGo.Application.Entities;

public sealed class PendingRegistration
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string OtpHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime LastSentAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public int FailedAttempts { get; set; }
}
