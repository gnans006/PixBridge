using EventPhoto.Contracts.Responses.Events;
using EventPhoto.Contracts.Responses.Photos;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.FaceSearch.Queries;

/// <summary>
/// Returns the event configuration that determines the guest's gallery mode.
/// Used by the React landing page to decide which UI to render:
/// GalleryOnly / FaceSearchOnly / Hybrid.
/// </summary>
public sealed record GetGuestGalleryConfigQuery(Guid EventId) : IRequest<Result<GuestGalleryConfigResponse>>;

/// <summary>Guest gallery mode and access configuration response.</summary>
public sealed record GuestGalleryConfigResponse(
    Guid EventId,
    string EventName,
    bool AllowGalleryBrowsing,
    bool AllowFaceSearch,
    bool FaceRecognitionEnabled,
    bool RestrictDownloadsToMatchedPhotos,
    string GalleryMode  // "GalleryOnly" | "FaceSearchOnly" | "Hybrid"
);

/// <summary>Handles <see cref="GetGuestGalleryConfigQuery"/>.</summary>
public sealed class GetGuestGalleryConfigQueryHandler(IEventRepository eventRepository)
    : IRequestHandler<GetGuestGalleryConfigQuery, Result<GuestGalleryConfigResponse>>
{
    public async Task<Result<GuestGalleryConfigResponse>> Handle(
        GetGuestGalleryConfigQuery request,
        CancellationToken cancellationToken)
    {
        var eventEntity = await eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (eventEntity is null)
            return Result.Failure<GuestGalleryConfigResponse>($"Event '{request.EventId}' not found.");

        if (!eventEntity.IsActive)
            return Result.Failure<GuestGalleryConfigResponse>("This event is not currently active.");

        var galleryMode = (eventEntity.AllowGalleryBrowsing, eventEntity.AllowFaceSearch) switch
        {
            (true, false) => "GalleryOnly",
            (false, true) => "FaceSearchOnly",
            _ => "Hybrid"
        };

        return Result.Success(new GuestGalleryConfigResponse(
            eventEntity.Id,
            eventEntity.Name,
            eventEntity.AllowGalleryBrowsing,
            eventEntity.AllowFaceSearch,
            eventEntity.EnableFaceRecognition,
            eventEntity.RestrictDownloadsToMatchedPhotos,
            galleryMode));
    }
}
