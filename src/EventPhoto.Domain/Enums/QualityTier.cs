namespace EventPhoto.Domain.Enums;

/// <summary>
/// Quality classification assigned to a detected face before indexing.
/// Low-quality faces are filtered to avoid polluting search results.
/// </summary>
public enum QualityTier
{
    /// <summary>Score ≥ 70 — index immediately for highest search accuracy.</summary>
    High = 0,

    /// <summary>Score 40–69 — index normally; may produce weaker matches.</summary>
    Medium = 1,

    /// <summary>Score &lt; 40 — flag for review; skip indexing by default.</summary>
    Low = 2,

    /// <summary>Quality evaluation not yet performed.</summary>
    Unscored = 3
}
