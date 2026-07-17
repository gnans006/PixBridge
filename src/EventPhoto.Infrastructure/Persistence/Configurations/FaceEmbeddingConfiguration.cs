using EventPhoto.Domain.Entities;
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

        // HNSW cosine-similarity index is created via raw SQL in the migration
        // because EF Core does not natively support vector index configuration.
    }
}
