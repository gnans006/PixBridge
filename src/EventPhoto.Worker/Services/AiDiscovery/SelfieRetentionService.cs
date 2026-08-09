using EventPhoto.Application.AiDiscovery.Commands;
using EventPhoto.Application.FaceSearch.Commands;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventPhoto.Worker.Services.AiDiscovery;

/// <summary>
/// Enforces the guest selfie privacy retention policy.
/// Purges stored selfie embeddings from sessions that have been expired
/// for longer than the configured retention period (default: 24 hours).
///
/// <para>Also handles session expiry marking and general cleanup.</para>
/// </summary>
public sealed class SelfieRetentionService : BackgroundService
{
    private const int CheckIntervalMinutes = 30;
    private const int DefaultRetentionHours = 24;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SelfieRetentionService> _logger;

    public SelfieRetentionService(
        IServiceScopeFactory scopeFactory,
        ILogger<SelfieRetentionService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Selfie Retention Service starting (policy: {Hours}h).", DefaultRetentionHours);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(CheckIntervalMinutes));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunRetentionCycleAsync(stoppingToken);
        }
    }

    private async Task RunRetentionCycleAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Purge expired selfie embeddings (privacy)
        try
        {
            var purgeResult = await mediator.Send(
                new PurgeExpiredSelfiesCommand(DefaultRetentionHours), cancellationToken);

            if (purgeResult.IsSuccess && purgeResult.Value > 0)
            {
                _logger.LogInformation(
                    "Selfie retention: purged {Count} selfie embedding(s).", purgeResult.Value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during selfie embedding purge.");
        }

        // Expire stale sessions
        try
        {
            var expireResult = await mediator.Send(new ExpireFaceSessionsCommand(), cancellationToken);
            if (expireResult.IsSuccess && expireResult.Value > 0)
            {
                _logger.LogInformation(
                    "Session retention: expired {Count} guest session(s).", expireResult.Value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during face session expiry.");
        }
    }
}
