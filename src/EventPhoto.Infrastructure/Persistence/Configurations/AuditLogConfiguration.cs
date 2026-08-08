using EventPhoto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventPhoto.Infrastructure.Persistence.Configurations;

/// <summary>EF Core fluent configuration for the <see cref="AuditLog"/> entity.</summary>
public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(a => a.UserId)
            .HasColumnName("user_id");

        builder.Property(a => a.ActorName)
            .HasColumnName("actor_name")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(a => a.EntityType)
            .HasColumnName("entity_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.EntityId)
            .HasColumnName("entity_id")
            .HasMaxLength(256);

        builder.Property(a => a.Action)
            .HasColumnName("action")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.Description)
            .HasColumnName("description")
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(a => a.Timestamp)
            .HasColumnName("timestamp")
            .IsRequired();

        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(a => a.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(a => a.Timestamp);
        builder.HasIndex(a => new { a.EntityType, a.EntityId });
        builder.HasIndex(a => a.UserId);
    }
}
