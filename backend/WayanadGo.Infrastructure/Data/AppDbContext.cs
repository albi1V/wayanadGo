
using Microsoft.EntityFrameworkCore;
using WayanadGo.Application.Entities;

namespace WayanadGo.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Destination> Destinations => Set<Destination>();

    public DbSet<User> Users => Set<User>();

    public DbSet<PendingRegistration> PendingRegistrations
        => Set<PendingRegistration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Destination>(entity =>
        {
            entity.ToTable("destinations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
            entity.Property(x => x.Slug).HasColumnName("slug").HasMaxLength(160).IsRequired();
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.Property(x => x.Description).HasColumnName("description").HasMaxLength(5000).IsRequired();
            entity.Property(x => x.Location).HasColumnName("location").HasMaxLength(160).IsRequired();
            entity.Property(x => x.ImageUrl).HasColumnName("image_url").HasMaxLength(1000);
            entity.Property(x => x.IsPublished).HasColumnName("is_published").HasDefaultValue(true);
            entity.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasDefaultValueSql("now()");
            entity.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").HasDefaultValueSql("now()");
            entity.HasIndex(x => new { x.IsPublished, x.Name });
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.FullName).HasColumnName("full_name").HasMaxLength(160).HasDefaultValue("").IsRequired();
            entity.Property(x => x.Email).HasColumnName("email").HasMaxLength(254).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(200).IsRequired();
            entity.Property(x => x.Role).HasColumnName("role").HasMaxLength(30).IsRequired();
            entity.Property(x => x.IsEmailVerified).HasColumnName("is_email_verified").HasDefaultValue(true).IsRequired();
            entity.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        });

        modelBuilder.Entity<PendingRegistration>(entity =>
        {
            entity.ToTable("pending_registrations");
            entity.HasKey(x => x.Email);

            entity.Property(x => x.Email)
                .HasColumnName("email")
                .HasMaxLength(254)
                .IsRequired();

            entity.Property(x => x.FullName)
                .HasColumnName("full_name")
                .HasMaxLength(160)
                .IsRequired();

            entity.Property(x => x.PasswordHash)
                .HasColumnName("password_hash")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.OtpHash)
                .HasColumnName("otp_hash")
                .HasMaxLength(64)
                .IsRequired();

            entity.Property(x => x.ExpiresAtUtc)
                .HasColumnName("expires_at_utc");

            entity.Property(x => x.LastSentAtUtc)
                .HasColumnName("last_sent_at_utc");

            entity.Property(x => x.CreatedAtUtc)
                .HasColumnName("created_at_utc");

            entity.Property(x => x.FailedAttempts)
                .HasColumnName("failed_attempts");

            entity.HasIndex(x => x.ExpiresAtUtc);
        });
    }
}
