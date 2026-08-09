using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pgvector.EntityFrameworkCore;

namespace EventPhoto.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core fluent configuration for <see cref="FaceEmbedding"/>.
/// Uses the pgvector extension to store 512-dimensional ArcFace embeddings
/// with an HNSW cosine-similarity index for sub-millisecond nearest-neighbour search.
/// </summary>
public sealed class FaceEmbeddingConfiguration : IEntityTypeConfiguration<FaceEmbedding>
{
    public void Configure(EntityTypeBuilder<FaceEmbedding> builder)
    {
        builder.ToTable("face_embeddings");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(f => f.EventId)
            .HasColumnName("event_id")
            .IsRequired();

        builder.Property(f => f.PhotoId)
            .HasColumnName("photo_id")
            .IsRequired();

        // pgvector vector(512) column — requires Pgvector.EntityFrameworkCore
        builder.Property(f => f.Embedding)
            .HasColumnName("embedding")
            .HasColumnType("vector(512)")
            .IsRequired();

        builder.Property(f => f.BoundingBox)
            .HasColumnName("bounding_box")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(f => f.Confidence)
            .HasColumnName("confidence")
            .IsRequired();

        // ── Quality fields ────────────────────────────────────────────────────

        builder.Property(f => f.QualityScore)
            .HasColumnName("quality_score")
            .IsRequired()
            .HasDefaultValue(50f);

        builder.Property(f => f.QualityTier)
            .HasColumnName("quality_tier")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue(QualityTier.Medium);

        builder.Property(f => f.FaceCountInPhoto)
            .HasColumnName("face_count_in_photo")
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(f => f.EmbeddingVersion)
            .HasColumnName("embedding_version")
            .HasMaxLength(50)
            .IsRequired()
            .HasDefaultValue("arcface-512-v1");

        // ─────────────────────────────────────────────────────────────────────

        builder.Property(f => f.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(f => f.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasOne(f => f.Photo)
            .WithMany()
            .HasForeignKey(f => f.PhotoId)
            .OnDelete(DeleteBehavior.Cascade);

        // Composite index for event-scoped nearest-neighbour queries
        builder.HasIndex(f => f.EventId).HasDatabaseName("IX_face_embeddings_event_id");
        builder.HasIndex(f => f.PhotoId).HasDatabaseName("IX_face_embeddings_photo_id");

        // Quality-filtered index — searches can exclude Low-quality embeddings
        builder.HasIndex(f => new { f.EventId, f.QualityTier })
            .HasDatabaseName("IX_face_embeddings_event_quality_tier");

        // HNSW cosine-similarity index is created via raw SQL in the migration
        // because EF Core does not natively support vector index configuration.
    }
}

