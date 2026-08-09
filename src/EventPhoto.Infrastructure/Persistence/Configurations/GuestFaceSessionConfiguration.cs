using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventPhoto.Infrastructure.Persistence.Configurations;

/// <summary>EF Core fluent configuration for <see cref="GuestFaceSession"/>.</summary>
public sealed class GuestFaceSessionConfiguration : IEntityTypeConfiguration<GuestFaceSession>
{
    public void Configure(EntityTypeBuilder<GuestFaceSession> builder)
    {
        builder.ToTable("guest_face_sessions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(s => s.EventId)
            .HasColumnName("event_id")
            .IsRequired();

        builder.Property(s => s.SessionToken)
            .HasColumnName("session_token")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(s => s.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // pgvector vector(512) — selfie embedding used for the search
        builder.Property(s => s.SelfieEmbedding)
            .HasColumnName("selfie_embedding")
            .HasColumnType("vector(512)")
            .IsRequired();

        // SHA-256 hex hash of the raw selfie bytes (64 chars) for cache lookup
        builder.Property(s => s.SelfieHash)
            .HasColumnName("selfie_hash")
            .HasMaxLength(64);

        builder.Property(s => s.SearchStartedAt)
            .HasColumnName("search_started_at");

        builder.Property(s => s.SearchCompletedAt)
            .HasColumnName("search_completed_at");

        builder.Property(s => s.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(s => s.MatchCount)
            .HasColumnName("match_count")
            .IsRequired();

        builder.Property(s => s.SearchDurationMs)
            .HasColumnName("search_duration_ms")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(s => s.SelfieDeletedAt)
            .HasColumnName("selfie_deleted_at");

        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasOne(s => s.Event)
            .WithMany()
            .HasForeignKey(s => s.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.SessionToken)
            .IsUnique()
            .HasDatabaseName("IX_guest_face_sessions_token");

        builder.HasIndex(s => new { s.EventId, s.Status })
            .HasDatabaseName("IX_guest_face_sessions_event_status");

        builder.HasIndex(s => s.ExpiresAt)
            .HasDatabaseName("IX_guest_face_sessions_expires_at");

        // For retention service: sessions with embedding not yet purged
        builder.HasIndex(s => new { s.ExpiresAt, s.SelfieDeletedAt })
            .HasDatabaseName("IX_guest_face_sessions_retention")
            .HasFilter("selfie_deleted_at IS NULL");

        // Selfie hash index for cache lookup
        builder.HasIndex(s => new { s.EventId, s.SelfieHash })
            .HasDatabaseName("IX_guest_face_sessions_event_selfie_hash");
    }
}
