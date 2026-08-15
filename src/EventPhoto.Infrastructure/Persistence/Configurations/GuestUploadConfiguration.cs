using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventPhoto.Infrastructure.Persistence.Configurations;

/// <summary>EF Core fluent configuration for <see cref="GuestUpload"/>.</summary>
public sealed class GuestUploadConfiguration : IEntityTypeConfiguration<GuestUpload>
{
    public void Configure(EntityTypeBuilder<GuestUpload> builder)
    {
        builder.ToTable("guest_uploads");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(u => u.EventId).HasColumnName("event_id").IsRequired();
        builder.Property(u => u.SessionId).HasColumnName("session_id").IsRequired();

        builder.Property(u => u.OriginalFileName)
            .HasColumnName("original_file_name")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(u => u.StoredPath)
            .HasColumnName("stored_path")
            .HasMaxLength(1024)
            .IsRequired();

        builder.Property(u => u.ThumbnailPath)
            .HasColumnName("thumbnail_path")
            .HasMaxLength(1024);

        builder.Property(u => u.FileSizeBytes).HasColumnName("file_size_bytes").IsRequired();

        builder.Property(u => u.ContentType)
            .HasColumnName("content_type")
            .HasMaxLength(128)
            .IsRequired()
            .HasDefaultValue("image/jpeg");

        builder.Property(u => u.UploadedAt).HasColumnName("uploaded_at").IsRequired();

        builder.Property(u => u.ModerationStatus)
            .HasColumnName("moderation_status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(u => u.RejectionReason)
            .HasColumnName("rejection_reason")
            .HasMaxLength(1000);

        builder.Property(u => u.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(u => u.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasIndex(u => u.EventId)
            .HasDatabaseName("IX_guest_uploads_event_id");

        builder.HasIndex(u => u.SessionId)
            .HasDatabaseName("IX_guest_uploads_session_id");

        builder.HasIndex(u => new { u.EventId, u.ModerationStatus })
            .HasDatabaseName("IX_guest_uploads_event_moderation");

        builder.HasOne<Event>()
            .WithMany()
            .HasForeignKey(u => u.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<GuestUploadSession>()
            .WithMany()
            .HasForeignKey(u => u.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
