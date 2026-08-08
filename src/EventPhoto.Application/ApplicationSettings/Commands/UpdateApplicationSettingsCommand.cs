using EventPhoto.Domain.Common;
using EventPhoto.Domain.Enums;
using EventPhoto.Domain.Exceptions;
using EventPhoto.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace EventPhoto.Application.ApplicationSettings.Commands;

/// <summary>Command to update the application settings.</summary>
public sealed record UpdateApplicationSettingsCommand(
    string StudioName,
    string ServerName,
    string PublicBaseUrl,
    int ServerPort,
    GalleryMode DefaultEventGalleryMode,
    bool EnableWatermarkByDefault,
    bool EnableFaceRecognitionByDefault) : IRequest<Result>;

/// <summary>Validates <see cref="UpdateApplicationSettingsCommand"/>.</summary>
public sealed class UpdateApplicationSettingsCommandValidator : AbstractValidator<UpdateApplicationSettingsCommand>
{
    public UpdateApplicationSettingsCommandValidator()
    {
        RuleFor(x => x.StudioName)
            .NotEmpty().WithMessage("Studio name is required.")
            .MaximumLength(200).WithMessage("Studio name must not exceed 200 characters.");

        RuleFor(x => x.ServerName)
            .NotEmpty().WithMessage("Server name is required.")
            .MaximumLength(100).WithMessage("Server name must not exceed 100 characters.");

        RuleFor(x => x.PublicBaseUrl)
            .NotEmpty().WithMessage("Public base URL is required.")
            .Must(url =>
            {
                if (!Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var uri)) return false;
                return uri.Scheme is "http" or "https";
            })
            .WithMessage("Public base URL must be a valid http:// or https:// URL.");

        RuleFor(x => x.ServerPort)
            .InclusiveBetween(1, 65535)
            .WithMessage("Server port must be between 1 and 65535.");
    }
}

/// <summary>Handles <see cref="UpdateApplicationSettingsCommand"/>.</summary>
public sealed class UpdateApplicationSettingsCommandHandler(
    IApplicationSettingsRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateApplicationSettingsCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> Handle(
        UpdateApplicationSettingsCommand request,
        CancellationToken cancellationToken)
    {
        var settings = await repository.GetOrCreateDefaultAsync(cancellationToken);

        try
        {
            settings.Update(
                request.StudioName,
                request.ServerName,
                request.PublicBaseUrl,
                request.ServerPort,
                request.DefaultEventGalleryMode,
                request.EnableWatermarkByDefault,
                request.EnableFaceRecognitionByDefault);
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
