# Wayanad Go - initial ASP.NET Core backend scaffold
# Run from D:\abhilash\wayanad-go in PowerShell.
# This script preserves existing files by backing up files it must replace.
# It does not create a PostgreSQL database or run migrations.

$ErrorActionPreference = "Stop"
$Root = (Get-Location).Path
$Backend = Join-Path $Root "backend"

# Keep local scaffold backups out of Git without replacing the existing .gitignore.
$RootGitIgnore = Join-Path $Root ".gitignore"
if (Test-Path $RootGitIgnore) {
    $ignoreText = Get-Content -LiteralPath $RootGitIgnore -Raw
    if ($ignoreText -notmatch '(?m)^backend/_scaffold-backup-\*/?$') {
        Add-Content -LiteralPath $RootGitIgnore -Value "`r`n# Local backups created by backend scaffold script`r`nbackend/_scaffold-backup-*/"
    }
}

if (-not (Test-Path (Join-Path $Root "frontend"))) {
    throw "Run this script from the wayanad-go project root (the folder containing frontend and backend)."
}
if (-not (Test-Path $Backend)) {
    throw "The backend folder was not found."
}

$Projects = @{
    Api = Join-Path $Backend "WayanadGo.Api"
    Application = Join-Path $Backend "WayanadGo.Application"
    Infrastructure = Join-Path $Backend "WayanadGo.Infrastructure"
    Contracts = Join-Path $Backend "WayanadGo.Contracts"
}
foreach ($p in $Projects.Values) {
    if (-not (Test-Path $p)) { throw "Expected existing .NET project folder not found: $p. This script will not recreate or overwrite it." }
}
foreach ($p in @(
    (Join-Path $Projects.Api "WayanadGo.Api.csproj"),
    (Join-Path $Projects.Application "WayanadGo.Application.csproj"),
    (Join-Path $Projects.Infrastructure "WayanadGo.Infrastructure.csproj"),
    (Join-Path $Projects.Contracts "WayanadGo.Contracts.csproj")
)) {
    if (-not (Test-Path $p)) { throw "Expected project file not found: $p" }
}

# Back up only the generated template files this scaffold needs to replace.
$BackupRoot = Join-Path $Backend ("_scaffold-backup-" + (Get-Date -Format "yyyyMMdd-HHmmss"))
$FilesToReplace = @(
    (Join-Path $Projects.Api "Program.cs"),
    (Join-Path $Projects.Api "appsettings.json")
)
$ExistingToBackup = @($FilesToReplace | Where-Object { Test-Path $_ })
if ($ExistingToBackup.Count -gt 0) {
    New-Item -ItemType Directory -Path $BackupRoot -Force | Out-Null
    foreach ($file in $ExistingToBackup) {
        $relative = $file.Substring($Backend.Length).TrimStart('\')
        $dest = Join-Path $BackupRoot $relative
        New-Item -ItemType Directory -Path (Split-Path $dest -Parent) -Force | Out-Null
        Copy-Item -LiteralPath $file -Destination $dest -Force
    }
    Write-Host "Backed up template files to: $BackupRoot" -ForegroundColor Yellow
}

function Write-ProjectFile {
    param([Parameter(Mandatory=$true)][string]$Path, [Parameter(Mandatory=$true)][string]$Content)
    if (Test-Path $Path) {
        # New feature files are never overwritten. Only the two explicitly backed-up template files above are replaced.
        $allowed = $FilesToReplace -contains $Path
        if (-not $allowed) { throw "Refusing to overwrite existing file: $Path" }
    }
    New-Item -ItemType Directory -Path (Split-Path $Path -Parent) -Force | Out-Null
    Set-Content -LiteralPath $Path -Value $Content -Encoding utf8
}

# Add all projects to the existing .NET 10 solution (works with the .slnx created by .NET 10).
$Solution = Join-Path $Backend "WayanadGo.slnx"
if (-not (Test-Path $Solution)) { throw "WayanadGo.slnx was not found; no solution file will be created over existing work." }
$projectFiles = @(
    "backend/WayanadGo.Api/WayanadGo.Api.csproj",
    "backend/WayanadGo.Application/WayanadGo.Application.csproj",
    "backend/WayanadGo.Infrastructure/WayanadGo.Infrastructure.csproj",
    "backend/WayanadGo.Contracts/WayanadGo.Contracts.csproj"
)
foreach ($project in $projectFiles) {
    dotnet sln $Solution add $project 2>$null
    if ($LASTEXITCODE -ne 0) {
        # "already in solution" is harmless; verify membership later.
        Write-Host "Project may already be in the solution: $project"
    }
}

# Project references.
dotnet add (Join-Path $Projects.Api "WayanadGo.Api.csproj") reference (Join-Path $Projects.Application "WayanadGo.Application.csproj")
if ($LASTEXITCODE -ne 0) { throw "Failed adding API -> Application reference." }
dotnet add (Join-Path $Projects.Api "WayanadGo.Api.csproj") reference (Join-Path $Projects.Infrastructure "WayanadGo.Infrastructure.csproj")
if ($LASTEXITCODE -ne 0) { throw "Failed adding API -> Infrastructure reference." }
dotnet add (Join-Path $Projects.Api "WayanadGo.Api.csproj") reference (Join-Path $Projects.Contracts "WayanadGo.Contracts.csproj")
if ($LASTEXITCODE -ne 0) { throw "Failed adding API -> Contracts reference." }
dotnet add (Join-Path $Projects.Application "WayanadGo.Application.csproj") reference (Join-Path $Projects.Contracts "WayanadGo.Contracts.csproj")
if ($LASTEXITCODE -ne 0) { throw "Failed adding Application -> Contracts reference." }
dotnet add (Join-Path $Projects.Infrastructure "WayanadGo.Infrastructure.csproj") reference (Join-Path $Projects.Application "WayanadGo.Application.csproj")
if ($LASTEXITCODE -ne 0) { throw "Failed adding Infrastructure -> Application reference." }
dotnet add (Join-Path $Projects.Infrastructure "WayanadGo.Infrastructure.csproj") reference (Join-Path $Projects.Contracts "WayanadGo.Contracts.csproj")
if ($LASTEXITCODE -ne 0) { throw "Failed adding Infrastructure -> Contracts reference." }

# Install .NET 10 compatible packages.
$packageCommands = @(
    @{ Project = (Join-Path $Projects.Infrastructure "WayanadGo.Infrastructure.csproj"); Package = "Npgsql.EntityFrameworkCore.PostgreSQL"; Version = "10.0.0" },
    @{ Project = (Join-Path $Projects.Infrastructure "WayanadGo.Infrastructure.csproj"); Package = "Microsoft.EntityFrameworkCore.Design"; Version = "10.0.0" },
    @{ Project = (Join-Path $Projects.Api "WayanadGo.Api.csproj"); Package = "Microsoft.AspNetCore.Authentication.JwtBearer"; Version = "10.0.0" },
    @{ Project = (Join-Path $Projects.Api "WayanadGo.Api.csproj"); Package = "Microsoft.EntityFrameworkCore.Design"; Version = "10.0.0" },
    @{ Project = (Join-Path $Projects.Application "WayanadGo.Application.csproj"); Package = "BCrypt.Net-Next"; Version = "4.0.3" }
)
foreach ($pkg in $packageCommands) {
    dotnet add $pkg.Project package $pkg.Package --version $pkg.Version
    if ($LASTEXITCODE -ne 0) { throw "Failed installing $($pkg.Package). Check network/package compatibility and rerun after resolving the error." }
}

# Folder layout.
$folders = @(
    "WayanadGo.Api/Controllers", "WayanadGo.Api/Services", "WayanadGo.Api/Properties",
    "WayanadGo.Application/Interfaces", "WayanadGo.Application/Services",
    "WayanadGo.Infrastructure/Data", "WayanadGo.Infrastructure/Repositories", "WayanadGo.Application/Entities",
    "WayanadGo.Contracts/Auth", "WayanadGo.Contracts/Destinations"
)
foreach ($folder in $folders) { New-Item -ItemType Directory -Path (Join-Path $Backend $folder) -Force | Out-Null }

Write-ProjectFile (Join-Path $Projects.Contracts "Destinations/DestinationResponse.cs") @'
namespace WayanadGo.Contracts.Destinations;

public sealed record DestinationResponse(
    int Id,
    string Name,
    string Slug,
    string Description,
    string Location,
    string? ImageUrl,
    bool IsPublished);
'@

Write-ProjectFile (Join-Path $Projects.Contracts "Destinations/CreateDestinationRequest.cs") @'
using System.ComponentModel.DataAnnotations;

namespace WayanadGo.Contracts.Destinations;

public sealed class CreateDestinationRequest
{
    [Required, StringLength(120, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [Required, StringLength(160, MinimumLength = 2)]
    public string Slug { get; init; } = string.Empty;

    [Required, StringLength(5000, MinimumLength = 10)]
    public string Description { get; init; } = string.Empty;

    [Required, StringLength(160, MinimumLength = 2)]
    public string Location { get; init; } = string.Empty;

    [Url, StringLength(1000)]
    public string? ImageUrl { get; init; }

    public bool IsPublished { get; init; } = true;
}
'@

Write-ProjectFile (Join-Path $Projects.Contracts "Auth/RegisterRequest.cs") @'
using System.ComponentModel.DataAnnotations;

namespace WayanadGo.Contracts.Auth;

public sealed class RegisterRequest
{
    [Required, EmailAddress, StringLength(254)]
    public string Email { get; init; } = string.Empty;

    [Required, MinLength(8), MaxLength(128)]
    public string Password { get; init; } = string.Empty;
}
'@

Write-ProjectFile (Join-Path $Projects.Contracts "Auth/LoginRequest.cs") @'
using System.ComponentModel.DataAnnotations;

namespace WayanadGo.Contracts.Auth;

public sealed class LoginRequest
{
    [Required, EmailAddress, StringLength(254)]
    public string Email { get; init; } = string.Empty;

    [Required, MinLength(8), MaxLength(128)]
    public string Password { get; init; } = string.Empty;
}
'@

Write-ProjectFile (Join-Path $Projects.Contracts "Auth/AuthResponse.cs") @'
namespace WayanadGo.Contracts.Auth;

public sealed record AuthResponse(string AccessToken, DateTime ExpiresAtUtc, string Email, string Role);
'@

Write-ProjectFile (Join-Path $Projects.Application "Interfaces/IDestinationRepository.cs") @'
using WayanadGo.Application.Entities;

namespace WayanadGo.Application.Interfaces;

public interface IDestinationRepository
{
    Task<IReadOnlyList<Destination>> GetPublishedAsync(string? search, CancellationToken cancellationToken);
    Task<Destination?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<Destination?> GetBySlugAsync(string slug, CancellationToken cancellationToken);
    Task<Destination> AddAsync(Destination destination, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
'@

Write-ProjectFile (Join-Path $Projects.Application "Interfaces/IUserRepository.cs") @'
using WayanadGo.Application.Entities;

namespace WayanadGo.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    Task<User> AddAsync(User user, CancellationToken cancellationToken);
}
'@

Write-ProjectFile (Join-Path $Projects.Application "Interfaces/ITokenService.cs") @'
using WayanadGo.Application.Entities;

namespace WayanadGo.Application.Interfaces;

public interface ITokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateToken(User user);
}
'@

Write-ProjectFile (Join-Path $Projects.Application "Services/DestinationService.cs") @'
using WayanadGo.Application.Interfaces;
using WayanadGo.Contracts.Destinations;
using WayanadGo.Application.Entities;

namespace WayanadGo.Application.Services;

public sealed class DestinationService(IDestinationRepository repository)
{
    public async Task<IReadOnlyList<DestinationResponse>> SearchAsync(string? search, CancellationToken ct)
    {
        var rows = await repository.GetPublishedAsync(search, ct);
        return rows.Select(ToResponse).ToArray();
    }

    public async Task<DestinationResponse?> GetPublishedByIdAsync(int id, CancellationToken ct)
    {
        var row = await repository.GetByIdAsync(id, ct);
        return row is { IsPublished: true } ? ToResponse(row) : null;
    }

    public async Task<DestinationResponse> CreateAsync(CreateDestinationRequest request, CancellationToken ct)
    {
        var entity = new Destination
        {
            Name = request.Name.Trim(),
            Slug = request.Slug.Trim().ToLowerInvariant(),
            Description = request.Description.Trim(),
            Location = request.Location.Trim(),
            ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim(),
            IsPublished = request.IsPublished
        };
        var created = await repository.AddAsync(entity, ct);
        return ToResponse(created);
    }

    public async Task<DestinationResponse?> UpdateAsync(int id, CreateDestinationRequest request, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity is null) return null;
        entity.Name = request.Name.Trim();
        entity.Slug = request.Slug.Trim().ToLowerInvariant();
        entity.Description = request.Description.Trim();
        entity.Location = request.Location.Trim();
        entity.ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim();
        entity.IsPublished = request.IsPublished;
        await repository.SaveChangesAsync(ct);
        return ToResponse(entity);
    }

    public Task<bool> DeleteAsync(int id, CancellationToken ct) => repository.DeleteAsync(id, ct);

    private static DestinationResponse ToResponse(Destination d) =>
        new(d.Id, d.Name, d.Slug, d.Description, d.Location, d.ImageUrl, d.IsPublished);
}
'@

Write-ProjectFile (Join-Path $Projects.Application "Services/AuthService.cs") @'
using WayanadGo.Application.Interfaces;
using WayanadGo.Contracts.Auth;
using WayanadGo.Application.Entities;

namespace WayanadGo.Application.Services;

public sealed class AuthService(IUserRepository users, ITokenService tokens)
{
    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await users.GetByEmailAsync(email, ct) is not null) return null;

        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = "User",
            CreatedAtUtc = DateTime.UtcNow
        };
        await users.AddAsync(user, ct);
        return CreateResponse(user);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await users.GetByEmailAsync(email, ct);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash)) return null;
        return CreateResponse(user);
    }

    private AuthResponse CreateResponse(User user)
    {
        var (token, expires) = tokens.CreateToken(user);
        return new AuthResponse(token, expires, user.Email, user.Role);
    }
}
'@

Write-ProjectFile (Join-Path $Projects.Application "Entities/Destination.cs") @'
namespace WayanadGo.Application.Entities;

public sealed class Destination
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public bool IsPublished { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
'@

Write-ProjectFile (Join-Path $Projects.Application "Entities/User.cs") @'
namespace WayanadGo.Application.Entities;

public sealed class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
'@

Write-ProjectFile (Join-Path $Projects.Infrastructure "Data/AppDbContext.cs") @'
using Microsoft.EntityFrameworkCore;
using WayanadGo.Application.Entities;

namespace WayanadGo.Infrastructure.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Destination> Destinations => Set<Destination>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Destination>(entity =>
        {
            entity.ToTable("destinations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(160).IsRequired();
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.Property(x => x.Description).HasMaxLength(5000).IsRequired();
            entity.Property(x => x.Location).HasMaxLength(160).IsRequired();
            entity.Property(x => x.ImageUrl).HasMaxLength(1000);
            entity.HasIndex(x => new { x.IsPublished, x.Name });
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Email).HasMaxLength(254).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.PasswordHash).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Role).HasMaxLength(30).IsRequired();
        });
    }
}
'@

Write-ProjectFile (Join-Path $Projects.Infrastructure "Repositories/DestinationRepository.cs") @'
using Microsoft.EntityFrameworkCore;
using WayanadGo.Application.Interfaces;
using WayanadGo.Infrastructure.Data;
using WayanadGo.Application.Entities;

namespace WayanadGo.Infrastructure.Repositories;

public sealed class DestinationRepository(AppDbContext db) : IDestinationRepository
{
    public async Task<IReadOnlyList<Destination>> GetPublishedAsync(string? search, CancellationToken ct)
    {
        var query = db.Destinations.AsNoTracking().Where(x => x.IsPublished);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => EF.Functions.ILike(x.Name, $"%{term}%") ||
                                     EF.Functions.ILike(x.Location, $"%{term}%") ||
                                     EF.Functions.ILike(x.Description, $"%{term}%"));
        }
        return await query.OrderBy(x => x.Name).Take(100).ToListAsync(ct);
    }

    public Task<Destination?> GetByIdAsync(int id, CancellationToken ct) =>
        db.Destinations.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<Destination?> GetBySlugAsync(string slug, CancellationToken ct) =>
        db.Destinations.FirstOrDefaultAsync(x => x.Slug == slug, ct);

    public async Task<Destination> AddAsync(Destination destination, CancellationToken ct)
    {
        db.Destinations.Add(destination);
        await db.SaveChangesAsync(ct);
        return destination;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var entity = await db.Destinations.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return false;
        db.Destinations.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
'@

Write-ProjectFile (Join-Path $Projects.Infrastructure "Repositories/UserRepository.cs") @'
using Microsoft.EntityFrameworkCore;
using WayanadGo.Application.Interfaces;
using WayanadGo.Infrastructure.Data;
using WayanadGo.Application.Entities;

namespace WayanadGo.Infrastructure.Repositories;

public sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct) =>
        db.Users.FirstOrDefaultAsync(x => x.Email == email, ct);

    public async Task<User> AddAsync(User user, CancellationToken ct)
    {
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return user;
    }
}
'@

Write-ProjectFile (Join-Path $Projects.Api "Services/JwtTokenService.cs") @'
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using WayanadGo.Application.Interfaces;
using WayanadGo.Application.Entities;

namespace WayanadGo.Api.Services;

public sealed class JwtTokenService(IConfiguration configuration) : ITokenService
{
    public (string Token, DateTime ExpiresAtUtc) CreateToken(User user)
    {
        var key = configuration["JWT_SECRET"];
        if (string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key) < 32)
            throw new InvalidOperationException("Set JWT_SECRET to a random secret of at least 32 bytes in the environment.");

        var issuer = configuration["Jwt:Issuer"] ?? "WayanadGo.Api";
        var audience = configuration["Jwt:Audience"] ?? "WayanadGo.Angular";
        var expires = DateTime.UtcNow.AddHours(1);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(issuer, audience, claims, expires: expires, signingCredentials: credentials);
        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
'@

Write-ProjectFile (Join-Path $Projects.Api "Controllers/DestinationsController.cs") @'
using Microsoft.AspNetCore.Mvc;
using WayanadGo.Application.Services;

namespace WayanadGo.Api.Controllers;

[ApiController]
[Route("api/destinations")]
public sealed class DestinationsController(DestinationService service) : ControllerBase
{
    // GET /api/destinations?search=waterfall
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? search, CancellationToken ct) =>
        Ok(await service.SearchAsync(search, ct));

    // GET /api/destinations/12
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var destination = await service.GetPublishedByIdAsync(id, ct);
        return destination is null ? NotFound() : Ok(destination);
    }
}
'@

Write-ProjectFile (Join-Path $Projects.Api "Controllers/AdminDestinationsController.cs") @'
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WayanadGo.Application.Services;
using WayanadGo.Contracts.Destinations;

namespace WayanadGo.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/destinations")]
public sealed class AdminDestinationsController(DestinationService service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateDestinationRequest request, CancellationToken ct)
    {
        try { return Created("/api/admin/destinations", await service.CreateAsync(request, ct)); }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            return Conflict(new { message = "A destination with this slug may already exist." });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CreateDestinationRequest request, CancellationToken ct)
    {
        var updated = await service.UpdateAsync(id, request, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) =>
        await service.DeleteAsync(id, ct) ? NoContent() : NotFound();
}
'@

Write-ProjectFile (Join-Path $Projects.Api "Controllers/AuthController.cs") @'
using Microsoft.AspNetCore.Mvc;
using WayanadGo.Application.Services;
using WayanadGo.Contracts.Auth;

namespace WayanadGo.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AuthService service) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        var result = await service.RegisterAsync(request, ct);
        return result is null
            ? Conflict(new { message = "An account with this email already exists." })
            : Ok(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await service.LoginAsync(request, ct);
        return result is null
            ? Unauthorized(new { message = "Invalid email or password." })
            : Ok(result);
    }
}
'@

Write-ProjectFile (Join-Path $Projects.Api "Program.cs") @'
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WayanadGo.Api.Services;
using WayanadGo.Application.Interfaces;
using WayanadGo.Application.Services;
using WayanadGo.Infrastructure.Data;
using WayanadGo.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
'@

Write-ProjectFile (Join-Path $Projects.Api "appsettings.json") @'
{
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "Jwt": {
    "Issuer": "WayanadGo.Api",
    "Audience": "WayanadGo.Angular"
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:4200",
      "https://localhost:4200"
    ]
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
'@

# Build to verify scaffold. Restore needs an internet connection for NuGet packages.
Write-Host "`nRestoring and building the solution..." -ForegroundColor Cyan
dotnet restore $Solution
if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed. Fix the reported package/network error and rerun the build step." }

dotnet build $Solution --no-restore
if ($LASTEXITCODE -ne 0) { throw "Build failed. The script has preserved backups and source files; review the compiler errors above." }

Write-Host "`nScaffold created and build succeeded." -ForegroundColor Green
Write-Host "Next steps: configure PostgreSQL connection + JWT_SECRET, create the database, then add/apply the EF Core migration."
Write-Host "IMPORTANT: Public registration always creates role 'User'. Admin role provisioning must be done separately and securely."
