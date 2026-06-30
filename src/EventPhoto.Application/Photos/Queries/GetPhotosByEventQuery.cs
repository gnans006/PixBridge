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
    IPhotoRepository photoRepository,
    IMapper mapper)
    : IRequestHandler<GetPhotosByEventQuery, Result<PagedResult<PhotoResponse>>>
{
    /// <inheritdoc />
    public async Task<Result<PagedResult<PhotoResponse>>> Handle(
        GetPhotosByEventQuery request,
        CancellationToken cancellationToken)
    {
        var photos = await photoRepository.GetByEventIdAsync(
            request.EventId,
            request.Page,
            request.PageSize,
            cancellationToken);
        var totalCount = await photoRepository.GetTotalCountByEventAsync(request.EventId, cancellationToken);
        var mapped = mapper.Map<List<PhotoResponse>>(photos);
        return Result.Success(new PagedResult<PhotoResponse>(mapped, totalCount, request.Page, request.PageSize));
    }
}
