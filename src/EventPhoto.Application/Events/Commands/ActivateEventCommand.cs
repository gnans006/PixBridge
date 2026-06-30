using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.Events.Commands;

/// <summary>
/// Command that activates an event so it begins accepting new photos.
/// </summary>
/// <param name="Id">The event identifier.</param>
public sealed record ActivateEventCommand(Guid Id) : IRequest<Result>;

/// <summary>
/// Handles the <see cref="ActivateEventCommand"/>.
/// </summary>
public sealed class ActivateEventCommandHandler(
    IEventRepository eventRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ActivateEventCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> Handle(
        ActivateEventCommand request,
        CancellationToken cancellationToken)
    {
        var eventEntity = await eventRepository.GetByIdAsync(request.Id, cancellationToken);
        if (eventEntity is null)
        {
            return Result.Failure($"Event '{request.Id}' was not found.");
        }

        eventEntity.Activate();
        await eventRepository.UpdateAsync(eventEntity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
