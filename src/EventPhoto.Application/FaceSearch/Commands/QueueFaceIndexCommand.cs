using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EventPhoto.Application.FaceSearch.Commands;

/// <summary>
/// Transitions a photo's <c>FaceIndexStatus</c> from <c>NotRequired</c> to <c>Pending</c>.
/// Dispatched by <see cref="Photos.Commands.IngestPhotoCommandHandler"/> after thumbnail
/// generation completes for events with <c>EnableFaceRecognition=true</c>.
/// </summary>
public sealed record QueueFaceIndexCommand(Guid PhotoId) : IRequest<Result>;

/// <summary>Handles <see cref="QueueFaceIndexCommand"/>.</summary>
public sealed class QueueFaceIndexCommandHandler(
    IPhotoRepository photoRepository,
    IUnitOfWork unitOfWork,
    ILogger<QueueFaceIndexCommandHandler> logger)
    : IRequestHandler<QueueFaceIndexCommand, Result>
{
    public async Task<Result> Handle(QueueFaceIndexCommand request, CancellationToken cancellationToken)
    {
        var photo = await photoRepository.GetByIdAsync(request.PhotoId, cancellationToken);
        if (photo is null)
            return Result.Failure($"Photo '{request.PhotoId}' not found.");

        photo.QueueForFaceIndexing();
        await photoRepository.UpdateAsync(photo, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Photo {PhotoId} queued for face indexing.", request.PhotoId);
        return Result.Success();
    }
}
