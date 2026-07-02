using AutoMapper;
using EventPhoto.Contracts.Responses.Events;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.Events.Queries;

/// <summary>Query to retrieve events. When <see cref="IncludeInactive"/> is true, all non-deleted events are returned; otherwise only active events.</summary>
public sealed record GetEventsQuery(bool IncludeInactive = false) : IRequest<Result<List<EventResponse>>>;

/// <summary>Handles fetching events.</summary>
public sealed class GetEventsQueryHandler(IEventRepository eventRepository, IMapper mapper)
    : IRequestHandler<GetEventsQuery, Result<List<EventResponse>>>
{
    /// <inheritdoc />
    public async Task<Result<List<EventResponse>>> Handle(GetEventsQuery request, CancellationToken cancellationToken)
    {
        var events = request.IncludeInactive
            ? await eventRepository.GetAllAsync(cancellationToken)
            : await eventRepository.GetAllActiveAsync(cancellationToken);
        return Result.Success(mapper.Map<List<EventResponse>>(events));
    }
}
