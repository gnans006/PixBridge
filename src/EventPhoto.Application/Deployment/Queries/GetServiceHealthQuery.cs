using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EventPhoto.Application.Deployment.Queries;

// ── Result records ─────────────────────────────────────────────────────────────

/// <summary>Overall health summary returned by <see cref="GetServiceHealthQuery"/>.</summary>
public sealed record ServiceHealthResult(
    ComponentHealth Database,
    ComponentHealth AiService,
    ComponentHealth Storage,
    ComponentHealth QrService,
    DateTimeOffset CheckedAt);

/// <summary>Health status for a single infrastructure component.</summary>
public sealed record ComponentHealth(
    string Name,
    HealthStatus Status,
    long? ResponseMs,
    string? Detail);

/// <summary>Health state enumeration.</summary>
public enum HealthStatus { Healthy, Degraded, Offline }

// ── Query ─────────────────────────────────────────────────────────────────────

/// <summary>
/// Returns a real-time health check for all PixBridge infrastructure components:
/// PostgreSQL, the AI face-recognition service, local storage, and the QR pipeline.
/// </summary>
public sealed record GetServiceHealthQuery : IRequest<Result<ServiceHealthResult>>;

/// <summary>Handles <see cref="GetServiceHealthQuery"/>.</summary>
public sealed class GetServiceHealthQueryHandler(
    IEventRepository eventRepository,
    IAiServiceHealthChecker aiHealthChecker,
    ILogger<GetServiceHealthQueryHandler> logger)
    : IRequestHandler<GetServiceHealthQuery, Result<ServiceHealthResult>>
{
    /// <inheritdoc />
    public async Task<Result<ServiceHealthResult>> Handle(
        GetServiceHealthQuery request,
        CancellationToken cancellationToken)
    {
        var dbTask      = CheckDatabaseAsync(cancellationToken);
        var aiTask      = CheckAiServiceAsync(cancellationToken);
        var storageTask = CheckStorageAsync(cancellationToken);
        var qrTask      = CheckQrServiceAsync(cancellationToken);

        await Task.WhenAll(dbTask, aiTask, storageTask, qrTask);

        return Result.Success(new ServiceHealthResult(
            Database:   await dbTask,
            AiService:  await aiTask,
            Storage:    await storageTask,
            QrService:  await qrTask,
            CheckedAt:  DateTimeOffset.UtcNow));
    }

    // ── Component checks ───────────────────────────────────────────────────────

    private async Task<ComponentHealth> CheckDatabaseAsync(CancellationToken cancellationToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            // A lightweight query — count events. Forces a real round-trip to Postgres.
            await eventRepository.GetAllAsync(cancellationToken);
            sw.Stop();
            return new ComponentHealth("PostgreSQL", HealthStatus.Healthy, sw.ElapsedMilliseconds, null);
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogWarning(ex, "Database health check failed");
            return new ComponentHealth("PostgreSQL", HealthStatus.Offline, sw.ElapsedMilliseconds, ex.Message);
        }
    }

    private async Task<ComponentHealth> CheckAiServiceAsync(CancellationToken cancellationToken)
    {
        try
        {
            var (isHealthy, elapsedMs, detail) = await aiHealthChecker.CheckAsync(cancellationToken);
            var status = isHealthy ? HealthStatus.Healthy : HealthStatus.Degraded;
            return new ComponentHealth("AI Face Recognition", status, elapsedMs, detail);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "AI service health check failed (service may not be running)");
            return new ComponentHealth("AI Face Recognition", HealthStatus.Offline, null,
                "Service unreachable — ensure the face recognition service is running.");
        }
    }

    private Task<ComponentHealth> CheckStorageAsync(CancellationToken cancellationToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            // Check the drive where the application is running
            var appPath = AppContext.BaseDirectory;
            var drive   = new System.IO.DriveInfo(System.IO.Path.GetPathRoot(appPath)!);

            sw.Stop();

            // Warn if free space is below 2 GB
            const long warningThresholdBytes = 2L * 1024 * 1024 * 1024;
            var status = drive.AvailableFreeSpace < warningThresholdBytes
                ? HealthStatus.Degraded
                : HealthStatus.Healthy;

            var freeGb  = drive.AvailableFreeSpace  / (1024.0 * 1024 * 1024);
            var totalGb = drive.TotalSize            / (1024.0 * 1024 * 1024);

            return Task.FromResult(new ComponentHealth(
                "Storage",
                status,
                sw.ElapsedMilliseconds,
                $"{freeGb:F1} GB free of {totalGb:F1} GB total"));
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogWarning(ex, "Storage health check failed");
            return Task.FromResult(new ComponentHealth("Storage", HealthStatus.Degraded, sw.ElapsedMilliseconds, ex.Message));
        }
    }

    private async Task<ComponentHealth> CheckQrServiceAsync(CancellationToken cancellationToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var events = await eventRepository.GetAllAsync(cancellationToken);
            sw.Stop();

            // Count events that have a QR path recorded but the file is missing from disk
            var missingQr = events.Count(e =>
                !e.IsDeleted &&
                !string.IsNullOrWhiteSpace(e.QrCodePath) &&
                !System.IO.File.Exists(e.QrCodePath));

            var status = missingQr > 0 ? HealthStatus.Degraded : HealthStatus.Healthy;
            var detail = missingQr > 0
                ? $"{missingQr} event(s) have missing QR code files — use Regenerate All to fix."
                : null;

            return new ComponentHealth("QR Service", status, sw.ElapsedMilliseconds, detail);
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogWarning(ex, "QR service health check failed");
            return new ComponentHealth("QR Service", HealthStatus.Degraded, sw.ElapsedMilliseconds, ex.Message);
        }
    }
}
