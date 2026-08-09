using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EventPhoto.Application.AiDiscovery.Commands;

/// <summary>
/// Promotes a dead-lettered <see cref="Domain.Entities.FaceProcessingJob"/> back to
/// <c>Pending</c> for retry, resetting the retry count and back-off schedule.
/// </summary>
public sealed record RetryDeadLetterJobCommand(Guid JobId) : IRequest<Result>;

/// <summary>Handles <see cref="RetryDeadLetterJobCommand"/>.</summary>
public sealed class RetryDeadLetterJobCommandHandler(
    IFaceProcessingJobRepository jobRepository,
    IUnitOfWork unitOfWork,
    ILogger<RetryDeadLetterJobCommandHandler> logger)
    : IRequestHandler<RetryDeadLetterJobCommand, Result>
{
    public async Task<Result> Handle(
        RetryDeadLetterJobCommand request,
        CancellationToken cancellationToken)
    {
        var job = await jobRepository.GetByIdAsync(request.JobId, cancellationToken);
        if (job is null)
            return Result.Failure($"Job '{request.JobId}' not found.");

        job.MarkRetryFromDeadLetter();
        await jobRepository.UpdateAsync(job, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Dead-letter job {JobId} promoted back to Pending for retry.", request.JobId);
        return Result.Success();
    }
}

/// <summary>
/// Permanently ignores a dead-lettered job — marks it as <c>Ignored</c> so it
/// no longer appears in the dead-letter queue.
/// </summary>
public sealed record IgnoreDeadLetterJobCommand(Guid JobId) : IRequest<Result>;

/// <summary>Handles <see cref="IgnoreDeadLetterJobCommand"/>.</summary>
public sealed class IgnoreDeadLetterJobCommandHandler(
    IFaceProcessingJobRepository jobRepository,
    IUnitOfWork unitOfWork,
    ILogger<IgnoreDeadLetterJobCommandHandler> logger)
    : IRequestHandler<IgnoreDeadLetterJobCommand, Result>
{
    public async Task<Result> Handle(
        IgnoreDeadLetterJobCommand request,
        CancellationToken cancellationToken)
    {
        var job = await jobRepository.GetByIdAsync(request.JobId, cancellationToken);
        if (job is null)
            return Result.Failure($"Job '{request.JobId}' not found.");

        job.MarkIgnored();
        await jobRepository.UpdateAsync(job, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Dead-letter job {JobId} marked as Ignored by operator.", request.JobId);
        return Result.Success();
    }
}
