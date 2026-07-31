namespace EventPhoto.Domain.Enums;

/// <summary>
/// Controls the visual placement and repetition of the watermark.
/// </summary>
public enum WatermarkStyle
{
    /// <summary>Single watermark stamp placed in the bottom-right corner.</summary>
    Corner = 0,

    /// <summary>Single watermark stamp placed at the centre of the image.</summary>
    Center = 1,

    /// <summary>Single watermark stamp drawn diagonally across the image.</summary>
    Diagonal = 2,

    /// <summary>Watermark text tiled repeatedly across the entire image surface.</summary>
    RepeatedPattern = 3,

    /// <summary>
    /// Premium full-width ribbon at the bottom with a semi-transparent dark background.
    /// Studio name (larger) sits above event name (smaller), both centred.
    /// </summary>
    BottomRibbon = 4,
}
