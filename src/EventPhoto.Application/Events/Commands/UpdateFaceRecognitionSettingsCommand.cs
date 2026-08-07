using AutoMapper;
using EventPhoto.Contracts.Responses.Events;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace EventPhoto.Application.Events.Commands;

/// <summary>Updates the face recognition AI settings for an event.</summary>
public sealed record UpdateFaceRecognitionSettingsCommand(
    Guid EventId,
    bool EnableFaceRecognition,
    float FaceMatchThreshold,
    bool AllowFaceSearch) : IRequest<Result<EventResponse>>;

/// <summary>Validates <see cref="UpdateFaceRecognitionSettingsCommand"/>.</summary>
public sealed class UpdateFaceRecognitionSettingsCommandValidator
    : AbstractValidator<UpdateFaceRecognitionSettingsCommand>
{
    public UpdateFaceRecognitionSettingsCommandValidator()
    {
        RuleFor(x => x.EventId).NotEmpty();
        RuleFor(x => x.FaceMatchThreshold)
            .InclusiveBetween(0.0f, 1.0f)
            .WithMessage("Match threshold must be between 0.0 and 1.0.");
    }
}

/// <summary>Handles <see cref="UpdateFaceRecognitionSettingsCommand"/>.</summary>
public sealed class UpdateFaceRecognitionSettingsCommandHandler(
    IEventRepository eventRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper)
    : IRequestHandler<UpdateFaceRecognitionSettingsCommand, Result<EventResponse>>
{
    /// <inheritdoc />
    public async Task<Result<EventResponse>> Handle(
        UpdateFaceRecognitionSettingsCommand request,
        CancellationToken cancellationToken)
    {
        var eventEntity = await eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (eventEntity is null)
        {
            return Result.Failure<EventResponse>("Event not found.");
        }

        // When disabling face recognition, face search cannot remain enabled.
        var allowFaceSearch = request.AllowFaceSearch && request.EnableFaceRecognition;

        // When disabling FR but AllowFaceSearch was on, we must also ensure AllowGalleryBrowsing
        // is true so the domain invariant (at least one of browsing/search) is satisfied.
        var allowGalleryBrowsing = eventEntity.AllowGalleryBrowsing || !allowFaceSearch;

        // Merge face recognition fields; preserve overview and gallery settings unchanged.
        eventEntity.Update(
            eventEntity.Name,
            eventEntity.EventType,
            eventEntity.EventDate,
            eventEntity.Description,
            eventEntity.VenueName,
            eventEntity.ClientName,
            eventEntity.GalleryRecentCount,
            request.EnableFaceRecognition,
            allowGalleryBrowsing,
            allowFaceSearch,
            eventEntity.RestrictDownloadsToMatchedPhotos,
            request.FaceMatchThreshold);

        await eventRepository.UpdateAsync(eventEntity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(mapper.Map<EventResponse>(eventEntity));
    }
}
