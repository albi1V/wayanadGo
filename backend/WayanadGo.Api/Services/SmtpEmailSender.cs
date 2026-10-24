using System.Net;
using System.Net.Mail;

namespace WayanadGo.Api.Services;

public sealed class SmtpEmailSender(
    IConfiguration configuration,
    ILogger<SmtpEmailSender> logger)
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
        try
        {
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
            logger.LogInformation("Attempting OTP SMTP submission. Host={SmtpHost} Port={SmtpPort} TlsEnabled={TlsEnabled} UsernameConfigured={UsernameConfigured} SenderConfigured={SenderConfigured}",
                host, port, client.EnableSsl, !string.IsNullOrWhiteSpace(configuration["Email:Username"]),
                !string.IsNullOrWhiteSpace(configuration["Email:From"]));
            await client.SendMailAsync(message, ct);
            logger.LogInformation("SMTP server accepted the OTP message for submission. Host={SmtpHost} Port={SmtpPort}; inbox delivery is not confirmed.",
                host, port);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("OTP SMTP submission was canceled. Host={SmtpHost} Port={SmtpPort}", host, port);
            throw;
        }
        catch (Exception exception)
        {
            var statusCode = exception is SmtpException smtpException
                ? smtpException.StatusCode.ToString()
                : "NotAvailable";
            logger.LogError("OTP SMTP submission failed. Host={SmtpHost} Port={SmtpPort} ExceptionType={ExceptionType} SmtpStatusCode={SmtpStatusCode}",
                host, port, exception.GetType().Name, statusCode);
            throw;
        }
    }
}
