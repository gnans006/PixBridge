using AutoMapper;
using EventPhoto.Contracts.Responses.Events;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Enums;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.Events.Commands;

/// <summary>Command to update an existing event's details.</summary>
public sealed record UpdateEventCommand(Guid EventId, string Name, string EventType, DateOnly EventDate, string? Description, string? VenueName, string? ClientName) : IRequest<Result<EventResponse>>;

/// <summary>Handles updating an event's metadata.</summary>
public sealed class UpdateEventCommandHandler : IRequestHandler<UpdateEventCommand, Result<EventResponse>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    /// <summary>Initializes a new instance of <see cref="UpdateEventCommandHandler"/>.</summary>
    public UpdateEventCommandHandler(IEventRepository eventRepository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<Result<EventResponse>> Handle(UpdateEventCommand request, CancellationToken cancellationToken)
    {
        var eventEntity = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (eventEntity is null)
        {
            return Result.Failure<EventResponse>("Event not found.");
        }

        if (!Enum.TryParse<EventType>(request.EventType, true, out var eventType))
        {
            return Result.Failure<EventResponse>($"Invalid event type: {request.EventType}");
        }

        eventEntity.Update(request.Name, eventType, request.EventDate, request.Description, request.VenueName, request.ClientName);
        await _eventRepository.UpdateAsync(eventEntity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(_mapper.Map<EventResponse>(eventEntity));
    }
}
