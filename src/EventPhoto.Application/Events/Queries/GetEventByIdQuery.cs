using AutoMapper;
using EventPhoto.Contracts.Responses.Events;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.Events.Queries;

/// <summary>Query to retrieve a single event by ID.</summary>
public sealed record GetEventByIdQuery(Guid EventId) : IRequest<Result<EventResponse>>;

/// <summary>Handles fetching a single event by its ID.</summary>
public sealed class GetEventByIdQueryHandler(
    IEventRepository eventRepository,
    IMapper mapper)
    : IRequestHandler<GetEventByIdQuery, Result<EventResponse>>
{
    /// <inheritdoc />
    public async Task<Result<EventResponse>> Handle(
        GetEventByIdQuery request,
        CancellationToken cancellationToken)
    {
        var eventEntity = await eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (eventEntity is null)
        {
            return Result.Failure<EventResponse>("Event not found.");
        }

        return Result.Success(mapper.Map<EventResponse>(eventEntity));
    }
}
