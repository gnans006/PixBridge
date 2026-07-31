using EventPhoto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventPhoto.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core fluent configuration for the <see cref="WatermarkConfiguration"/> entity.
/// </summary>
public sealed class WatermarkConfigurationConfiguration : IEntityTypeConfiguration<WatermarkConfiguration>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<WatermarkConfiguration> builder)
    {
        builder.ToTable("watermark_configurations");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(w => w.EventId)
            .HasColumnName("event_id")
            .IsRequired();

        builder.Property(w => w.Enabled)
            .HasColumnName("enabled")
            .IsRequired();

        builder.Property(w => w.Mode)
            .HasColumnName("mode")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(w => w.Style)
            .HasColumnName("style")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(w => w.Opacity)
            .HasColumnName("opacity")
            .IsRequired();

        builder.Property(w => w.Scale)
            .HasColumnName("scale")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(w => w.CustomText)
            .HasColumnName("custom_text")
            .HasMaxLength(500);

        builder.Property(w => w.Template)
            .HasColumnName("template")
            .HasMaxLength(1000);

        builder.Property(w => w.LogoPath)
            .HasColumnName("logo_path")
            .HasMaxLength(1024);

        builder.Property(w => w.TextColor)
            .HasColumnName("text_color")
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue("#FFFFFF");

        builder.Property(w => w.FontName)
            .HasColumnName("font_name")
            .HasMaxLength(100);

        builder.Property(w => w.BackgroundOpacity)
            .HasColumnName("background_opacity")
            .IsRequired()
            .HasDefaultValue(0.20f);

        builder.Property(w => w.ApplyOnPreview)
            .HasColumnName("apply_on_preview")
            .IsRequired();

        builder.Property(w => w.IncludeStudioName)
            .HasColumnName("include_studio_name")
            .IsRequired();

        builder.Property(w => w.IncludeEventName)
            .HasColumnName("include_event_name")
            .IsRequired();

        builder.Property(w => w.IncludeDownloadDate)
            .HasColumnName("include_download_date")
            .IsRequired();

        builder.Property(w => w.ApplyOnDownload)
            .HasColumnName("apply_on_download")
            .IsRequired();

        builder.Property(w => w.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(w => w.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // 1:1 — one WatermarkConfiguration per Event; cascade delete cleans up orphan rows.
        builder.HasOne<Event>()
            .WithOne()
            .HasForeignKey<WatermarkConfiguration>(w => w.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(w => w.EventId)
            .IsUnique()
            .HasDatabaseName("IX_watermark_configurations_event_id");
    }
}
