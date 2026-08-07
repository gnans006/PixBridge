using AutoMapper;
using EventPhoto.Contracts.Responses.Events;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Enums;
using EventPhoto.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace EventPhoto.Application.Events.Commands;

/// <summary>Updates the core overview fields of an event (name, type, dates, client info).</summary>
public sealed record UpdateEventOverviewCommand(
    Guid EventId,
    string Name,
    string EventType,
    DateOnly EventDate,
    string? Description,
    string? VenueName,
    string? ClientName) : IRequest<Result<EventResponse>>;

/// <summary>Validates <see cref="UpdateEventOverviewCommand"/>.</summary>
public sealed class UpdateEventOverviewCommandValidator : AbstractValidator<UpdateEventOverviewCommand>
{
    private static readonly string[] ValidEventTypes =
        ["Wedding", "Reception", "Birthday", "Corporate", "Outdoor", "Other"];

    public UpdateEventOverviewCommandValidator()
    {
        RuleFor(x => x.EventId).NotEmpty();
        RuleFor(x => x.Name)
            .NotEmpty().MinimumLength(2).MaximumLength(200);
        RuleFor(x => x.EventType)
            .NotEmpty()
            .Must(t => ValidEventTypes.Contains(t, StringComparer.OrdinalIgnoreCase))
            .WithMessage("EventType must be one of: Wedding, Reception, Birthday, Corporate, Outdoor, Other.");
        RuleFor(x => x.EventDate)
            .NotEmpty()
            .Must(d => d >= DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-10)))
            .WithMessage("Event date cannot be more than 10 years in the past.")
            .Must(d => d <= DateOnly.FromDateTime(DateTime.UtcNow.AddYears(5)))
            .WithMessage("Event date cannot be more than 5 years in the future.");
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
        RuleFor(x => x.VenueName).MinimumLength(2).MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.VenueName));
        RuleFor(x => x.ClientName).MinimumLength(2).MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.ClientName));
    }
}

/// <summary>Handles <see cref="UpdateEventOverviewCommand"/>.</summary>
public sealed class UpdateEventOverviewCommandHandler(
    IEventRepository eventRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper)
    : IRequestHandler<UpdateEventOverviewCommand, Result<EventResponse>>
{
    /// <inheritdoc />
    public async Task<Result<EventResponse>> Handle(
        UpdateEventOverviewCommand request,
        CancellationToken cancellationToken)
    {
        var eventEntity = await eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (eventEntity is null)
        {
            return Result.Failure<EventResponse>("Event not found.");
        }

        if (!Enum.TryParse<EventType>(request.EventType, true, out var eventType))
        {
            return Result.Failure<EventResponse>($"Invalid event type: {request.EventType}");
        }

        // Merge overview fields; preserve gallery and face recognition settings unchanged.
        eventEntity.Update(
            request.Name,
            eventType,
            request.EventDate,
            request.Description,
            request.VenueName,
            request.ClientName,
            eventEntity.GalleryRecentCount,
            eventEntity.EnableFaceRecognition,
            eventEntity.AllowGalleryBrowsing,
            eventEntity.AllowFaceSearch,
            eventEntity.RestrictDownloadsToMatchedPhotos,
            eventEntity.FaceMatchThreshold);

        await eventRepository.UpdateAsync(eventEntity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(mapper.Map<EventResponse>(eventEntity));
    }
}
