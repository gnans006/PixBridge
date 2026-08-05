using EventPhoto.Contracts.Responses.Statistics;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.Statistics.Queries;

/// <summary>Returns face recognition analytics across all events.</summary>
public sealed record GetFaceAnalyticsQuery : IRequest<Result<FaceAnalyticsResponse>>;

/// <summary>Handles <see cref="GetFaceAnalyticsQuery"/>.</summary>
public sealed class GetFaceAnalyticsQueryHandler(
    IEventRepository eventRepository,
    IPhotoRepository photoRepository,
    IFaceEmbeddingRepository faceEmbeddingRepository)
    : IRequestHandler<GetFaceAnalyticsQuery, Result<FaceAnalyticsResponse>>
{
    /// <inheritdoc />
    public async Task<Result<FaceAnalyticsResponse>> Handle(
        GetFaceAnalyticsQuery request,
        CancellationToken cancellationToken)
    {
        var allEvents = await eventRepository.GetAllAsync(cancellationToken);
        var faceEvents = allEvents.Where(e => e.EnableFaceRecognition).ToList();

        var breakdown = new List<FaceAnalyticsEventItem>();
        var totalFaces = 0;

        try
        {
            foreach (var evt in faceEvents)
            {
                var faceCount = await faceEmbeddingRepository.CountByEventIdAsync(evt.Id, cancellationToken);
                totalFaces += faceCount;
                breakdown.Add(new FaceAnalyticsEventItem(evt.Id, evt.Name, evt.PhotoCount, faceCount));
            }
        }
        catch
        {
            // face_embeddings table may not exist in this environment — return empty breakdown
            breakdown.Clear();
            totalFaces = 0;
        }

        var totalPending = 0;
        try
        {
            totalPending = await photoRepository.CountPendingFaceIndexAsync(cancellationToken);
        }
        catch { }

        return Result.Success(new FaceAnalyticsResponse(
            totalFaces,
            totalPending,
            faceEvents.Count,
            breakdown.OrderByDescending(b => b.FaceEmbeddings).ToList()));
    }
}
