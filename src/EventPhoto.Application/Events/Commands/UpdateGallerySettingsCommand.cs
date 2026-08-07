using AutoMapper;
using EventPhoto.Contracts.Responses.Events;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace EventPhoto.Application.Events.Commands;

/// <summary>Updates the gallery access settings for an event.</summary>
public sealed record UpdateGallerySettingsCommand(
    Guid EventId,
    bool AllowGalleryBrowsing,
    bool AllowFaceSearch,
    bool RestrictDownloadsToMatchedPhotos,
    int? GalleryRecentCount) : IRequest<Result<EventResponse>>;

/// <summary>Validates <see cref="UpdateGallerySettingsCommand"/>.</summary>
public sealed class UpdateGallerySettingsCommandValidator : AbstractValidator<UpdateGallerySettingsCommand>
{
    public UpdateGallerySettingsCommandValidator()
    {
        RuleFor(x => x.EventId).NotEmpty();
        RuleFor(x => x)
            .Must(x => x.AllowGalleryBrowsing || x.AllowFaceSearch)
            .WithMessage("At least one of AllowGalleryBrowsing or AllowFaceSearch must be enabled.");
        RuleFor(x => x.GalleryRecentCount)
            .InclusiveBetween(1, 1000)
            .When(x => x.GalleryRecentCount.HasValue);
    }
}

/// <summary>Handles <see cref="UpdateGallerySettingsCommand"/>.</summary>
public sealed class UpdateGallerySettingsCommandHandler(
    IEventRepository eventRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper)
    : IRequestHandler<UpdateGallerySettingsCommand, Result<EventResponse>>
{
    /// <inheritdoc />
    public async Task<Result<EventResponse>> Handle(
        UpdateGallerySettingsCommand request,
        CancellationToken cancellationToken)
    {
        var eventEntity = await eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (eventEntity is null)
        {
            return Result.Failure<EventResponse>("Event not found.");
        }

        // AllowFaceSearch requires EnableFaceRecognition — enforce at command level.
        var allowFaceSearch = request.AllowFaceSearch && eventEntity.EnableFaceRecognition;

        // Merge gallery fields; preserve overview and face recognition settings unchanged.
        eventEntity.Update(
            eventEntity.Name,
            eventEntity.EventType,
            eventEntity.EventDate,
            eventEntity.Description,
            eventEntity.VenueName,
            eventEntity.ClientName,
            request.GalleryRecentCount,
            eventEntity.EnableFaceRecognition,
            request.AllowGalleryBrowsing,
            allowFaceSearch,
            request.RestrictDownloadsToMatchedPhotos,
            eventEntity.FaceMatchThreshold);

        await eventRepository.UpdateAsync(eventEntity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(mapper.Map<EventResponse>(eventEntity));
    }
}
