using AutoMapper;
using EventPhoto.Application.Common.Models;
using EventPhoto.Contracts.Responses.Photos;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.Photos.Queries;

/// <summary>
/// Query that returns a paged list of photos for a given event.
/// </summary>
/// <param name="EventId">The event identifier.</param>
/// <param name="Page">The 1-based page number.</param>
/// <param name="PageSize">The number of records per page.</param>
public sealed record GetPhotosByEventQuery(
    Guid EventId,
    int Page = 1,
    int PageSize = 50)
    : IRequest<Result<PagedResult<PhotoResponse>>>;

/// <summary>
/// Handles the <see cref="GetPhotosByEventQuery"/>.
/// </summary>
public sealed class GetPhotosByEventQueryHandler(
    IEventRepository eventRepository,
    IPhotoRepository photoRepository,
    IMapper mapper)
    : IRequestHandler<GetPhotosByEventQuery, Result<PagedResult<PhotoResponse>>>
{
    /// <inheritdoc />
    public async Task<Result<PagedResult<PhotoResponse>>> Handle(
        GetPhotosByEventQuery request,
        CancellationToken cancellationToken)
    {
        var eventEntity = await eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (eventEntity is null)
        {
            return Result.Failure<PagedResult<PhotoResponse>>("Event not found.");
        }

        var photos = await photoRepository.GetByEventIdAsync(
            request.EventId,
            request.Page,
            request.PageSize,
            cancellationToken);

        var mapped = mapper.Map<List<PhotoResponse>>(photos);

        // Use the cached PhotoCount from the event aggregate instead of a separate COUNT(*) query.
        return Result.Success(new PagedResult<PhotoResponse>(mapped, eventEntity.PhotoCount, request.Page, request.PageSize));
    }
}
