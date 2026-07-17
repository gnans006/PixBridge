using EventPhoto.Domain.Common;
using EventPhoto.Domain.Exceptions;

namespace EventPhoto.Domain.Entities;

/// <summary>
/// Records a single photo match returned by a guest face-search session.
/// </summary>
public sealed class PhotoMatch : Entity
{
    private PhotoMatch()
    {
    }

    /// <summary>Gets the identifier of the guest face-search session.</summary>
    public Guid SessionId { get; private set; }

    /// <summary>Gets the identifier of the matched photo.</summary>
    public Guid PhotoId { get; private set; }

    /// <summary>
    /// Gets the cosine similarity score between the guest's selfie embedding
    /// and the closest face embedding in this photo (0.0–1.0; higher = more similar).
    /// </summary>
    public float SimilarityScore { get; private set; }

    /// <summary>Gets the navigation property to the parent session.</summary>
    public GuestFaceSession? Session { get; private set; }

    /// <summary>Gets the navigation property to the matched photo.</summary>
    public Photo? Photo { get; private set; }

    /// <summary>
    /// Creates a new <see cref="PhotoMatch"/> record.
    /// </summary>
    /// <param name="sessionId">Parent session identifier.</param>
    /// <param name="photoId">Matched photo identifier.</param>
    /// <param name="similarityScore">Cosine similarity score (0.0–1.0).</param>
    public static PhotoMatch Create(Guid sessionId, Guid photoId, float similarityScore)
    {
        if (sessionId == Guid.Empty)
            throw new DomainException("SessionId is required.");

        if (photoId == Guid.Empty)
            throw new DomainException("PhotoId is required.");

        if (similarityScore is < 0f or > 1f)
            throw new DomainException("Similarity score must be between 0 and 1.");

        return new PhotoMatch
        {
            SessionId = sessionId,
            PhotoId = photoId,
            SimilarityScore = similarityScore
        };
    }
}
