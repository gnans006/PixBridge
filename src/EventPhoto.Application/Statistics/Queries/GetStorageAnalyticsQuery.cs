using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Contracts.Responses.Statistics;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.Statistics.Queries;

/// <summary>Returns live storage analytics per event folder.</summary>
public sealed record GetStorageAnalyticsQuery : IRequest<Result<StorageAnalyticsResponse>>;

/// <summary>Handles <see cref="GetStorageAnalyticsQuery"/>.</summary>
public sealed class GetStorageAnalyticsQueryHandler(
    IEventRepository eventRepository,
    IFileStorageService fileStorageService)
    : IRequestHandler<GetStorageAnalyticsQuery, Result<StorageAnalyticsResponse>>
{
    /// <inheritdoc />
    public async Task<Result<StorageAnalyticsResponse>> Handle(
        GetStorageAnalyticsQuery request,
        CancellationToken cancellationToken)
    {
        var allEvents = await eventRepository.GetAllAsync(cancellationToken);

        var items = allEvents
            .Select(e =>
            {
                var size = fileStorageService.GetFolderSize(e.WatchFolder);
                return new StorageEventItem(e.Id, e.Name, size, FormatBytes(size), e.PhotoCount);
            })
            .OrderByDescending(e => e.SizeBytes)
            .Take(8)
            .ToList();

        var totalBytes = items.Sum(i => i.SizeBytes);

        return Result.Success(new StorageAnalyticsResponse(
            totalBytes,
            FormatBytes(totalBytes),
            allEvents.Count,
            items));
    }

    private static string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1_048_576 => $"{bytes / 1024.0:F1} KB",
        < 1_073_741_824 => $"{bytes / 1_048_576.0:F1} MB",
        _ => $"{bytes / 1_073_741_824.0:F2} GB"
    };
}
