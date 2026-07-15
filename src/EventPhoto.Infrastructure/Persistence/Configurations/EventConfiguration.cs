using EventPhoto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventPhoto.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core fluent configuration for the <see cref="Event"/> aggregate.
/// </summary>
public sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasMaxLength(2000);

        builder.Property(e => e.EventType)
            .HasColumnName("event_type")
            .IsRequired();

        builder.Property(e => e.EventDate)
            .HasColumnName("event_date")
            .IsRequired();

        builder.Property(e => e.VenueName)
            .HasColumnName("venue_name")
            .HasMaxLength(256);

        builder.Property(e => e.ClientName)
            .HasColumnName("client_name")
            .HasMaxLength(256);

        builder.Property(e => e.WatchFolder)
            .HasColumnName("watch_folder")
            .HasMaxLength(1024)
            .IsRequired();

        builder.Property(e => e.ThumbnailFolder)
            .HasColumnName("thumbnail_folder")
            .HasMaxLength(1024)
            .IsRequired();

        builder.Property(e => e.QrCodePath)
            .HasColumnName("qr_code_path")
            .HasMaxLength(1024);

        builder.Property(e => e.QrCodeUrl)
            .HasColumnName("qr_code_url")
            .HasMaxLength(2048);

        builder.Property(e => e.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(e => e.IsDeleted)
            .HasColumnName("is_deleted")
            .IsRequired();

        builder.Property(e => e.PhotoCount)
            .HasColumnName("photo_count")
            .IsRequired();

        builder.Property(e => e.GalleryRecentCount)
            .HasColumnName("gallery_recent_count");

        builder.Property(e => e.TotalSizeBytes)
            .HasColumnName("total_size_bytes")
            .IsRequired();

        builder.Property(e => e.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Map the private _photos backing field for the IReadOnlyCollection<Photo> navigation.
        builder.HasMany(e => e.Photos)
            .WithOne(p => p.Event)
            .HasForeignKey(p => p.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.Photos)
            .HasField("_photos")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(e => e.IsDeleted);
        builder.HasIndex(e => new { e.IsActive, e.IsDeleted });
    }
}
