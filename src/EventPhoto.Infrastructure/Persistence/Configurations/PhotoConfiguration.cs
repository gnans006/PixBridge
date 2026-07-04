using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventPhoto.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core fluent configuration for the <see cref="Photo"/> aggregate.
/// </summary>
public sealed class PhotoConfiguration : IEntityTypeConfiguration<Photo>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Photo> builder)
    {
        builder.ToTable("photos");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(p => p.EventId)
            .HasColumnName("event_id")
            .IsRequired();

        builder.Property(p => p.FileName)
            .HasColumnName("file_name")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(p => p.OriginalPath)
            .HasColumnName("original_path")
            .HasMaxLength(1024)
            .IsRequired();

        builder.Property(p => p.ThumbnailPath)
            .HasColumnName("thumbnail_path")
            .HasMaxLength(1024)
            .IsRequired();

        builder.Property(p => p.FileSizeBytes)
            .HasColumnName("file_size_bytes")
            .IsRequired();

        builder.Property(p => p.MimeType)
            .HasColumnName("mime_type")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(p => p.Width)
            .HasColumnName("width");

        builder.Property(p => p.Height)
            .HasColumnName("height");

        builder.Property(p => p.TakenAt)
            .HasColumnName("taken_at");

        builder.Property(p => p.CapturedAt)
            .HasColumnName("captured_at")
            .IsRequired();

        builder.Property(p => p.DownloadCount)
            .HasColumnName("download_count")
            .IsRequired();

        builder.Property(p => p.IsDeleted)
            .HasColumnName("is_deleted")
            .IsRequired();

        builder.Property(p => p.ThumbnailStatus)
            .HasColumnName("thumbnail_status")
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(p => p.EventId);  // FK navigation
        builder.HasIndex(p => new { p.EventId, p.IsDeleted, p.CapturedAt })
            .IsDescending(false, false, true)
            .HasDatabaseName("IX_photos_event_paged");  // covers ORDER BY captured_at DESC
        builder.HasIndex(p => p.OriginalPath).IsUnique();
        builder.HasIndex(p => new { p.ThumbnailStatus, p.IsDeleted });
    }
}
