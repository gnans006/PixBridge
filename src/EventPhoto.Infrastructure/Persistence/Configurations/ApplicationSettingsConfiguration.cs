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

        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(a => a.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();
    }
}
