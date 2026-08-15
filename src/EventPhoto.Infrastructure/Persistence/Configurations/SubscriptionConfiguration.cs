using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventPhoto.Infrastructure.Persistence.Configurations;

/// <summary>EF Core fluent configuration for the <see cref="Subscription"/> singleton.</summary>
public sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("subscriptions");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(s => s.Plan)
            .HasColumnName("plan")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired()
            .HasDefaultValue(SubscriptionPlan.Trial);

        builder.Property(s => s.State)
            .HasColumnName("state")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue(SubscriptionState.Trial);

        builder.Property(s => s.LicenseKey)
            .HasColumnName("license_key")
            .HasMaxLength(256);

        builder.Property(s => s.StudioEmail)
            .HasColumnName("studio_email")
            .HasMaxLength(256);

        builder.Property(s => s.ActivatedAt).HasColumnName("activated_at");
        builder.Property(s => s.ExpiresAt).HasColumnName("expires_at");
        builder.Property(s => s.GracePeriodEndsAt).HasColumnName("grace_period_ends_at");

        builder.Property(s => s.MaxEvents).HasColumnName("max_events").IsRequired();
        builder.Property(s => s.MaxUsersPerStudio).HasColumnName("max_users_per_studio").IsRequired();

        builder.Property(s => s.Notes).HasColumnName("notes").HasMaxLength(2000);

        builder.Property(s => s.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").IsRequired();
    }
}
