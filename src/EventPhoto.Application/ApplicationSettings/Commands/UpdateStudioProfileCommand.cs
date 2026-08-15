using EventPhoto.Domain.Common;
using EventPhoto.Domain.Exceptions;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.ApplicationSettings.Commands;

/// <summary>Command to update the studio profile contact and social fields.</summary>
public sealed record UpdateStudioProfileCommand(
    string? Phone,
    string? Email,
    string? Website,
    string? Address,
    string? Instagram,
    string? Facebook,
    string? WhatsApp,
    string? LogoPath,
    string? GstNumber) : IRequest<Result>;

/// <summary>Handles <see cref="UpdateStudioProfileCommand"/>.</summary>
public sealed class UpdateStudioProfileCommandHandler(
    IApplicationSettingsRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateStudioProfileCommand, Result>
{
    public async Task<Result> Handle(UpdateStudioProfileCommand request, CancellationToken cancellationToken)
    {
        var settings = await repository.GetOrCreateDefaultAsync(cancellationToken);

        try
        {
            settings.UpdateStudioProfile(
                request.Phone,
                request.Email,
                request.Website,
                request.Address,
                request.Instagram,
                request.Facebook,
                request.WhatsApp,
                request.LogoPath,
                request.GstNumber);
        }
        catch (DomainException ex)
        {
            return Result.Failure(ex.Message);
        }

        await repository.UpdateAsync(settings, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
