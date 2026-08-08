using EventPhoto.Domain.Common;
using EventPhoto.Domain.Enums;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.ApplicationSettings.Queries;

/// <summary>Returns the current application settings, auto-seeding defaults if none exist.</summary>
public sealed record GetApplicationSettingsQuery : IRequest<Result<ApplicationSettingsDto>>;

/// <summary>Projection of the application settings for use in API responses.</summary>
public sealed record ApplicationSettingsDto(
    Guid Id,
    string StudioName,
    string ServerName,
    string PublicBaseUrl,
    int ServerPort,
    GalleryMode DefaultEventGalleryMode,
    bool EnableWatermarkByDefault,
    bool EnableFaceRecognitionByDefault,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>Handles <see cref="GetApplicationSettingsQuery"/>.</summary>
public sealed class GetApplicationSettingsQueryHandler(
    IApplicationSettingsRepository repository)
    : IRequestHandler<GetApplicationSettingsQuery, Result<ApplicationSettingsDto>>
{
    /// <inheritdoc />
    public async Task<Result<ApplicationSettingsDto>> Handle(
        GetApplicationSettingsQuery request,
        CancellationToken cancellationToken)
    {
        var settings = await repository.GetOrCreateDefaultAsync(cancellationToken);
        return Result.Success(ToDto(settings));
    }

    private static ApplicationSettingsDto ToDto(Domain.Entities.ApplicationSettings s) => new(
        s.Id,
        s.StudioName,
        s.ServerName,
        s.PublicBaseUrl,
        s.ServerPort,
        s.DefaultEventGalleryMode,
        s.EnableWatermarkByDefault,
        s.EnableFaceRecognitionByDefault,
        s.CreatedAt,
        s.UpdatedAt);
}
