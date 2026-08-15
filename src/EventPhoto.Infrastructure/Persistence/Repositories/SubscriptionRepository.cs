using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EventPhoto.Infrastructure.Persistence.Repositories;

/// <summary>PostgreSQL-backed implementation of <see cref="ISubscriptionRepository"/>.</summary>
public sealed class SubscriptionRepository(AppDbContext context) : ISubscriptionRepository
{
    public Task<Subscription?> GetAsync(CancellationToken ct = default)
        => context.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == Subscription.SingletonId, ct);

    public async Task<Subscription> GetOrCreateTrialAsync(CancellationToken ct = default)
    {
        var existing = await GetAsync(ct);
        if (existing is not null)
            return existing;

        var trial = Subscription.CreateTrial();
        await context.Subscriptions.AddAsync(trial, ct);
        await context.SaveChangesAsync(ct);
        return trial;
    }

    public Task UpdateAsync(Subscription subscription, CancellationToken ct = default)
    {
        context.Subscriptions.Update(subscription);
        return Task.CompletedTask;
    }
}
