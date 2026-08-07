using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Contracts.Responses.Events;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.Events.Queries;

/// <summary>Returns storage metrics for a single event.</summary>
/// <param name="EventId">The event identifier.</param>
public sealed record GetStorageMetricsQuery(Guid EventId)
    : IRequest<Result<StorageMetricsResponse>>;

/// <summary>Handles <see cref="GetStorageMetricsQuery"/>.</summary>
public sealed class GetStorageMetricsQueryHandler(
    IEventRepository eventRepository,
    IFileStorageService fileStorageService)
    : IRequestHandler<GetStorageMetricsQuery, Result<StorageMetricsResponse>>
{
    /// <inheritdoc />
    public async Task<Result<StorageMetricsResponse>> Handle(
        GetStorageMetricsQuery request,
        CancellationToken cancellationToken)
    {
        var eventEntity = await eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (eventEntity is null)
        {
            return Result.Failure<StorageMetricsResponse>($"Event '{request.EventId}' was not found.");
        }

        var sizeBytes = fileStorageService.GetFolderSize(eventEntity.WatchFolder);
        if (sizeBytes <= 0)
        {
            sizeBytes = eventEntity.TotalSizeBytes;
        }

        var thumbnailSizeBytes = fileStorageService.GetFolderSize(eventEntity.ThumbnailFolder);

        return Result.Success(new StorageMetricsResponse(
            eventEntity.Id,
            eventEntity.WatchFolder,
            eventEntity.ThumbnailFolder,
            sizeBytes + thumbnailSizeBytes,
            FormatBytes(sizeBytes + thumbnailSizeBytes),
            eventEntity.PhotoCount,
            eventEntity.PhotoCount));
    }

    private static string FormatBytes(long bytes) => bytes switch
    {
        <= 0 => "0 B",
        < 1024 => $"{bytes} B",
        < 1_048_576 => $"{bytes / 1024.0:F1} KB",
        < 1_073_741_824 => $"{bytes / 1_048_576.0:F1} MB",
        _ => $"{bytes / 1_073_741_824.0:F2} GB",
    };
}
