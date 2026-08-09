using EventPhoto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pgvector.EntityFrameworkCore;

namespace EventPhoto.Infrastructure.Persistence.Configurations;

/// <summary>EF Core fluent configuration for <see cref="FaceCluster"/>.</summary>
public sealed class FaceClusterConfiguration : IEntityTypeConfiguration<FaceCluster>
{
    public void Configure(EntityTypeBuilder<FaceCluster> builder)
    {
        builder.ToTable("face_clusters");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(c => c.EventId).HasColumnName("event_id").IsRequired();

        builder.Property(c => c.RepresentativeEmbedding)
            .HasColumnName("representative_embedding")
            .HasColumnType("vector(512)")
            .IsRequired();

        builder.Property(c => c.PhotoCount).HasColumnName("photo_count").IsRequired();

        builder.Property(c => c.Label)
            .HasColumnName("label")
            .HasMaxLength(100);

        builder.Property(c => c.AverageQualityScore)
            .HasColumnName("average_quality_score")
            .IsRequired();

        builder.Property(c => c.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasOne(c => c.Event)
            .WithMany()
            .HasForeignKey(c => c.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.EventId)
            .HasDatabaseName("IX_face_clusters_event_id");

        // HNSW index on cluster centroids for fast cross-cluster search
        // Created via raw SQL in the migration
    }
}
