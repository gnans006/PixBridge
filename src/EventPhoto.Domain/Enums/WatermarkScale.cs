namespace EventPhoto.Domain.Enums;

/// <summary>
/// Controls the relative size of the watermark text and logo.
/// </summary>
public enum WatermarkScale
{
    /// <summary>Small watermark — approximately 3 % of the shorter image dimension.</summary>
    Small = 0,

    /// <summary>Medium watermark — approximately 5 % of the shorter image dimension.</summary>
    Medium = 1,

    /// <summary>Large watermark — approximately 8 % of the shorter image dimension.</summary>
    Large = 2,

    /// <summary>
    /// Automatically choose the scale based on image aspect ratio.
    /// Landscape images use Small; square and portrait images use Medium.
    /// </summary>
    Auto = 3,
}
