using EventPhoto.Contracts.Responses.Statistics;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.Statistics.Queries;

/// <summary>Returns recent download activity for the dashboard timeline.</summary>
public sealed record GetRecentActivityQuery(int Count = 20)
    : IRequest<Result<List<RecentActivityItemResponse>>>;

/// <summary>Handles <see cref="GetRecentActivityQuery"/>.</summary>
public sealed class GetRecentActivityQueryHandler(
    IEventRepository eventRepository,
    IDownloadLogRepository downloadLogRepository)
    : IRequestHandler<GetRecentActivityQuery, Result<List<RecentActivityItemResponse>>>
{
    /// <inheritdoc />
    public async Task<Result<List<RecentActivityItemResponse>>> Handle(
        GetRecentActivityQuery request,
        CancellationToken cancellationToken)
    {
        var recentDownloads = await downloadLogRepository.GetRecentAsync(request.Count, cancellationToken);
        var allEvents = await eventRepository.GetAllAsync(cancellationToken);
        var eventNames = allEvents.ToDictionary(e => e.Id, e => e.Name);

        var activities = recentDownloads
            .Select(d => new RecentActivityItemResponse(
                "download",
                d.EventId,
                eventNames.GetValueOrDefault(d.EventId, "Unknown Event"),
                d.PhotoId,
                d.DownloadedAt,
                d.IpAddress))
            .ToList();

        return Result.Success(activities);
    }
}
