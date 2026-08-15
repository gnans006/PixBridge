using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.Deployment.Queries;

// ── Result ────────────────────────────────────────────────────────────────────

/// <summary>Full deployment status snapshot returned by <see cref="GetDeploymentStatusQuery"/>.</summary>
public sealed record DeploymentStatusResult(
    DeploymentStatus Deployment,
    string LanIpAddress,
    IReadOnlyList<string> AllLanIpAddresses,
    int ServerPort,
    int TotalEvents,
    int EventsWithQr,
    int EventsWithMissingQr,
    DateTimeOffset CheckedAt);

// ── Query ─────────────────────────────────────────────────────────────────────

/// <summary>
/// Returns a full deployment status snapshot: deployment mode, network identity,
/// event / QR summary, and reverse-proxy detection.
/// </summary>
/// <param name="RequestHeaders">
/// Optional request headers forwarded from the HTTP context for proxy detection.
/// </param>
public sealed record GetDeploymentStatusQuery(
    IEnumerable<KeyValuePair<string, string>>? RequestHeaders = null)
    : IRequest<Result<DeploymentStatusResult>>;

/// <summary>Handles <see cref="GetDeploymentStatusQuery"/>.</summary>
public sealed class GetDeploymentStatusQueryHandler(
    IApplicationSettingsRepository appSettingsRepo,
    INetworkInformationService networkSvc,
    IDeploymentInfoService deploymentInfoSvc,
    IEventRepository eventRepository)
    : IRequestHandler<GetDeploymentStatusQuery, Result<DeploymentStatusResult>>
{
    /// <inheritdoc />
    public async Task<Result<DeploymentStatusResult>> Handle(
        GetDeploymentStatusQuery request,
        CancellationToken cancellationToken)
    {
        var settings    = await appSettingsRepo.GetOrCreateDefaultAsync(cancellationToken);
        var networkInfo = networkSvc.GetCurrentNetworkInformation(settings.ServerPort);
        var deployment  = deploymentInfoSvc.Analyze(settings.PublicBaseUrl, request.RequestHeaders);

        var events        = await eventRepository.GetAllAsync(cancellationToken);
        var totalEvents   = events.Count;
        var eventsWithQr  = events.Count(e => !string.IsNullOrWhiteSpace(e.QrCodePath));
        var missingQr     = events.Count(e =>
            !e.IsDeleted &&
            !string.IsNullOrWhiteSpace(e.QrCodePath) &&
            !File.Exists(e.QrCodePath));

        return Result.Success(new DeploymentStatusResult(
            Deployment:            deployment,
            LanIpAddress:          networkInfo.PrimaryIpAddress,
            AllLanIpAddresses:     networkInfo.AllIpAddresses,
            ServerPort:            networkInfo.Port,
            TotalEvents:           totalEvents,
            EventsWithQr:          eventsWithQr,
            EventsWithMissingQr:   missingQr,
            CheckedAt:             DateTimeOffset.UtcNow));
    }
}
