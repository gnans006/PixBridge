using EventPhoto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventPhoto.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core fluent configuration for the <see cref="DownloadLog"/> entity.
/// </summary>
public sealed class DownloadLogConfiguration : IEntityTypeConfiguration<DownloadLog>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DownloadLog> builder)
    {
        builder.ToTable("download_logs");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(d => d.PhotoId)
            .HasColumnName("photo_id")
            .IsRequired();

        builder.Property(d => d.EventId)
            .HasColumnName("event_id")
            .IsRequired();

        builder.Property(d => d.IpAddress)
            .HasColumnName("ip_address")
            .HasMaxLength(50);

        builder.Property(d => d.UserAgent)
            .HasColumnName("user_agent")
            .HasMaxLength(1024);

        builder.Property(d => d.DownloadedAt)
            .HasColumnName("downloaded_at")
            .IsRequired();

        builder.Property(d => d.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(d => d.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasOne(d => d.Photo)
            .WithMany()
            .HasForeignKey(d => d.PhotoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.Event)
            .WithMany()
            .HasForeignKey(d => d.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => d.PhotoId);
        builder.HasIndex(d => d.EventId);
        builder.HasIndex(d => d.DownloadedAt);
    }
}
