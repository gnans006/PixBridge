using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Contracts.Responses.Events;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.Events.Queries;

/// <summary>Returns the consolidated workspace data for a single event.</summary>
/// <param name="EventId">The event identifier.</param>
public sealed record GetEventWorkspaceQuery(Guid EventId)
    : IRequest<Result<EventWorkspaceResponse>>;

/// <summary>Handles <see cref="GetEventWorkspaceQuery"/>.</summary>
public sealed class GetEventWorkspaceQueryHandler(
    IEventRepository eventRepository,
    IDownloadLogRepository downloadLogRepository,
    IWatermarkConfigurationRepository watermarkRepository,
    IFileStorageService fileStorageService)
    : IRequestHandler<GetEventWorkspaceQuery, Result<EventWorkspaceResponse>>
{
    /// <inheritdoc />
    public async Task<Result<EventWorkspaceResponse>> Handle(
        GetEventWorkspaceQuery request,
        CancellationToken cancellationToken)
    {
        var eventEntity = await eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (eventEntity is null)
        {
            return Result.Failure<EventWorkspaceResponse>($"Event '{request.EventId}' was not found.");
        }

        var totalDownloads = await downloadLogRepository.GetDownloadCountByEventAsync(
            request.EventId, cancellationToken);

        var watermarkConfig = await watermarkRepository.GetByEventIdAsync(
            request.EventId, cancellationToken);

        var liveSizeBytes = fileStorageService.GetFolderSize(eventEntity.WatchFolder);
        var sizeHuman = FormatBytes(liveSizeBytes > 0 ? liveSizeBytes : eventEntity.TotalSizeBytes);

        return Result.Success(new EventWorkspaceResponse(
            eventEntity.Id,
            eventEntity.Name,
            eventEntity.Description,
            eventEntity.EventType.ToString(),
            eventEntity.EventDate,
            eventEntity.VenueName,
            eventEntity.ClientName,
            eventEntity.WatchFolder,
            eventEntity.ThumbnailFolder,
            eventEntity.QrCodeUrl,
            eventEntity.IsActive,
            eventEntity.PhotoCount,
            liveSizeBytes > 0 ? liveSizeBytes : eventEntity.TotalSizeBytes,
            sizeHuman,
            totalDownloads,
            eventEntity.CreatedAt,
            eventEntity.GalleryRecentCount,
            eventEntity.AllowGalleryBrowsing,
            eventEntity.AllowFaceSearch,
            eventEntity.RestrictDownloadsToMatchedPhotos,
            eventEntity.EnableFaceRecognition,
            eventEntity.FaceMatchThreshold,
            watermarkConfig?.Enabled ?? false));
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
