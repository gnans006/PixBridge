using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.Photos.Commands;

/// <summary>Command to soft-delete a photo.</summary>
/// <param name="PhotoId">The photo identifier.</param>
public sealed record DeletePhotoCommand(Guid PhotoId) : IRequest<Result>;

/// <summary>
/// Handles the <see cref="DeletePhotoCommand"/>.
/// </summary>
public sealed class DeletePhotoCommandHandler(
    IPhotoRepository photoRepository,
    IEventRepository eventRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeletePhotoCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> Handle(
        DeletePhotoCommand request,
        CancellationToken cancellationToken)
    {
        var photo = await photoRepository.GetByIdAsync(request.PhotoId, cancellationToken);
        if (photo is null)
        {
            return Result.Failure($"Photo '{request.PhotoId}' was not found.");
        }

        photo.Delete();
        var eventEntity = await eventRepository.GetByIdAsync(photo.EventId, cancellationToken);
        eventEntity?.DecrementPhotoCount(photo.FileSizeBytes);

        await photoRepository.UpdateAsync(photo, cancellationToken);
        if (eventEntity is not null)
        {
            await eventRepository.UpdateAsync(eventEntity, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
