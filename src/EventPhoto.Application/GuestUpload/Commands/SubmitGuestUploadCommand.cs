using EventPhoto.Domain.Common;
using EventPhoto.Domain.Enums;
using EventPhoto.Domain.Interfaces;
using GuestUploadEntity = EventPhoto.Domain.Entities.GuestUpload;
using GuestUploadSessionEntity = EventPhoto.Domain.Entities.GuestUploadSession;
using MediatR;

namespace EventPhoto.Application.GuestUpload.Commands;

/// <summary>Records a guest photo submission linked to an upload session.</summary>
public sealed record SubmitGuestUploadCommand(
    string SessionCode,
    string OriginalFileName,
    string StoredPath,
    long FileSizeBytes,
    string ContentType) : IRequest<Result<GuestUploadEntity>>;

/// <summary>Handles <see cref="SubmitGuestUploadCommand"/>.</summary>
public sealed class SubmitGuestUploadCommandHandler(
    IGuestUploadRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SubmitGuestUploadCommand, Result<GuestUploadEntity>>
{
    public async Task<Result<GuestUploadEntity>> Handle(
        SubmitGuestUploadCommand request,
        CancellationToken cancellationToken)
    {
        var session = await repository.GetSessionByCodeAsync(request.SessionCode, cancellationToken);
        if (session is null)
            return Result.Failure<GuestUploadEntity>("Upload session not found.");

        if (session.Status == GuestUploadSessionStatus.Closed)
            return Result.Failure<GuestUploadEntity>("This upload session is closed.");

        var upload = GuestUploadEntity.Create(
            session.EventId,
            session.Id,
            request.OriginalFileName,
            request.StoredPath,
            request.FileSizeBytes,
            request.ContentType);

        await repository.AddUploadAsync(upload, cancellationToken);
        session.IncrementPhotoCount();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success<GuestUploadEntity>(upload);
    }
}
