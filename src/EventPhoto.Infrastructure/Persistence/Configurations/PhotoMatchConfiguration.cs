using EventPhoto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventPhoto.Infrastructure.Persistence.Configurations;

/// <summary>EF Core fluent configuration for <see cref="PhotoMatch"/>.</summary>
public sealed class PhotoMatchConfiguration : IEntityTypeConfiguration<PhotoMatch>
{
    public void Configure(EntityTypeBuilder<PhotoMatch> builder)
    {
        builder.ToTable("photo_matches");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(m => m.SessionId)
            .HasColumnName("session_id")
            .IsRequired();

        builder.Property(m => m.PhotoId)
            .HasColumnName("photo_id")
            .IsRequired();

        builder.Property(m => m.SimilarityScore)
            .HasColumnName("similarity_score")
            .IsRequired();

        builder.Property(m => m.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(m => m.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasOne(m => m.Session)
            .WithMany()
            .HasForeignKey(m => m.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Photo)
            .WithMany()
            .HasForeignKey(m => m.PhotoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => new { m.SessionId, m.SimilarityScore })
            .IsDescending(false, true)
            .HasDatabaseName("IX_photo_matches_session_score");

        builder.HasIndex(m => new { m.SessionId, m.PhotoId })
            .IsUnique()
            .HasDatabaseName("IX_photo_matches_session_photo");
    }
}
