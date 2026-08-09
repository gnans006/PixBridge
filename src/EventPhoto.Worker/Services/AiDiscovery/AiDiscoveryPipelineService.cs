using EventPhoto.Application.AiDiscovery.Commands;
using EventPhoto.Domain.Enums;
using EventPhoto.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace EventPhoto.Worker.Services.AiDiscovery;

/// <summary>
/// Priority-aware background pipeline for the AI Discovery Engine ("Find My Photos™").
/// Operates independently of the Gallery Pipeline — gallery visibility is NEVER blocked by this service.
///
/// <para>Pipeline stages per photo:</para>
/// <list type="number">
///   <item>Pending → Detecting (InsightFace)</item>
///   <item>Detecting → QualityChecking</item>
///   <item>QualityChecking → Embedding (ArcFace)</item>
///   <item>Embedding → Indexing (pgvector HNSW)</item>
///   <item>Indexing → Completed</item>
/// </list>
///
/// <para>Failures trigger exponential back-off retry. After <c>MaxRetries</c>
/// the job is promoted to the dead-letter queue for operator review.</para>
/// </summary>
public sealed class AiDiscoveryPipelineService : BackgroundService
{
    private const int PendingBatchSize = 8;
    private const int RetryBatchSize = 4;
    private const int PollIntervalSeconds = 10;
    private const int ChannelCapacity = 32;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AiDiscoveryPipelineService> _logger;

    // Bounded channel acts as the in-memory priority queue between polling and processing
    private readonly Channel<Guid> _jobChannel;

    public AiDiscoveryPipelineService(
        IServiceScopeFactory scopeFactory,
        ILogger<AiDiscoveryPipelineService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _jobChannel = Channel.CreateBounded<Guid>(new BoundedChannelOptions(ChannelCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = false,
            SingleReader = false
        });
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AI Discovery Pipeline starting.");

        // Run the poller and the processor concurrently
        await Task.WhenAll(
            RunPollerAsync(stoppingToken),
            RunProcessorAsync(stoppingToken));
    }

    // ── Poller: loads pending/retry-eligible jobs into the channel ────────────

    private async Task RunPollerAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(PollIntervalSeconds));
        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            await EnqueuePendingJobsAsync(cancellationToken);
            await EnqueueRetryEligibleJobsAsync(cancellationToken);
        }
    }

    private async Task EnqueuePendingJobsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var jobRepo = scope.ServiceProvider.GetRequiredService<IFaceProcessingJobRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        List<Domain.Entities.FaceProcessingJob> jobs;
        try
        {
            jobs = await jobRepo.GetPendingBatchAsync(PendingBatchSize, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query pending AI jobs.");
            return;
        }

        if (jobs.Count == 0) return;

        foreach (var job in jobs)
        {
            job.MarkQueued();
            await jobRepo.UpdateAsync(job, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var job in jobs)
        {
            await _jobChannel.Writer.WriteAsync(job.Id, cancellationToken);
            _logger.LogDebug("Enqueued AI job {JobId} for photo {PhotoId}.", job.Id, job.PhotoId);
        }
    }

    private async Task EnqueueRetryEligibleJobsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var jobRepo = scope.ServiceProvider.GetRequiredService<IFaceProcessingJobRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        List<Domain.Entities.FaceProcessingJob> jobs;
        try
        {
            jobs = await jobRepo.GetRetryEligibleBatchAsync(RetryBatchSize, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query retry-eligible AI jobs.");
            return;
        }

        if (jobs.Count == 0) return;

        foreach (var job in jobs)
        {
            // Reset to Pending so ProcessAiDiscoveryJobCommand re-runs the full pipeline
            job.MarkQueued();
            await jobRepo.UpdateAsync(job, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var job in jobs)
        {
            await _jobChannel.Writer.WriteAsync(job.Id, cancellationToken);
            _logger.LogInformation(
                "Retry AI job {JobId} enqueued (attempt {Attempt}).", job.Id, job.RetryCount);
        }
    }

    // ── Processor: consumes the channel and runs the pipeline per job ─────────

    private async Task RunProcessorAsync(CancellationToken cancellationToken)
    {
        // Process up to 3 jobs concurrently for throughput
        var semaphore = new SemaphoreSlim(3, 3);

        await foreach (var jobId in _jobChannel.Reader.ReadAllAsync(cancellationToken))
        {
            await semaphore.WaitAsync(cancellationToken);
            _ = Task.Run(async () =>
            {
                try
                {
                    await ProcessJobAsync(jobId, cancellationToken);
                }
                finally
                {
                    semaphore.Release();
                }
            }, cancellationToken);
        }
    }

    private async Task ProcessJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        try
        {
            var result = await mediator.Send(new ProcessAiDiscoveryJobCommand(jobId), cancellationToken);
            if (result.IsFailure)
            {
                _logger.LogWarning(
                    "AI Discovery job {JobId} returned failure: {Error}", jobId, result.Error);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception processing AI Discovery job {JobId}.", jobId);
        }
    }
}
