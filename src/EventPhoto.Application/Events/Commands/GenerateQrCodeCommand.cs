using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.Events.Commands;

/// <summary>
/// Command that generates a QR code for an event and stores the result on the aggregate.
/// </summary>
/// <param name="EventId">The event identifier.</param>
/// <param name="QrCodeUrl">The URL that will be encoded into the QR code.</param>
/// <param name="OutputPath">Absolute path where the QR code PNG image should be written.</param>
public sealed record GenerateQrCodeCommand(
    Guid EventId,
    string QrCodeUrl,
    string OutputPath)
    : IRequest<Result<string>>;

/// <summary>
/// Handles the <see cref="GenerateQrCodeCommand"/>.
/// </summary>
public sealed class GenerateQrCodeCommandHandler(
    IEventRepository eventRepository,
    IQrCodeService qrCodeService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<GenerateQrCodeCommand, Result<string>>
{
    /// <inheritdoc />
    public async Task<Result<string>> Handle(
        GenerateQrCodeCommand request,
        CancellationToken cancellationToken)
    {
        var eventEntity = await eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (eventEntity is null)
        {
            return Result.Failure<string>($"Event '{request.EventId}' was not found.");
        }

        var resolvedPath = await qrCodeService.GenerateQrCodeAsync(
            request.QrCodeUrl,
            request.OutputPath,
            cancellationToken);

        eventEntity.SetQrCode(resolvedPath, request.QrCodeUrl);
        await eventRepository.UpdateAsync(eventEntity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(resolvedPath);
    }
}
