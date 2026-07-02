using AutoMapper;
using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Contracts.Responses.Events;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Enums;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.Events.Commands;

/// <summary>Command to create a new photography event.</summary>
public sealed record CreateEventCommand(
    string Name,
    string EventType,
    DateOnly EventDate,
    string WatchFolder,
    string? Description,
    string? VenueName,
    string? ClientName,
    Guid CreatedBy) : IRequest<Result<EventResponse>>;

/// <summary>Handles event creation, watch folder setup, QR code generation, and directory initialization.</summary>
public sealed class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, Result<EventResponse>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorageService;
    private readonly IQrCodeService _qrCodeService;
    private readonly ISystemSettingRepository _settingRepository;
    private readonly IMapper _mapper;

    /// <summary>Initializes a new instance of <see cref="CreateEventCommandHandler"/>.</summary>
    public CreateEventCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        IFileStorageService fileStorageService,
        IQrCodeService qrCodeService,
        ISystemSettingRepository settingRepository,
        IMapper mapper)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _fileStorageService = fileStorageService;
        _qrCodeService = qrCodeService;
        _settingRepository = settingRepository;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<Result<EventResponse>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<EventType>(request.EventType, true, out var eventType))
        {
            return Result.Failure<EventResponse>($"Invalid event type: {request.EventType}");
        }

        var serverUrl = await _settingRepository.GetValueAsync("app.serverUrl", cancellationToken) ?? "http://192.168.10.10";
        var thumbnailFolder = Path.Combine(request.WatchFolder, ".thumbnails");
        var qrFolder = Path.Combine(request.WatchFolder, ".qrcodes");

        // Create entity first so we have the ID available for paths/QR URL.
        var eventEntity = Domain.Entities.Event.Create(
            request.Name,
            eventType,
            request.EventDate,
            request.WatchFolder,
            thumbnailFolder,
            request.CreatedBy,
            request.Description,
            request.VenueName,
            request.ClientName);

        var galleryUrl = $"{serverUrl}/gallery/{eventEntity.Id}";
        var qrPath = Path.Combine(qrFolder, $"qr-{eventEntity.Id}.png");

        // Initialise directories and generate QR code BEFORE persisting to the database.
        // This ensures no orphaned DB record is created if file-system operations fail.
        try
        {
            _fileStorageService.EnsureDirectoryExists(request.WatchFolder);
            _fileStorageService.EnsureDirectoryExists(thumbnailFolder);
            _fileStorageService.EnsureDirectoryExists(qrFolder);
            await _qrCodeService.GenerateAsync(galleryUrl, qrPath, eventEntity.Name, cancellationToken);
        }
        catch (Exception ex)
        {
            return Result.Failure<EventResponse>($"Failed to initialise event folders: {ex.Message}");
        }

        eventEntity.SetQrCode(qrPath, galleryUrl);

        await _eventRepository.AddAsync(eventEntity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(_mapper.Map<EventResponse>(eventEntity));
    }
}
