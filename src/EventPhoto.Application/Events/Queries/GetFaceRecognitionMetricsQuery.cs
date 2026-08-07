using EventPhoto.Contracts.Responses.Events;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.Events.Queries;

/// <summary>Returns face recognition indexing metrics for a single event.</summary>
/// <param name="EventId">The event identifier.</param>
public sealed record GetFaceRecognitionMetricsQuery(Guid EventId)
    : IRequest<Result<FaceRecognitionMetricsResponse>>;

/// <summary>Handles <see cref="GetFaceRecognitionMetricsQuery"/>.</summary>
public sealed class GetFaceRecognitionMetricsQueryHandler(
    IEventRepository eventRepository,
    IFaceEmbeddingRepository faceEmbeddingRepository)
    : IRequestHandler<GetFaceRecognitionMetricsQuery, Result<FaceRecognitionMetricsResponse>>
{
    /// <inheritdoc />
    public async Task<Result<FaceRecognitionMetricsResponse>> Handle(
        GetFaceRecognitionMetricsQuery request,
        CancellationToken cancellationToken)
    {
        var eventEntity = await eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (eventEntity is null)
        {
            return Result.Failure<FaceRecognitionMetricsResponse>(
                $"Event '{request.EventId}' was not found.");
        }

        var indexedFaces = 0;

        try
        {
            indexedFaces = await faceEmbeddingRepository.CountByEventIdAsync(
                request.EventId, cancellationToken);
        }
        catch
        {
            // face_embeddings table may not exist in all environments — fall back gracefully.
        }

        // One face embedding per indexed photo (approximation; some photos may have multiple faces)
        var indexedPhotos = Math.Min(indexedFaces, eventEntity.PhotoCount);
        var pendingPhotos = Math.Max(0, eventEntity.PhotoCount - indexedPhotos);

        return Result.Success(new FaceRecognitionMetricsResponse(
            eventEntity.Id,
            eventEntity.EnableFaceRecognition,
            eventEntity.FaceMatchThreshold,
            eventEntity.PhotoCount,
            indexedFaces,
            indexedPhotos,
            pendingPhotos,
            FailedPhotos: 0));
    }
}
