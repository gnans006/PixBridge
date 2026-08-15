using EventPhoto.Domain.Common;
using EventPhoto.Domain.Enums;
using EventPhoto.Domain.Exceptions;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.Subscription.Commands;

/// <summary>Activates the studio subscription with a valid license key.</summary>
public sealed record ActivateSubscriptionCommand(
    string LicenseKey,
    string StudioEmail,
    string Plan,
    DateTimeOffset ExpiresAt) : IRequest<Result>;

/// <summary>Handles <see cref="ActivateSubscriptionCommand"/>.</summary>
public sealed class ActivateSubscriptionCommandHandler(
    ISubscriptionRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ActivateSubscriptionCommand, Result>
{
    public async Task<Result> Handle(
        ActivateSubscriptionCommand request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<SubscriptionPlan>(request.Plan, ignoreCase: true, out var plan))
            return Result.Failure($"Unknown subscription plan: {request.Plan}");

        var sub = await repository.GetOrCreateTrialAsync(cancellationToken);

        try
        {
            sub.Activate(request.LicenseKey, request.StudioEmail, plan, request.ExpiresAt);
        }
        catch (DomainException ex)
        {
            return Result.Failure(ex.Message);
        }

        await repository.UpdateAsync(sub, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
