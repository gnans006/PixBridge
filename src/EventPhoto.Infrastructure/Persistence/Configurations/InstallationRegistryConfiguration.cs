using EventPhoto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventPhoto.Infrastructure.Persistence.Configurations;

/// <summary>EF Core fluent configuration for the <see cref="InstallationRegistry"/> singleton.</summary>
public sealed class InstallationRegistryConfiguration : IEntityTypeConfiguration<InstallationRegistry>
{
    public void Configure(EntityTypeBuilder<InstallationRegistry> builder)
    {
        builder.ToTable("installation_registry");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(r => r.InstallationId)
            .HasColumnName("installation_id")
            .IsRequired();

        builder.Property(r => r.MachineFingerprintHash)
            .HasColumnName("machine_fingerprint_hash")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(r => r.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(r => r.LastValidatedAtUtc)
            .HasColumnName("last_validated_at_utc")
            .IsRequired();

        builder.Property(r => r.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at").IsRequired();
    }
}
