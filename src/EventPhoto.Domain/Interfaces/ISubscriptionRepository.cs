using EventPhoto.Domain.Entities;

namespace EventPhoto.Domain.Interfaces;

/// <summary>Repository contract for the <see cref="Subscription"/> singleton aggregate.</summary>
public interface ISubscriptionRepository
{
    /// <summary>Returns the subscription record, or <see langword="null"/> if not yet seeded.</summary>
    Task<Subscription?> GetAsync(CancellationToken ct = default);

    /// <summary>Returns the subscription, creating and persisting a default trial record if absent.</summary>
    Task<Subscription> GetOrCreateTrialAsync(CancellationToken ct = default);

    /// <summary>Marks the subscription as modified.</summary>
    Task UpdateAsync(Subscription subscription, CancellationToken ct = default);
}
