using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Application.Common.Models;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.Photos.Queries;

/// <summary>
/// Query that reads the raw bytes of a photo's original file and records a download event.
/// </summary>
/// <param name="PhotoId">The photo identifier.</param>
/// <param name="IpAddress">The optional IP address of the downloader.</param>
/// <param name="UserAgent">The optional browser user-agent string.</param>
public sealed record DownloadPhotoQuery(
    Guid PhotoId,
    string? IpAddress,
    string? UserAgent)
    : IRequest<Result<DownloadResult>>;

/// <summary>
/// Handles the <see cref="DownloadPhotoQuery"/>.
/// </summary>
public sealed class DownloadPhotoQueryHandler(
    IPhotoRepository photoRepository,
    IDownloadLogRepository downloadLogRepository,
    IFileService fileService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DownloadPhotoQuery, Result<DownloadResult>>
{
    /// <inheritdoc />
    public async Task<Result<DownloadResult>> Handle(
        DownloadPhotoQuery request,
        CancellationToken cancellationToken)
    {
        var photo = await photoRepository.GetByIdAsync(request.PhotoId, cancellationToken);
        if (photo is null)
        {
            return Result.Failure<DownloadResult>($"Photo '{request.PhotoId}' was not found.");
        }

        if (!fileService.FileExists(photo.OriginalPath))
        {
            return Result.Failure<DownloadResult>("The photo file could not be found on disk.");
        }

        var bytes = await fileService.ReadAllBytesAsync(photo.OriginalPath, cancellationToken);

        photo.RecordDownload();
        await photoRepository.UpdateAsync(photo, cancellationToken);

        var log = Domain.Entities.DownloadLog.Create(
            photo.Id,
            photo.EventId,
            request.IpAddress,
            request.UserAgent);

        await downloadLogRepository.AddAsync(log, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new DownloadResult(bytes, photo.MimeType, photo.FileName));
    }
}
