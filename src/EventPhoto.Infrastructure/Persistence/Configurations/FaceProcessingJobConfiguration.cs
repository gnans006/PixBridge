using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventPhoto.Infrastructure.Persistence.Configurations;

/// <summary>EF Core fluent configuration for <see cref="FaceProcessingJob"/>.</summary>
public sealed class FaceProcessingJobConfiguration : IEntityTypeConfiguration<FaceProcessingJob>
{
    public void Configure(EntityTypeBuilder<FaceProcessingJob> builder)
    {
        builder.ToTable("face_processing_jobs");

        builder.HasKey(j => j.Id);
        builder.Property(j => j.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(j => j.EventId).HasColumnName("event_id").IsRequired();
        builder.Property(j => j.PhotoId).HasColumnName("photo_id").IsRequired();

        builder.Property(j => j.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(j => j.RetryCount).HasColumnName("retry_count").IsRequired();

        builder.Property(j => j.LastError)
            .HasColumnName("last_error")
            .HasMaxLength(2000);

        builder.Property(j => j.FailureType)
            .HasColumnName("failure_type")
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(j => j.NextRetryAt).HasColumnName("next_retry_at");
        builder.Property(j => j.StartedAt).HasColumnName("started_at");
        builder.Property(j => j.CompletedAt).HasColumnName("completed_at");

        builder.Property(j => j.Priority)
            .HasColumnName("priority")
            .IsRequired()
            .HasDefaultValue(2);

        builder.Property(j => j.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(j => j.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasOne(j => j.Event)
            .WithMany()
            .HasForeignKey(j => j.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(j => j.Photo)
            .WithMany()
            .HasForeignKey(j => j.PhotoId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for pipeline polling queries
        builder.HasIndex(j => new { j.Status, j.Priority, j.CreatedAt })
            .HasDatabaseName("IX_face_processing_jobs_pending_priority")
            .HasFilter("status IN ('Pending', 'Queued')");

        builder.HasIndex(j => new { j.Status, j.NextRetryAt })
            .HasDatabaseName("IX_face_processing_jobs_retry_eligible")
            .HasFilter("status = 'Failed'");

        builder.HasIndex(j => new { j.EventId, j.Status })
            .HasDatabaseName("IX_face_processing_jobs_event_status");

        builder.HasIndex(j => j.PhotoId)
            .HasDatabaseName("IX_face_processing_jobs_photo_id");
    }
}
