using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.GuestUpload.Commands;

/// <summary>Closes an active guest upload session so no more photos may be submitted.</summary>
public sealed record CloseGuestUploadSessionCommand(Guid SessionId) : IRequest<Result>;

/// <summary>Handles <see cref="CloseGuestUploadSessionCommand"/>.</summary>
public sealed class CloseGuestUploadSessionCommandHandler(
    IGuestUploadRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CloseGuestUploadSessionCommand, Result>
{
    public async Task<Result> Handle(
        CloseGuestUploadSessionCommand request,
        CancellationToken cancellationToken)
    {
        var session = await repository.GetSessionByIdAsync(request.SessionId, cancellationToken);
        if (session is null)
            return Result.Failure("Session not found.");

        session.Close();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
