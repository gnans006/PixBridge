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
    }
}
