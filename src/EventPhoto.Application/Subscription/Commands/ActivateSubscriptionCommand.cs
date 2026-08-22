using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Enums;
using EventPhoto.Domain.Exceptions;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.Subscription.Commands;

/// <summary>Activates the studio subscription with a valid license key.</summary>
public sealed record ActivateSubscriptionCommand(
    string LicenseKey,
    string StudioEmail) : IRequest<Result>;

/// <summary>Handles <see cref="ActivateSubscriptionCommand"/>.</summary>
public sealed class ActivateSubscriptionCommandHandler(
    ISubscriptionRepository repository,
    ILicenseKeyService licenseKeyService,
    IFingerprintService fingerprintService,
    IInstallationRegistryRepository installationRegistryRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ActivateSubscriptionCommand, Result>
{
    public async Task<Result> Handle(
        ActivateSubscriptionCommand request,
        CancellationToken cancellationToken)
    {
        // Validate and decode the license key — plan and duration come from the key payload,
        // never from the client request. This eliminates the "client sends ExpiresAt" security hole.
        var payload = licenseKeyService.ValidateAndDecode(request.LicenseKey);
        if (payload is null)
            return Result.Failure("Invalid or tampered license key. Please contact support.");

        // Compute machine fingerprint and fetch installation identity
        var fingerprintHash   = fingerprintService.ComputeHash();
        var registry          = await installationRegistryRepository.GetAsync(cancellationToken);
        var installationId    = registry?.InstallationId;

        var sub = await repository.GetOrCreateTrialAsync(cancellationToken);

        try
        {
            sub.Activate(
                request.LicenseKey,
                request.StudioEmail,
                payload.Plan,
                payload.DurationDays,
                installationId,
                fingerprintHash,
                integrityHash: null); // integrity hash computed inside the entity
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
