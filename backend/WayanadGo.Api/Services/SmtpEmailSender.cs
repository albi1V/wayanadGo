using System.Net;
using System.Net.Mail;

namespace WayanadGo.Api.Services;

public sealed class SmtpEmailSender(IConfiguration configuration)
{
    public bool IsConfigured
    {
        get
        {
            return !string.IsNullOrWhiteSpace(configuration["Email:Host"]) &&
                   int.TryParse(configuration["Email:Port"], out var port) && port is >= 1 and <= 65535 &&
                   !string.IsNullOrWhiteSpace(configuration["Email:Username"]) &&
                   !string.IsNullOrWhiteSpace(configuration["Email:Password"]) &&
                   !string.IsNullOrWhiteSpace(configuration["Email:From"]);
        }
    }

    public async Task SendOtpAsync(string email, string fullName, string otp, CancellationToken ct)
    {
        if (!IsConfigured) throw new InvalidOperationException("Email delivery is not configured.");
        var host = configuration["Email:Host"]!;
        var port = int.Parse(configuration["Email:Port"]!);
        using var message = new MailMessage(configuration["Email:From"]!, email)
        {
            Subject = "Your Wayanad Go verification code",
            Body = $"Hello {WebUtility.HtmlEncode(fullName)},\n\nYour verification code is {otp}. It expires in five minutes. If you did not request this code, you can ignore this email.",
            IsBodyHtml = false
        };
        using var client = new SmtpClient(host, port)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(configuration["Email:Username"], configuration["Email:Password"]),
            Timeout = 15_000
        };
        ct.ThrowIfCancellationRequested();
        await client.SendMailAsync(message, ct);
    }
}
