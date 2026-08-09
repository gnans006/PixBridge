namespace EventPhoto.Domain.Enums;

/// <summary>
/// Guest-facing confidence category for a face-search match.
/// Raw similarity scores are NEVER shown to guests — only these human-readable categories.
/// </summary>
public enum MatchConfidenceCategory
{
    /// <summary>Similarity ≥ 0.90 — very high confidence this is the same person.</summary>
    Excellent = 0,

    /// <summary>Similarity ≥ 0.80 — high confidence match.</summary>
    Strong = 1,

    /// <summary>Similarity ≥ configured threshold (typically 0.65) — plausible match.</summary>
    Possible = 2
}
