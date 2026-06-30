using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EventPhoto.Application.Photos.Commands;

/// <summary>
/// Command that processes the thumbnail for a single photo.
/// Marks the photo as <c>Processing</c>, generates the thumbnail image,
/// then marks it as <c>Done</c> (or <c>Failed</c> on error).
/// </summary>
/// <param name="PhotoId">The photo identifier.</param>
/// <param name="MaxWidth">Maximum thumbnail width in pixels.</param>
/// <param name="MaxHeight">Maximum thumbnail height in pixels.</param>
public sealed record ProcessThumbnailCommand(
    Guid PhotoId,
    int MaxWidth = 400,
    int MaxHeight = 300)
    : IRequest<Result>;

/// <summary>
/// Handles the <see cref="ProcessThumbnailCommand"/>.
/// </summary>
public sealed class ProcessThumbnailCommandHandler(
    IPhotoRepository photoRepository,
    IThumbnailService thumbnailService,
    IUnitOfWork unitOfWork,
    ILogger<ProcessThumbnailCommandHandler> logger)
    : IRequestHandler<ProcessThumbnailCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> Handle(
        ProcessThumbnailCommand request,
        CancellationToken cancellationToken)
    {
        var photo = await photoRepository.GetByIdAsync(request.PhotoId, cancellationToken);
        if (photo is null)
        {
            return Result.Failure($"Photo '{request.PhotoId}' was not found.");
        }

        photo.MarkThumbnailProcessing();
        await photoRepository.UpdateAsync(photo, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            var (width, height) = await thumbnailService.GenerateThumbnailAsync(
                photo.OriginalPath,
                photo.ThumbnailPath,
                request.MaxWidth,
                request.MaxHeight,
                cancellationToken);

            photo.MarkThumbnailDone(width, height);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Thumbnail generation failed for photo {PhotoId}",
                photo.Id);

            photo.MarkThumbnailFailed();
            await photoRepository.UpdateAsync(photo, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Failure($"Thumbnail generation failed: {ex.Message}");
        }

        await photoRepository.UpdateAsync(photo, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
