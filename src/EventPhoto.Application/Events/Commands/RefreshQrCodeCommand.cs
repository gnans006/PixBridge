using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.Events.Commands;

/// <summary>
/// Command that regenerates the QR code for an event using the current server URL.
/// </summary>
/// <param name="EventId">The event identifier.</param>
public sealed record RefreshQrCodeCommand(Guid EventId) : IRequest<Result>;

/// <summary>
/// Handles the <see cref="RefreshQrCodeCommand"/>.
/// </summary>
public sealed class RefreshQrCodeCommandHandler(
    IEventRepository eventRepository,
    ISystemSettingRepository settingRepository,
    IQrCodeService qrCodeService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RefreshQrCodeCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> Handle(RefreshQrCodeCommand request, CancellationToken cancellationToken)
    {
        var eventEntity = await eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (eventEntity is null)
            return Result.Failure($"Event '{request.EventId}' was not found.");

        var serverUrl = await settingRepository.GetValueAsync("app.serverUrl", cancellationToken)
                        ?? "http://localhost:5000";

        var galleryUrl = $"{serverUrl}/gallery/{eventEntity.Id}";

        // Reuse the existing QR path or derive a default one
        var qrPath = !string.IsNullOrWhiteSpace(eventEntity.QrCodePath)
            ? eventEntity.QrCodePath
            : System.IO.Path.Combine(eventEntity.WatchFolder, ".qrcodes", $"qr-{eventEntity.Id}.png");

        await qrCodeService.GenerateAsync(galleryUrl, qrPath, eventEntity.Name, cancellationToken);
        eventEntity.SetQrCode(qrPath, galleryUrl);
        await eventRepository.UpdateAsync(eventEntity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
