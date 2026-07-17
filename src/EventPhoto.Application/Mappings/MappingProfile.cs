using AutoMapper;
using EventPhoto.Contracts.Responses.Events;
using EventPhoto.Contracts.Responses.Photos;
using EventPhoto.Contracts.Responses.Settings;
using EventPhoto.Domain.Entities;

namespace EventPhoto.Application.Mappings;

/// <summary>AutoMapper profile for domain entity → contract response mappings.</summary>
public sealed class MappingProfile : Profile
{
    /// <summary>Initializes a new instance of <see cref="MappingProfile"/>.</summary>
    public MappingProfile()
    {
        // AutoMapper 16.x requires explicit ConstructUsing for positional records
        // (records with no parameterless constructor). ForAllMembers(Ignore) stops
        // AutoMapper from attempting property-level mapping after construction.

        CreateMap<Event, EventResponse>()
            .ConstructUsing(s => new EventResponse(
                s.Id,
                s.Name,
                s.Description,
                s.EventType.ToString(),
                s.EventDate,
                s.VenueName,
                s.ClientName,
                s.WatchFolder,
                s.QrCodeUrl,
                s.IsActive,
                s.PhotoCount,
                FormatBytes(s.TotalSizeBytes),
                s.CreatedAt,
                s.GalleryRecentCount,
                s.EnableFaceRecognition,
                s.AllowGalleryBrowsing,
                s.AllowFaceSearch,
                s.RestrictDownloadsToMatchedPhotos,
                s.FaceMatchThreshold))
            .ForAllMembers(opt => opt.Ignore());

        CreateMap<Event, EventSummaryResponse>()
            .ConstructUsing(s => new EventSummaryResponse(
                s.Id,
                s.Name,
                s.EventType.ToString(),
                s.EventDate,
                s.ClientName,
                s.IsActive,
                s.PhotoCount))
            .ForAllMembers(opt => opt.Ignore());

        CreateMap<Photo, PhotoResponse>()
            .ConstructUsing(s => new PhotoResponse(
                s.Id,
                s.EventId,
                s.FileName,
                $"/api/photos/{s.Id}/thumbnail",
                $"/api/photos/{s.Id}/download",
                s.FileSizeBytes,
                s.Width,
                s.Height,
                s.TakenAt,
                s.CapturedAt,
                s.DownloadCount,
                s.ThumbnailStatus.ToString()))
            .ForAllMembers(opt => opt.Ignore());

        CreateMap<Photo, PhotoSummaryResponse>()
            .ConstructUsing(s => new PhotoSummaryResponse(
                s.Id,
                s.FileName,
                s.ThumbnailPath,
                s.MimeType,
                s.CapturedAt,
                s.ThumbnailStatus.ToString()))
            .ForAllMembers(opt => opt.Ignore());

        CreateMap<SystemSetting, SystemSettingResponse>()
            .ConstructUsing(s => new SystemSettingResponse(
                s.Id,
                s.Key,
                s.Value,
                s.Description))
            .ForAllMembers(opt => opt.Ignore());
    }

    private static string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1_048_576 => $"{bytes / 1024.0:F1} KB",
        < 1_073_741_824 => $"{bytes / 1_048_576.0:F1} MB",
        _ => $"{bytes / 1_073_741_824.0:F2} GB"
    };
}

