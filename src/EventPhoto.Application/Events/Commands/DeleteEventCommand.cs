using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.Events.Commands;

/// <summary>
/// Command that soft-deletes an event.
/// </summary>
/// <param name="Id">The event identifier.</param>
public sealed record DeleteEventCommand(Guid Id) : IRequest<Result>;

/// <summary>
/// Handles the <see cref="DeleteEventCommand"/>.
/// </summary>
public sealed class DeleteEventCommandHandler(
    IEventRepository eventRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteEventCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> Handle(
        DeleteEventCommand request,
        CancellationToken cancellationToken)
    {
        var eventEntity = await eventRepository.GetByIdAsync(request.Id, cancellationToken);
        if (eventEntity is null)
        {
            return Result.Failure($"Event '{request.Id}' was not found.");
        }

        eventEntity.Delete();
        await eventRepository.UpdateAsync(eventEntity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
