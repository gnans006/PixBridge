using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Contracts.Responses.Statistics;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.Statistics.Queries;

/// <summary>Returns the consolidated KPI overview for the studio command centre dashboard.</summary>
public sealed record GetDashboardOverviewQuery : IRequest<Result<DashboardOverviewResponse>>;

/// <summary>Handles <see cref="GetDashboardOverviewQuery"/>.</summary>
public sealed class GetDashboardOverviewQueryHandler(
    IEventRepository eventRepository,
    IPhotoRepository photoRepository,
    IDownloadLogRepository downloadLogRepository,
    IWatermarkConfigurationRepository watermarkRepository,
    IFaceEmbeddingRepository faceEmbeddingRepository)
    : IRequestHandler<GetDashboardOverviewQuery, Result<DashboardOverviewResponse>>
{
    /// <inheritdoc />
    public async Task<Result<DashboardOverviewResponse>> Handle(
        GetDashboardOverviewQuery request,
        CancellationToken cancellationToken)
    {
        var allEvents = await eventRepository.GetAllAsync(cancellationToken);
        var activeEvents = allEvents.Where(e => e.IsActive).ToList();

        var totalDownloads = await downloadLogRepository.GetTotalCountAsync(cancellationToken);
        var downloadsToday = await downloadLogRepository.GetTodayCountAsync(cancellationToken);
        var pendingThumbnails = await photoRepository.CountPendingThumbnailsAsync(cancellationToken);
        var pendingFaceIndexes = await photoRepository.CountPendingFaceIndexAsync(cancellationToken);

        var totalSizeBytes = allEvents.Sum(e => e.TotalSizeBytes);
        var eventsWithFaceSearch = allEvents.Count(e => e.EnableFaceRecognition);

        var eventsWithWatermark = 0;
        foreach (var evt in allEvents)
        {
            var wm = await watermarkRepository.GetByEventIdAsync(evt.Id, cancellationToken);
            if (wm is { Enabled: true }) eventsWithWatermark++;
        }

        var totalFaceEmbeddings = 0;
        try
        {
            foreach (var evt in allEvents.Where(e => e.EnableFaceRecognition))
                totalFaceEmbeddings += await faceEmbeddingRepository.CountByEventIdAsync(evt.Id, cancellationToken);
        }
        catch
        {
            // face_embeddings table may not exist in this environment — degrade gracefully
        }

        return Result.Success(new DashboardOverviewResponse(
            activeEvents.Count,
            allEvents.Count,
            allEvents.Sum(e => e.PhotoCount),
            downloadsToday,
            totalDownloads,
            totalSizeBytes,
            FormatBytes(totalSizeBytes),
            pendingThumbnails,
            pendingFaceIndexes,
            totalFaceEmbeddings,
            eventsWithFaceSearch,
            eventsWithWatermark));
    }

    private static string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1_048_576 => $"{bytes / 1024.0:F1} KB",
        < 1_073_741_824 => $"{bytes / 1_048_576.0:F1} MB",
        _ => $"{bytes / 1_073_741_824.0:F2} GB"
    };
}
