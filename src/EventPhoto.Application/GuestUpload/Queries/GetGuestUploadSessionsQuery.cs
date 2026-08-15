using EventPhoto.Domain.Common;
using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.GuestUpload.Queries;

/// <summary>Returns all guest upload sessions for an event, ordered newest-first.</summary>
public sealed record GetGuestUploadSessionsQuery(Guid EventId)
    : IRequest<Result<List<GuestUploadSession>>>;

/// <summary>Handles <see cref="GetGuestUploadSessionsQuery"/>.</summary>
public sealed class GetGuestUploadSessionsQueryHandler(IGuestUploadRepository repository)
    : IRequestHandler<GetGuestUploadSessionsQuery, Result<List<GuestUploadSession>>>
{
    public async Task<Result<List<GuestUploadSession>>> Handle(
        GetGuestUploadSessionsQuery request,
        CancellationToken cancellationToken)
    {
        var sessions = await repository.GetSessionsForEventAsync(request.EventId, cancellationToken);
        return Result.Success(sessions);
    }
}
