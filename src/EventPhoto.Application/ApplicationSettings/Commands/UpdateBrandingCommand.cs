using EventPhoto.Domain.Common;
using EventPhoto.Domain.Exceptions;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.ApplicationSettings.Commands;

/// <summary>Command to update branding colors and theme.</summary>
public sealed record UpdateBrandingCommand(
    string PrimaryColor,
    string SecondaryColor,
    string BrandTheme,
    string GalleryTheme,
    string QrTheme,
    Guid? DefaultWatermarkProfileId) : IRequest<Result>;

/// <summary>Handles <see cref="UpdateBrandingCommand"/>.</summary>
public sealed class UpdateBrandingCommandHandler(
    IApplicationSettingsRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateBrandingCommand, Result>
{
    public async Task<Result> Handle(UpdateBrandingCommand request, CancellationToken cancellationToken)
    {
        var settings = await repository.GetOrCreateDefaultAsync(cancellationToken);

        try
        {
            settings.UpdateBranding(
                request.PrimaryColor,
                request.SecondaryColor,
                request.BrandTheme,
                request.GalleryTheme,
                request.QrTheme,
                request.DefaultWatermarkProfileId);
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
