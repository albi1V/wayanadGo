using System.Security.Cryptography;
using System.Text;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using WayanadGo.Application.Entities;
using WayanadGo.Contracts.Auth;
using WayanadGo.Infrastructure.Data;

namespace WayanadGo.Api.Services;

public sealed class EmailOtpService(AppDbContext db, IConfiguration configuration, SmtpEmailSender emailSender)
{
    private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan RegistrationLifetime = TimeSpan.FromHours(24);
    private const int MaximumAttempts = 5;

    public async Task<OtpRequestOutcome> RequestAsync(RequestOtpRequest request, CancellationToken ct)
    {
        if (request.FullName.Trim().Length < 2) return OtpRequestOutcome.InvalidRegistration;
        if (!emailSender.IsConfigured) return OtpRequestOutcome.EmailNotConfigured;

        var email = NormalizeEmail(request.Email);
        if (await db.Users.AnyAsync(x => x.Email == email, ct)) return OtpRequestOutcome.Accepted;

        var now = DateTime.UtcNow;
        var pending = await db.PendingRegistrations.SingleOrDefaultAsync(x => x.Email == email, ct);
        if (pending is not null && now - pending.CreatedAtUtc >= RegistrationLifetime)
        {
            db.PendingRegistrations.Remove(pending);
            await db.SaveChangesAsync(ct);
            pending = null;
        }
        if (pending is not null && now - pending.LastSentAtUtc < ResendCooldown)
            return OtpRequestOutcome.Accepted;

        var otp = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        if (pending is null)
        {
            pending = new PendingRegistration { Email = email, CreatedAtUtc = now };
            db.PendingRegistrations.Add(pending);
        }

        pending.FullName = request.FullName.Trim();
        pending.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        pending.OtpHash = HashOtp(otp);
        pending.ExpiresAtUtc = now.Add(OtpLifetime);
        pending.LastSentAtUtc = now;
        pending.FailedAttempts = 0;
        await db.SaveChangesAsync(ct);

        try
        {
            await emailSender.SendOtpAsync(email, pending.FullName, otp, ct);
        }
        catch (SmtpException)
        {
            pending.LastSentAtUtc = now.Subtract(ResendCooldown);
            await db.SaveChangesAsync(CancellationToken.None);
            throw;
        }
        return OtpRequestOutcome.Accepted;
    }

    public async Task<OtpVerificationOutcome> VerifyAsync(VerifyOtpRequest request, CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);
        var pending = await db.PendingRegistrations.SingleOrDefaultAsync(x => x.Email == email, ct);
        if (pending is null) return OtpVerificationOutcome.Invalid;

        var now = DateTime.UtcNow;
        if (pending.ExpiresAtUtc <= now)
        {
            db.PendingRegistrations.Remove(pending);
            await db.SaveChangesAsync(ct);
            return OtpVerificationOutcome.Expired;
        }

        if (pending.FailedAttempts >= MaximumAttempts) return OtpVerificationOutcome.AttemptsExceeded;

        var expected = Convert.FromHexString(pending.OtpHash);
        var supplied = Convert.FromHexString(HashOtp(request.Otp));
        if (!CryptographicOperations.FixedTimeEquals(expected, supplied))
        {
            pending.FailedAttempts++;
            await db.SaveChangesAsync(ct);
            return pending.FailedAttempts >= MaximumAttempts
                ? OtpVerificationOutcome.AttemptsExceeded
                : OtpVerificationOutcome.Invalid;
        }

        if (await db.Users.AnyAsync(x => x.Email == email, ct))
        {
            db.PendingRegistrations.Remove(pending);
            await db.SaveChangesAsync(ct);
            return OtpVerificationOutcome.Invalid;
        }

        db.Users.Add(new User
        {
            FullName = pending.FullName,
            Email = pending.Email,
            PasswordHash = pending.PasswordHash,
            Role = "User",
            IsEmailVerified = true,
            CreatedAtUtc = now
        });
        db.PendingRegistrations.Remove(pending);
        try
        {
            await db.SaveChangesAsync(ct);
            return OtpVerificationOutcome.Verified;
        }
        catch (DbUpdateException exception) when
            (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return OtpVerificationOutcome.Invalid;
        }
    }

    public async Task<OtpRequestOutcome> ResendAsync(string rawEmail, CancellationToken ct)
    {
        if (!emailSender.IsConfigured) return OtpRequestOutcome.EmailNotConfigured;
        var email = NormalizeEmail(rawEmail);
        var now = DateTime.UtcNow;
        var pending = await db.PendingRegistrations.SingleOrDefaultAsync(x => x.Email == email, ct);
        if (pending is not null && now - pending.CreatedAtUtc >= RegistrationLifetime)
        {
            db.PendingRegistrations.Remove(pending);
            await db.SaveChangesAsync(ct);
            return OtpRequestOutcome.Accepted;
        }
        if (pending is null || now - pending.LastSentAtUtc < ResendCooldown)
            return OtpRequestOutcome.Accepted;

        var otp = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        pending.OtpHash = HashOtp(otp);
        pending.ExpiresAtUtc = now.Add(OtpLifetime);
        pending.LastSentAtUtc = now;
        pending.FailedAttempts = 0;
        await db.SaveChangesAsync(ct);
        try
        {
            await emailSender.SendOtpAsync(email, pending.FullName, otp, ct);
        }
        catch (SmtpException)
        {
            pending.LastSentAtUtc = now.Subtract(ResendCooldown);
            await db.SaveChangesAsync(CancellationToken.None);
            throw;
        }
        return OtpRequestOutcome.Accepted;
    }

    private string HashOtp(string otp)
    {
        var secret = configuration["JWT_SECRET"]!;
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(otp)));
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}

public enum OtpRequestOutcome { Accepted, EmailNotConfigured, InvalidRegistration }
public enum OtpVerificationOutcome { Verified, Invalid, Expired, AttemptsExceeded }
