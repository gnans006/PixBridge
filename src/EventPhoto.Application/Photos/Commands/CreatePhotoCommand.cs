using AutoMapper;
using EventPhoto.Domain.Common;
using EventPhoto.Contracts.Responses.Photos;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.Photos.Commands;

/// <summary>Command to register a new photo detected by the file watcher.</summary>
/// <param name="EventId">The parent event identifier.</param>
/// <param name="FileName">The file name.</param>
/// <param name="OriginalPath">Absolute path to the full-resolution file.</param>
/// <param name="FileSizeBytes">File size in bytes.</param>
/// <param name="MimeType">The MIME type.</param>
/// <param name="TakenAt">Optional EXIF capture timestamp.</param>
public sealed record CreatePhotoCommand(
    Guid EventId,
    string FileName,
    string OriginalPath,
    long FileSizeBytes,
    string MimeType,
    DateTimeOffset? TakenAt = null)
    : IRequest<Result<PhotoResponse>>;

/// <summary>Handles <see cref="CreatePhotoCommand"/>.</summary>
public sealed class CreatePhotoCommandHandler(
    IEventRepository eventRepository,
    IPhotoRepository photoRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper)
    : IRequestHandler<CreatePhotoCommand, Result<PhotoResponse>>
{
    /// <inheritdoc />
    public async Task<Result<PhotoResponse>> Handle(CreatePhotoCommand request, CancellationToken cancellationToken)
    {
        var eventEntity = await eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (eventEntity is null)
        {
            return Result.Failure<PhotoResponse>("Event not found.");
        }

        if (!eventEntity.IsActive)
        {
            return Result.Failure<PhotoResponse>("Event is not active.");
        }

        var alreadyExists = await photoRepository.ExistsByPathAsync(request.OriginalPath, cancellationToken);
        if (alreadyExists)
        {
            return Result.Failure<PhotoResponse>("Photo already registered.");
        }

        var thumbnailPath = Path.Combine(eventEntity.ThumbnailFolder, $"thumb_{Path.GetFileNameWithoutExtension(request.FileName)}.jpg");
        var photo = Domain.Entities.Photo.Create(request.EventId, request.FileName, request.OriginalPath, thumbnailPath, request.FileSizeBytes, request.MimeType, request.TakenAt);
        eventEntity.IncrementPhotoCount(request.FileSizeBytes);
        await photoRepository.AddAsync(photo, cancellationToken);
        await eventRepository.UpdateAsync(eventEntity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(mapper.Map<PhotoResponse>(photo));
    }
}
