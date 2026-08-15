using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EventPhoto.Application.Deployment.Commands;

/// <summary>
/// Regenerates QR code PNG files for every non-deleted event using the current
/// <c>ApplicationSettings.PublicBaseUrl</c>. Skips events that have no <c>QrCodePath</c>.
/// </summary>
public sealed record RegenerateAllQrCodesCommand : IRequest<Result<int>>;

/// <summary>Handles <see cref="RegenerateAllQrCodesCommand"/>.</summary>
public sealed class RegenerateAllQrCodesCommandHandler(
    IEventRepository eventRepository,
    IQrCodeService qrCodeService,
    IUrlGenerationService urlGenerationService,
    IUnitOfWork unitOfWork,
    ILogger<RegenerateAllQrCodesCommandHandler> logger)
    : IRequestHandler<RegenerateAllQrCodesCommand, Result<int>>
{
    /// <inheritdoc />
    public async Task<Result<int>> Handle(
        RegenerateAllQrCodesCommand request,
        CancellationToken cancellationToken)
    {
        var events = await eventRepository.GetAllAsync(cancellationToken);

        var candidates = events
            .Where(e => !e.IsDeleted && !string.IsNullOrWhiteSpace(e.QrCodePath))
            .ToList();

        var count = 0;
        foreach (var ev in candidates)
        {
            try
            {
                var galleryUrl = await urlGenerationService.GenerateGalleryUrlAsync(ev.Id, cancellationToken);
                await qrCodeService.GenerateAsync(galleryUrl, ev.QrCodePath!, ev.Name, cancellationToken);
                ev.SetQrCode(ev.QrCodePath!, galleryUrl);
                await eventRepository.UpdateAsync(ev, cancellationToken);
                count++;
                logger.LogInformation("QR regenerated: {Event} → {Url}", ev.Name, galleryUrl);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to regenerate QR for event {EventId}", ev.Id);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(count);
    }
}
