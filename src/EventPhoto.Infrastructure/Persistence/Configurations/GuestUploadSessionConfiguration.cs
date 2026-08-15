using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventPhoto.Infrastructure.Persistence.Configurations;

/// <summary>EF Core fluent configuration for <see cref="GuestUploadSession"/>.</summary>
public sealed class GuestUploadSessionConfiguration : IEntityTypeConfiguration<GuestUploadSession>
{
    public void Configure(EntityTypeBuilder<GuestUploadSession> builder)
    {
        builder.ToTable("guest_upload_sessions");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(s => s.EventId).HasColumnName("event_id").IsRequired();

        builder.Property(s => s.SessionCode)
            .HasColumnName("session_code")
            .HasMaxLength(8)
            .IsRequired();

        builder.Property(s => s.Title)
            .HasColumnName("title")
            .HasMaxLength(200);

        builder.Property(s => s.PhotoCount).HasColumnName("photo_count").IsRequired();

        builder.Property(s => s.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(s => s.ClosedAt).HasColumnName("closed_at");

        builder.Property(s => s.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasIndex(s => s.SessionCode)
            .IsUnique()
            .HasDatabaseName("IX_guest_upload_sessions_code");

        builder.HasIndex(s => s.EventId)
            .HasDatabaseName("IX_guest_upload_sessions_event_id");

        builder.HasOne<Event>()
            .WithMany()
            .HasForeignKey(s => s.EventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
