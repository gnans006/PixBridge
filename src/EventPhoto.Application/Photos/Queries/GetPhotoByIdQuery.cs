using AutoMapper;
using EventPhoto.Contracts.Responses.Photos;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.Photos.Queries;

/// <summary>
/// Query that retrieves the full details of a single photo.
/// </summary>
/// <param name="Id">The photo identifier.</param>
public sealed record GetPhotoByIdQuery(Guid Id) : IRequest<Result<PhotoResponse>>;

/// <summary>
/// Handles the <see cref="GetPhotoByIdQuery"/>.
/// </summary>
public sealed class GetPhotoByIdQueryHandler(
    IPhotoRepository photoRepository,
    IMapper mapper)
    : IRequestHandler<GetPhotoByIdQuery, Result<PhotoResponse>>
{
    /// <inheritdoc />
    public async Task<Result<PhotoResponse>> Handle(
        GetPhotoByIdQuery request,
        CancellationToken cancellationToken)
    {
        var photo = await photoRepository.GetByIdAsync(request.Id, cancellationToken);
        if (photo is null)
        {
            return Result.Failure<PhotoResponse>($"Photo '{request.Id}' was not found.");
        }

        return Result.Success(mapper.Map<PhotoResponse>(photo));
    }
}
