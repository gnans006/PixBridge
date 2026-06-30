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
        CreateMap<Event, EventResponse>()
            .ForMember(d => d.EventType, o => o.MapFrom(s => s.EventType.ToString()))
            .ForMember(d => d.TotalSize, o => o.MapFrom(s => FormatBytes(s.TotalSizeBytes)));

        CreateMap<Event, EventSummaryResponse>()
            .ForMember(d => d.EventType, o => o.MapFrom(s => s.EventType.ToString()));

        CreateMap<Photo, PhotoResponse>()
            .ForMember(d => d.ThumbnailUrl, o => o.MapFrom(s => $"/api/photos/{s.Id}/thumbnail"))
            .ForMember(d => d.OriginalUrl, o => o.MapFrom(s => $"/api/photos/{s.Id}/download"))
            .ForMember(d => d.ThumbnailStatus, o => o.MapFrom(s => s.ThumbnailStatus.ToString()));

        CreateMap<Photo, PhotoSummaryResponse>()
            .ForMember(d => d.ThumbnailStatus, o => o.MapFrom(s => s.ThumbnailStatus.ToString()));

        CreateMap<SystemSetting, SystemSettingResponse>();
    }

    private static string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1_048_576 => $"{bytes / 1024.0:F1} KB",
        < 1_073_741_824 => $"{bytes / 1_048_576.0:F1} MB",
        _ => $"{bytes / 1_073_741_824.0:F2} GB"
    };
}

