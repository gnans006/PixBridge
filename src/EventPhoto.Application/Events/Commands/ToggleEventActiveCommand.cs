using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.Events.Commands;

using AutoMapper;
using EventPhoto.Contracts.Responses.Events;

/// <summary>Command to activate or deactivate an event.</summary>
/// <param name="EventId">The event identifier.</param>
/// <param name="Activate">Whether to activate the event.</param>
public sealed record ToggleEventActiveCommand(Guid EventId, bool Activate) : IRequest<Result<EventResponse>>;

/// <summary>Handles the <see cref="ToggleEventActiveCommand"/>.</summary>
public sealed class ToggleEventActiveCommandHandler(
    IEventRepository eventRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper)
    : IRequestHandler<ToggleEventActiveCommand, Result<EventResponse>>
{
    /// <inheritdoc />
    public async Task<Result<EventResponse>> Handle(ToggleEventActiveCommand request, CancellationToken cancellationToken)
    {
        var eventEntity = await eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (eventEntity is null)
        {
            return Result.Failure<EventResponse>("Event not found.");
        }

        if (request.Activate)
        {
            eventEntity.Activate();
        }
        else
        {
            eventEntity.Deactivate();
        }

        await eventRepository.UpdateAsync(eventEntity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(mapper.Map<EventResponse>(eventEntity));
    }
}
