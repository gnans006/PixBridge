using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EventPhoto.Application.Subscription.Commands;

/// <summary>
/// Grants the one-time 15-day trial extension to the studio's subscription.
///
/// <para>Business rules (enforced by the domain):</para>
/// <list type="bullet">
///   <item>Extension can only be applied once per installation.</item>
///   <item>Extension is only valid while the subscription state is <c>Trial</c>.</item>
/// </list>
/// </summary>
public sealed record ExtendTrialCommand : IRequest<Result>;

/// <summary>Handles <see cref="ExtendTrialCommand"/>.</summary>
public sealed class ExtendTrialCommandHandler(
    ISubscriptionRepository subscriptionRepository,
    IUnitOfWork unitOfWork,
    ILogger<ExtendTrialCommandHandler> logger)
    : IRequestHandler<ExtendTrialCommand, Result>
{
    public async Task<Result> Handle(ExtendTrialCommand request, CancellationToken cancellationToken)
    {
        var subscription = await subscriptionRepository.GetAsync(cancellationToken);
        if (subscription is null)
        {
            logger.LogWarning("ExtendTrial: subscription record not found.");
            return Result.Failure("Subscription record not found.");
        }

        // Domain enforces: only once, only in Trial state — throws DomainException on violation
        subscription.ExtendTrial();

        await subscriptionRepository.UpdateAsync(subscription, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Trial extended to ExtendedTrial for subscription {SubscriptionId}.",
            subscription.Id);

        return Result.Success();
    }
}
