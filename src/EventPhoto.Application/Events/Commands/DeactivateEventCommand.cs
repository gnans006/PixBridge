using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.Events.Commands;

/// <summary>
/// Command that deactivates an event so it stops accepting new photos.
/// </summary>
/// <param name="Id">The event identifier.</param>
public sealed record DeactivateEventCommand(Guid Id) : IRequest<Result>;

/// <summary>
/// Handles the <see cref="DeactivateEventCommand"/>.
/// </summary>
public sealed class DeactivateEventCommandHandler(
    IEventRepository eventRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeactivateEventCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> Handle(
        DeactivateEventCommand request,
        CancellationToken cancellationToken)
    {
        var eventEntity = await eventRepository.GetByIdAsync(request.Id, cancellationToken);
        if (eventEntity is null)
        {
            return Result.Failure($"Event '{request.Id}' was not found.");
        }

        eventEntity.Deactivate();
        await eventRepository.UpdateAsync(eventEntity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
