using EventPhoto.Domain.Common;
using EventPhoto.Domain.Exceptions;
using Pgvector;

namespace EventPhoto.Domain.Entities;

/// <summary>
/// Represents a cluster of face embeddings within an event that likely belong to the same person.
/// Future-ready architecture for smart album features (Find Bride™, Find Groom™, VIP Detection™).
///
/// <para>Clustering is computed offline by the AI Discovery Pipeline and does NOT block search.</para>
/// </summary>
public sealed class FaceCluster : Entity
{
    private FaceCluster()
    {
    }

    /// <summary>Gets the parent event identifier.</summary>
    public Guid EventId { get; private set; }

    /// <summary>
    /// Gets the representative 512-dimensional embedding vector for this cluster.
    /// Computed as the centroid of all member embeddings.
    /// </summary>
    public Vector RepresentativeEmbedding { get; private set; } = new Vector(new float[512]);

    /// <summary>Gets the number of photos containing a member of this cluster.</summary>
    public int PhotoCount { get; private set; }

    /// <summary>
    /// Gets an optional human-readable label assigned by the studio operator.
    /// Examples: "Bride", "Groom", "VIP Guest", "Family Group A".
    /// </summary>
    public string? Label { get; private set; }

    /// <summary>
    /// Gets the average quality score (0–100) of embeddings in this cluster.
    /// Higher quality clusters produce more accurate searches.
    /// </summary>
    public float AverageQualityScore { get; private set; }

    /// <summary>Gets the parent event navigation property.</summary>
    public Event? Event { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new <see cref="FaceCluster"/> from the centroid of its member embeddings.
    /// </summary>
    public static FaceCluster Create(
        Guid eventId,
        float[] representativeEmbedding,
        int photoCount,
        float averageQualityScore)
    {
        if (eventId == Guid.Empty)
            throw new DomainException("EventId is required.");

        if (representativeEmbedding is null || representativeEmbedding.Length != 512)
            throw new DomainException("Representative embedding must be a 512-dimensional vector.");

        if (photoCount < 1)
            throw new DomainException("A cluster must contain at least one photo.");

        return new FaceCluster
        {
            EventId = eventId,
            RepresentativeEmbedding = new Vector(representativeEmbedding),
            PhotoCount = photoCount,
            AverageQualityScore = Math.Clamp(averageQualityScore, 0f, 100f)
        };
    }

    // ── Behaviour ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Updates the cluster centroid and member count after re-clustering.
    /// </summary>
    public void Update(float[] newCentroid, int newPhotoCount, float newAverageQualityScore)
    {
        if (newCentroid is null || newCentroid.Length != 512)
            throw new DomainException("Centroid must be a 512-dimensional vector.");

        RepresentativeEmbedding = new Vector(newCentroid);
        PhotoCount = newPhotoCount;
        AverageQualityScore = Math.Clamp(newAverageQualityScore, 0f, 100f);
        Touch();
    }

    /// <summary>Assigns a human-readable label to this cluster (e.g., "Bride").</summary>
    public void AssignLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
            throw new DomainException("Label cannot be empty.");

        Label = label.Trim();
        Touch();
    }
}
