namespace EventPhoto.Domain.Enums;

/// <summary>
/// Defines the source of content for watermark rendering.
/// </summary>
public enum WatermarkMode
{
    /// <summary>Watermarking is disabled.</summary>
    Disabled = 0,

    /// <summary>Watermark displays the studio name and optional logo.</summary>
    StudioBranding = 1,

    /// <summary>Watermark displays the event name and date.</summary>
    EventBranding = 2,

    /// <summary>Watermark combines studio name, logo, and event name.</summary>
    StudioAndEvent = 3,

    /// <summary>Watermark renders a user-supplied static text string.</summary>
    CustomText = 4,

    /// <summary>Watermark renders a tokenised template string at download time.</summary>
    DynamicTemplate = 5,
}
