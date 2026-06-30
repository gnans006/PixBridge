using AutoMapper;
using EventPhoto.Contracts.Responses.Events;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.Events.Queries;

/// <summary>
/// Query that retrieves all active, non-deleted events as a summary list.
/// </summary>
public sealed record GetAllActiveEventsQuery : IRequest<Result<List<EventSummaryResponse>>>;

/// <summary>
/// Handles the <see cref="GetAllActiveEventsQuery"/>.
/// </summary>
public sealed class GetAllActiveEventsQueryHandler(
    IEventRepository eventRepository,
    IMapper mapper)
    : IRequestHandler<GetAllActiveEventsQuery, Result<List<EventSummaryResponse>>>
{
    /// <inheritdoc />
    public async Task<Result<List<EventSummaryResponse>>> Handle(
        GetAllActiveEventsQuery request,
        CancellationToken cancellationToken)
    {
        var events = await eventRepository.GetAllActiveAsync(cancellationToken);
        return Result.Success(mapper.Map<List<EventSummaryResponse>>(events));
    }
}
