using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Threading.RateLimiting;
using WayanadGo.Api.Services;
using WayanadGo.Application.Interfaces;
using WayanadGo.Application.Services;
using WayanadGo.Infrastructure.Data;
using WayanadGo.Infrastructure.Repositories;

LoadLocalEnvironmentFile();
var builder = WebApplication.CreateBuilder(args);

// Keep local startup independent of Windows Event Log permissions. The API is
// commonly run from a developer terminal, where writing to the Event Log is denied.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection using user-secrets or environment variables.");

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddScoped<IDestinationRepository, DestinationRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<DestinationService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<SmtpEmailSender>();
builder.Services.AddScoped<EmailOtpService>();

var jwtSecret = builder.Configuration["JWT_SECRET"];
if (string.IsNullOrWhiteSpace(jwtSecret) || Encoding.UTF8.GetByteCount(jwtSecret) < 32)
{
    throw new InvalidOperationException(
        "Set environment variable JWT_SECRET to a random secret of at least 32 bytes before starting the API.");
}
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "WayanadGo.Api";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "WayanadGo.Angular";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("otp-request", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5, Window = TimeSpan.FromMinutes(15), QueueLimit = 0, AutoReplenishment = true
        }));
    options.AddPolicy("otp-verify", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10, Window = TimeSpan.FromMinutes(15), QueueLimit = 0, AutoReplenishment = true
        }));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularFrontend", policy =>
        policy.WithOrigins(
                builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ??
                ["http://localhost:4200", "https://localhost:4200"])
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapGet("/", () => Results.Ok(new { name = "Wayanad Go API", status = "running" }));
}

app.UseHttpsRedirection();
app.UseCors("AngularFrontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

var missingEmailSettings = new List<string>();
if (string.IsNullOrWhiteSpace(builder.Configuration["Email:Host"])) missingEmailSettings.Add("Email__Host");
if (!int.TryParse(builder.Configuration["Email:Port"], out var emailPort) || emailPort is < 1 or > 65535)
    missingEmailSettings.Add("Email__Port");
if (string.IsNullOrWhiteSpace(builder.Configuration["Email:Username"])) missingEmailSettings.Add("Email__Username");
if (string.IsNullOrWhiteSpace(builder.Configuration["Email:Password"])) missingEmailSettings.Add("Email__Password");
if (string.IsNullOrWhiteSpace(builder.Configuration["Email:From"])) missingEmailSettings.Add("Email__From");
if (missingEmailSettings.Count > 0)
    app.Logger.LogWarning("Email OTP is unavailable. Missing or invalid configuration keys: {ConfigurationKeys}",
        string.Join(", ", missingEmailSettings));

app.Run();

static void LoadLocalEnvironmentFile()
{
    var candidates = new List<string>();
    AddAncestors(Directory.GetCurrentDirectory());
    AddAncestors(AppContext.BaseDirectory);
    var envPath = candidates.FirstOrDefault(File.Exists);
    if (envPath is null) return;

    foreach (var rawLine in File.ReadLines(envPath))
    {
        var line = rawLine.Trim();
        if (line.Length == 0 || line.StartsWith('#')) continue;
        var separator = line.IndexOf('=');
        if (separator <= 0) continue;
        var name = line[..separator].Trim();
        if (name.Length == 0 || Environment.GetEnvironmentVariable(name) is not null) continue;
        var value = line[(separator + 1)..].Trim();
        if (value.Length >= 2 && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
            value = value[1..^1];
        Environment.SetEnvironmentVariable(name, value);
    }

    void AddAncestors(string path)
    {
        var directory = new DirectoryInfo(path);
        while (directory is not null)
        {
            candidates.Add(Path.Combine(directory.FullName, ".env"));
            candidates.Add(Path.Combine(directory.FullName, "WayanadGo.Api", ".env"));
            directory = directory.Parent;
        }
    }
}
