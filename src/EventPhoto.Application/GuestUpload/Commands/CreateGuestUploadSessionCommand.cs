using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Application.Common.Models;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.GuestUpload.Commands;

/// <summary>Creates a new guest photo-upload session for an event.</summary>
public sealed record CreateGuestUploadSessionCommand(
    Guid EventId,
    string? Title) : IRequest<Result<GuestUploadSession>>, IRequiresFeature
{
    /// <inheritdoc />
    public string FeatureKey => Common.Models.FeatureKey.GuestUploadSessions;
}

/// <summary>Handles <see cref="CreateGuestUploadSessionCommand"/>.</summary>
public sealed class CreateGuestUploadSessionCommandHandler(
    IGuestUploadRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateGuestUploadSessionCommand, Result<GuestUploadSession>>
{
    public async Task<Result<GuestUploadSession>> Handle(
        CreateGuestUploadSessionCommand request,
        CancellationToken cancellationToken)
    {
        var session = GuestUploadSession.Create(request.EventId, request.Title);
        await repository.AddSessionAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(session);
    }
}
