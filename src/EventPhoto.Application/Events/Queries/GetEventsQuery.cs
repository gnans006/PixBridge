using AutoMapper;
using EventPhoto.Contracts.Responses.Events;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.Events.Queries;

/// <summary>Query to retrieve all active events.</summary>
public sealed record GetEventsQuery : IRequest<Result<List<EventResponse>>>;

/// <summary>Handles fetching all active events.</summary>
public sealed class GetEventsQueryHandler(IEventRepository eventRepository, IMapper mapper)
    : IRequestHandler<GetEventsQuery, Result<List<EventResponse>>>
{
    /// <inheritdoc />
    public async Task<Result<List<EventResponse>>> Handle(GetEventsQuery request, CancellationToken cancellationToken)
    {
        var events = await eventRepository.GetAllActiveAsync(cancellationToken);
        return Result.Success(mapper.Map<List<EventResponse>>(events));
    }
}
