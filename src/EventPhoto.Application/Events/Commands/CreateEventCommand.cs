using AutoMapper;
using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Application.Common.Models;
using EventPhoto.Contracts.Responses.Events;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Enums;
using EventPhoto.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
    Guid CreatedBy,
    int? GalleryRecentCount,
    bool EnableFaceRecognition = false,
    bool AllowGalleryBrowsing = true,
    bool AllowFaceSearch = false,
    bool RestrictDownloadsToMatchedPhotos = false,
    float FaceMatchThreshold = 0.75f) : IRequest<Result<EventResponse>>, IRequiresFeature
{
    /// <inheritdoc />
    public string FeatureKey => Common.Models.FeatureKey.Events;
}

/// <summary>Handles event creation, watch folder setup, QR code generation, and directory initialization.</summary>
public sealed class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, Result<EventResponse>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorageService;
    private readonly IQrCodeService _qrCodeService;
    private readonly IUrlGenerationService _urlGenerationService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateEventCommandHandler> _logger;

    /// <summary>Initializes a new instance of <see cref="CreateEventCommandHandler"/>.</summary>
    public CreateEventCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        IFileStorageService fileStorageService,
        IQrCodeService qrCodeService,
        IUrlGenerationService urlGenerationService,
        IServiceScopeFactory scopeFactory,
        IMapper mapper,
        ILogger<CreateEventCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _fileStorageService = fileStorageService;
        _qrCodeService = qrCodeService;
        _urlGenerationService = urlGenerationService;
        _scopeFactory = scopeFactory;
        _mapper = mapper;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<EventResponse>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<EventType>(request.EventType, true, out var eventType))
        {
            return Result.Failure<EventResponse>($"Invalid event type: {request.EventType}");
        }

        var serverUrl = await _urlGenerationService.GetPublicBaseUrlAsync(cancellationToken);
        var thumbnailFolder = Path.Combine(request.WatchFolder, ".thumbnails");
        var qrFolder = Path.Combine(request.WatchFolder, ".qrcodes");

        var eventEntity = Domain.Entities.Event.Create(
            request.Name,
            eventType,
            request.EventDate,
            request.WatchFolder,
            thumbnailFolder,
            request.CreatedBy,
            request.Description,
            request.VenueName,
            request.ClientName,
            request.GalleryRecentCount,
            request.EnableFaceRecognition,
            request.AllowGalleryBrowsing,
            request.AllowFaceSearch,
            request.RestrictDownloadsToMatchedPhotos,
            request.FaceMatchThreshold);

        var galleryUrl = $"{serverUrl}/gallery/{eventEntity.Id}";
        var qrPath = Path.Combine(qrFolder, $"qr-{eventEntity.Id}.png");

        // Create directories synchronously — fast local disk op.
        try
        {
            _fileStorageService.EnsureDirectoryExists(request.WatchFolder);
            _fileStorageService.EnsureDirectoryExists(thumbnailFolder);
            _fileStorageService.EnsureDirectoryExists(qrFolder);
        }
        catch (Exception ex)
        {
            return Result.Failure<EventResponse>($"Failed to initialise event folders: {ex.Message}");
        }

        // Persist the event immediately — do NOT block on QR generation.
        // QR code is generated in the background after the HTTP response is returned.
        // The gallery URL is stored now so the QR path is deterministic.
        eventEntity.SetQrCode(qrPath, galleryUrl);

        await _eventRepository.AddAsync(eventEntity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Fire-and-forget QR generation on a background thread with its own DI scope.
        // Failures are logged but never propagate to the caller.
        var capturedId   = eventEntity.Id;
        var capturedName = eventEntity.Name;
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope   = _scopeFactory.CreateScope();
                var qrService     = scope.ServiceProvider.GetRequiredService<IQrCodeService>();
                await qrService.GenerateAsync(galleryUrl, qrPath, capturedName, CancellationToken.None);
                _logger.LogInformation("QR code generated for event {EventId}.", capturedId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background QR generation failed for event {EventId}. " +
                    "Use the Refresh QR action in the event workspace to retry.", capturedId);
            }
        });

        return Result.Success(_mapper.Map<EventResponse>(eventEntity));
    }
}
