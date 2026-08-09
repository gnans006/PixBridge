using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventPhoto.Infrastructure.Persistence.Configurations;

/// <summary>EF Core fluent configuration for <see cref="AiSearchAnalytics"/>.</summary>
public sealed class AiSearchAnalyticsConfiguration : IEntityTypeConfiguration<AiSearchAnalytics>
{
    public void Configure(EntityTypeBuilder<AiSearchAnalytics> builder)
    {
        builder.ToTable("ai_search_analytics");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(a => a.EventId).HasColumnName("event_id").IsRequired();
        builder.Property(a => a.SessionId).HasColumnName("session_id").IsRequired();
        builder.Property(a => a.MatchesFound).HasColumnName("matches_found").IsRequired();
        builder.Property(a => a.SearchDurationMs).HasColumnName("search_duration_ms").IsRequired();
        builder.Property(a => a.WasSuccessful).HasColumnName("was_successful").IsRequired();

        builder.Property(a => a.TopMatchCategory)
            .HasColumnName("top_match_category")
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(a => a.TopSimilarityScore).HasColumnName("top_similarity_score");

        builder.Property(a => a.EmbeddingVersion)
            .HasColumnName("embedding_version")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.SearchedAt).HasColumnName("searched_at").IsRequired();

        builder.Property(a => a.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasOne(a => a.Event)
            .WithMany()
            .HasForeignKey(a => a.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // Time-series queries: searched_at is the primary access pattern
        builder.HasIndex(a => a.SearchedAt)
            .HasDatabaseName("IX_ai_search_analytics_searched_at");

        // Per-event analytics queries
        builder.HasIndex(a => new { a.EventId, a.SearchedAt })
            .HasDatabaseName("IX_ai_search_analytics_event_searched_at");
    }
}
