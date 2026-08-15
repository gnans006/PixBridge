using EventPhoto.Domain.Common;
using EventPhoto.Domain.Enums;
using EventPhoto.Domain.Interfaces;
using GuestUploadEntity = EventPhoto.Domain.Entities.GuestUpload;
using MediatR;

namespace EventPhoto.Application.GuestUpload.Queries;

/// <summary>Returns all guest uploads for an event, optionally filtered by moderation status.</summary>
public sealed record GetGuestUploadsQuery(Guid EventId, string? Status = null)
    : IRequest<Result<List<GuestUploadEntity>>>;

/// <summary>Handles <see cref="GetGuestUploadsQuery"/>.</summary>
public sealed class GetGuestUploadsQueryHandler(IGuestUploadRepository repository)
    : IRequestHandler<GetGuestUploadsQuery, Result<List<GuestUploadEntity>>>
{
    public async Task<Result<List<GuestUploadEntity>>> Handle(
        GetGuestUploadsQuery request,
        CancellationToken cancellationToken)
    {
        ModerationStatus? filter = null;
        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<ModerationStatus>(request.Status, ignoreCase: true, out var parsed))
        {
            filter = parsed;
        }

        var uploads = await repository.GetUploadsForEventAsync(request.EventId, filter, cancellationToken);
        return Result.Success<List<GuestUploadEntity>>(uploads);
    }
}
