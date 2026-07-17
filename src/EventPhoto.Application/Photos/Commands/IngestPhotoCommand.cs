using EventPhoto.Application.FaceSearch.Commands;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.Photos.Commands;

/// <summary>
/// Command that ingests a newly detected photo file into the system, creating a
/// <see cref="Domain.Entities.Photo"/> record and updating the parent event's statistics.
/// </summary>
/// <param name="EventId">The parent event identifier.</param>
/// <param name="FileName">The detected file name.</param>
/// <param name="OriginalPath">Absolute path to the full-resolution file.</param>
/// <param name="ThumbnailPath">Absolute path where the thumbnail should be written.</param>
/// <param name="FileSizeBytes">File size in bytes.</param>
/// <param name="MimeType">The MIME type (e.g. <c>image/jpeg</c>).</param>
/// <param name="TakenAt">Optional EXIF capture timestamp.</param>
public sealed record IngestPhotoCommand(
    Guid EventId,
    string FileName,
    string OriginalPath,
    string ThumbnailPath,
    long FileSizeBytes,
    string MimeType,
    DateTimeOffset? TakenAt = null)
    : IRequest<Result<Guid>>;

/// <summary>
/// Handles the <see cref="IngestPhotoCommand"/>.
/// After persisting the photo, queues it for face indexing if the event has
/// <c>EnableFaceRecognition=true</c> — face indexing never blocks this handler.
/// </summary>
public sealed class IngestPhotoCommandHandler(
    IEventRepository eventRepository,
    IPhotoRepository photoRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<IngestPhotoCommand, Result<Guid>>
{
    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(
        IngestPhotoCommand request,
        CancellationToken cancellationToken)
    {
        var eventEntity = await eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (eventEntity is null)
        {
            return Result.Failure<Guid>($"Event '{request.EventId}' was not found.");
        }

        if (!eventEntity.IsActive)
        {
            return Result.Failure<Guid>("Cannot ingest a photo into an inactive event.");
        }

        var alreadyExists = await photoRepository.ExistsByPathAsync(
            request.OriginalPath,
            cancellationToken);

        if (alreadyExists)
        {
            return Result.Failure<Guid>($"A photo already exists for path '{request.OriginalPath}'.");
        }

        var photo = Domain.Entities.Photo.Create(
            request.EventId,
            request.FileName,
            request.OriginalPath,
            request.ThumbnailPath,
            request.FileSizeBytes,
            request.MimeType,
            request.TakenAt);

        // Queue for face indexing immediately if recognition is enabled for this event.
        // The FaceIndexingService will pick it up asynchronously — gallery visibility is NOT blocked.
        if (eventEntity.EnableFaceRecognition)
        {
            photo.QueueForFaceIndexing();
        }

        eventEntity.IncrementPhotoCount(request.FileSizeBytes);

        await photoRepository.AddAsync(photo, cancellationToken);
        await eventRepository.UpdateAsync(eventEntity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(photo.Id);
    }
}

