using EventPhoto.Application.AiDiscovery.Commands;
using EventPhoto.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventPhoto.Worker.Services.AiDiscovery;

/// <summary>
/// Monitors the dead-letter queue and surfaces actionable metrics for the AI Studio dashboard.
/// Runs every 5 minutes — lightweight, no heavy processing.
///
/// <para>This service logs dead-letter queue depth so operators see alerts in structured logs
/// and the AI Studio dashboard can display queue health in real time.</para>
/// </summary>
public sealed class DeadLetterProcessorService : BackgroundService
{
    private const int CheckIntervalMinutes = 5;
    private const int DeadLetterAlertThreshold = 10;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DeadLetterProcessorService> _logger;

    public DeadLetterProcessorService(
        IServiceScopeFactory scopeFactory,
        ILogger<DeadLetterProcessorService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Dead Letter Processor Service starting.");

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(CheckIntervalMinutes));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await CheckDeadLetterQueueAsync(stoppingToken);
        }
    }

    private async Task CheckDeadLetterQueueAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var jobRepo = scope.ServiceProvider.GetRequiredService<IFaceProcessingJobRepository>();

        try
        {
            var depth = await jobRepo.GetQueueDepthAsync(cancellationToken);
            var (dlJobs, dlTotal) = await jobRepo.GetDeadLetteredPagedAsync(1, 1, cancellationToken);

            if (dlTotal >= DeadLetterAlertThreshold)
            {
                _logger.LogWarning(
                    "AI Discovery dead-letter queue has {Count} job(s). " +
                    "Review failed jobs in the AI Studio dashboard.",
                    dlTotal);
            }
            else if (dlTotal > 0)
            {
                _logger.LogInformation(
                    "AI Discovery dead-letter queue: {DLCount} job(s). Queue depth: {Depth}.",
                    dlTotal, depth);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking dead-letter queue.");
        }
    }
}
