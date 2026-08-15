using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.GuestUpload.Commands;

/// <summary>Approves or rejects a guest-submitted photo.</summary>
public sealed record ModerateGuestUploadCommand(
    Guid UploadId,
    bool Approve,
    string? RejectionReason = null) : IRequest<Result>;

/// <summary>Handles <see cref="ModerateGuestUploadCommand"/>.</summary>
public sealed class ModerateGuestUploadCommandHandler(
    IGuestUploadRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ModerateGuestUploadCommand, Result>
{
    public async Task<Result> Handle(
        ModerateGuestUploadCommand request,
        CancellationToken cancellationToken)
    {
        var upload = await repository.GetUploadByIdAsync(request.UploadId, cancellationToken);
        if (upload is null)
            return Result.Failure("Upload not found.");

        if (request.Approve)
            upload.Approve();
        else
            upload.Reject(request.RejectionReason);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
