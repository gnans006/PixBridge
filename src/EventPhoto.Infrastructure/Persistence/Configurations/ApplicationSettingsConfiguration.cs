using EventPhoto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventPhoto.Infrastructure.Persistence.Configurations;

/// <summary>EF Core fluent configuration for the <see cref="ApplicationSettings"/> singleton entity.</summary>
public sealed class ApplicationSettingsConfiguration : IEntityTypeConfiguration<ApplicationSettings>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ApplicationSettings> builder)
    {
        builder.ToTable("application_settings");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(a => a.StudioName)
            .HasColumnName("studio_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(a => a.ServerName)
            .HasColumnName("server_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.PublicBaseUrl)
            .HasColumnName("public_base_url")
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(a => a.ServerPort)
            .HasColumnName("server_port")
            .IsRequired();

        builder.Property(a => a.DefaultEventGalleryMode)
            .HasColumnName("default_event_gallery_mode")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.EnableWatermarkByDefault)
            .HasColumnName("enable_watermark_by_default")
            .IsRequired();

        builder.Property(a => a.EnableFaceRecognitionByDefault)
            .HasColumnName("enable_face_recognition_by_default")
            .IsRequired();

        // Feature flags
        builder.Property(a => a.IsWatermarkEnabled)
            .HasColumnName("is_watermark_enabled")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(a => a.IsFaceSearchEnabled)
            .HasColumnName("is_face_search_enabled")
            .IsRequired()
            .HasDefaultValue(true);

        // Phase 6 — Studio Profile
        builder.Property(a => a.Phone)
            .HasColumnName("phone")
            .HasMaxLength(30);

        builder.Property(a => a.Email)
            .HasColumnName("email")
            .HasMaxLength(200);

        builder.Property(a => a.Website)
            .HasColumnName("website")
            .HasMaxLength(2048);

        builder.Property(a => a.Address)
            .HasColumnName("address")
            .HasMaxLength(500);

        builder.Property(a => a.Instagram)
            .HasColumnName("instagram")
            .HasMaxLength(200);

        builder.Property(a => a.Facebook)
            .HasColumnName("facebook")
            .HasMaxLength(200);

        builder.Property(a => a.WhatsApp)
            .HasColumnName("whats_app")
            .HasMaxLength(50);

        builder.Property(a => a.LogoPath)
            .HasColumnName("logo_path")
            .HasMaxLength(1024);

        // Phase 7 — Branding
        builder.Property(a => a.PrimaryColor)
            .HasColumnName("primary_color")
            .HasMaxLength(7)
            .IsRequired()
            .HasDefaultValue("#6366f1");

        builder.Property(a => a.SecondaryColor)
            .HasColumnName("secondary_color")
            .HasMaxLength(7)
            .IsRequired()
            .HasDefaultValue("#8b5cf6");

        builder.Property(a => a.BrandTheme)
            .HasColumnName("brand_theme")
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue("dark");

        builder.Property(a => a.DefaultWatermarkProfileId)
            .HasColumnName("default_watermark_profile_id");

        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(a => a.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();
    }
}
