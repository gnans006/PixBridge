using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using SubscriptionEntity = EventPhoto.Domain.Entities.Subscription;
using MediatR;

namespace EventPhoto.Application.Subscription.Queries;

/// <summary>Returns the current subscription state, seeding a Trial if absent.</summary>
public sealed record GetSubscriptionQuery : IRequest<Result<SubscriptionEntity>>;

/// <summary>Handles <see cref="GetSubscriptionQuery"/>.</summary>
public sealed class GetSubscriptionQueryHandler(ISubscriptionRepository repository)
    : IRequestHandler<GetSubscriptionQuery, Result<SubscriptionEntity>>
{
    public async Task<Result<SubscriptionEntity>> Handle(
        GetSubscriptionQuery request,
        CancellationToken cancellationToken)
    {
        var sub = await repository.GetOrCreateTrialAsync(cancellationToken);
        return Result.Success<SubscriptionEntity>(sub);
    }
}
