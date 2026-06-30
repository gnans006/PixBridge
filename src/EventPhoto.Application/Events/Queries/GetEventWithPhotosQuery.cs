using AutoMapper;
using EventPhoto.Contracts.Responses.Events;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.Events.Queries;

/// <summary>
/// Query that retrieves an event with its associated photos eagerly loaded.
/// </summary>
/// <param name="Id">The event identifier.</param>
public sealed record GetEventWithPhotosQuery(Guid Id) : IRequest<Result<EventResponse>>;

/// <summary>
/// Handles the <see cref="GetEventWithPhotosQuery"/>.
/// </summary>
public sealed class GetEventWithPhotosQueryHandler(
    IEventRepository eventRepository,
    IMapper mapper)
    : IRequestHandler<GetEventWithPhotosQuery, Result<EventResponse>>
{
    /// <inheritdoc />
    public async Task<Result<EventResponse>> Handle(
        GetEventWithPhotosQuery request,
        CancellationToken cancellationToken)
    {
        var eventEntity = await eventRepository.GetWithPhotosAsync(request.Id, cancellationToken);
        if (eventEntity is null)
        {
            return Result.Failure<EventResponse>($"Event '{request.Id}' was not found.");
        }

        return Result.Success(mapper.Map<EventResponse>(eventEntity));
    }
}
