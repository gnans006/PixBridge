using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Contracts.Responses.Statistics;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.Statistics.Queries;

/// <summary>Returns the most active event for the spotlight section of the dashboard.</summary>
public sealed record GetEventSpotlightQuery : IRequest<Result<EventSpotlightResponse?>>;

/// <summary>Handles <see cref="GetEventSpotlightQuery"/>.</summary>
public sealed class GetEventSpotlightQueryHandler(
    IEventRepository eventRepository,
    IPhotoRepository photoRepository,
    IDownloadLogRepository downloadLogRepository,
    IWatermarkConfigurationRepository watermarkRepository,
    IFileStorageService fileStorageService)
    : IRequestHandler<GetEventSpotlightQuery, Result<EventSpotlightResponse?>>
{
    /// <inheritdoc />
    public async Task<Result<EventSpotlightResponse?>> Handle(
        GetEventSpotlightQuery request,
        CancellationToken cancellationToken)
    {
        var allEvents = await eventRepository.GetAllAsync(cancellationToken);

        // Pick the most active event: most photos among active events, fallback to most recent
        var spotlight = allEvents
            .Where(e => e.IsActive)
            .OrderByDescending(e => e.PhotoCount)
            .ThenByDescending(e => e.CreatedAt)
            .FirstOrDefault()
            ?? allEvents.OrderByDescending(e => e.CreatedAt).FirstOrDefault();

        if (spotlight is null)
            return Result.Success<EventSpotlightResponse?>(null);

        var totalDownloads = await downloadLogRepository.GetDownloadCountByEventAsync(
            spotlight.Id, cancellationToken);

        var wmConfig = await watermarkRepository.GetByEventIdAsync(spotlight.Id, cancellationToken);
        var storageBytes = fileStorageService.GetFolderSize(spotlight.WatchFolder);

        // Grab first available thumbnail to use as a cover
        var firstPhotos = await photoRepository.GetByEventIdAsync(spotlight.Id, 1, 1, cancellationToken);
        var firstThumbnailUrl = firstPhotos.FirstOrDefault() is { } photo
            ? $"/api/photos/{photo.Id}/thumbnail"
            : null;

        return Result.Success<EventSpotlightResponse?>(new EventSpotlightResponse(
            spotlight.Id,
            spotlight.Name,
            spotlight.EventType.ToString(),
            spotlight.EventDate,
            spotlight.ClientName,
            spotlight.VenueName,
            spotlight.PhotoCount,
            totalDownloads,
            storageBytes,
            FormatBytes(storageBytes),
            spotlight.EnableFaceRecognition,
            wmConfig is { Enabled: true },
            spotlight.IsActive,
            firstThumbnailUrl));
    }

    private static string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1_048_576 => $"{bytes / 1024.0:F1} KB",
        < 1_073_741_824 => $"{bytes / 1_048_576.0:F1} MB",
        _ => $"{bytes / 1_073_741_824.0:F2} GB"
    };
}
