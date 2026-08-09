using EventPhoto.Domain.Common;
using EventPhoto.Domain.Enums;
using EventPhoto.Domain.Exceptions;
using Pgvector;

namespace EventPhoto.Domain.Entities;

/// <summary>
/// Stores the face embedding vector for a single detected face within a photo.
/// One photo can have multiple <see cref="FaceEmbedding"/> records (one per detected face).
///
/// <para>Quality metadata is stored alongside the vector so low-quality embeddings
/// can be excluded from search without re-processing.</para>
/// </summary>
public sealed class FaceEmbedding : Entity
{
    private FaceEmbedding()
    {
    }

    /// <summary>Gets the identifier of the parent event.</summary>
    public Guid EventId { get; private set; }

    /// <summary>Gets the identifier of the photo containing this face.</summary>
    public Guid PhotoId { get; private set; }

    /// <summary>
    /// Gets the 512-dimensional ArcFace embedding vector.
    /// Stored as pgvector <c>vector(512)</c> — mapped via Pgvector.EntityFrameworkCore.
    /// </summary>
    public Vector Embedding { get; private set; } = new Vector(new float[512]);

    /// <summary>
    /// Gets the bounding-box of the detected face as a JSON string.
    /// Format: <c>{"x":int,"y":int,"width":int,"height":int}</c>
    /// </summary>
    public string BoundingBox { get; private set; } = string.Empty;

    /// <summary>Gets the InsightFace detection confidence score (0.0 – 1.0).</summary>
    public float Confidence { get; private set; }

    /// <summary>
    /// Gets the composite quality score for this face (0–100).
    /// Derived from blur, brightness, resolution, pose, occlusion, and face-size metrics.
    /// </summary>
    public float QualityScore { get; private set; }

    /// <summary>Gets the quality tier classification based on <see cref="QualityScore"/>.</summary>
    public QualityTier QualityTier { get; private set; } = QualityTier.Unscored;

    /// <summary>
    /// Gets the total number of faces detected in the source photo at the time of indexing.
    /// Used to flag crowd/group shots for clustering heuristics.
    /// </summary>
    public int FaceCountInPhoto { get; private set; } = 1;

    /// <summary>
    /// Gets the embedding model version used to generate this vector.
    /// Enables targeted re-indexing when a newer model is deployed.
    /// </summary>
    public string EmbeddingVersion { get; private set; } = "arcface-512-v1";

    /// <summary>Gets the navigation property to the parent photo.</summary>
    public Photo? Photo { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new <see cref="FaceEmbedding"/> record from InsightFace output.
    /// </summary>
    /// <param name="eventId">Parent event identifier.</param>
    /// <param name="photoId">Parent photo identifier.</param>
    /// <param name="embedding">512-dimensional embedding vector from ArcFace.</param>
    /// <param name="boundingBox">JSON-serialised bounding box.</param>
    /// <param name="confidence">Detection confidence (0.0–1.0).</param>
    /// <param name="qualityScore">Composite quality score (0–100).</param>
    /// <param name="qualityTier">Quality tier classification.</param>
    /// <param name="faceCountInPhoto">Number of faces detected in the source photo.</param>
    /// <param name="embeddingVersion">Model version string.</param>
    public static FaceEmbedding Create(
        Guid eventId,
        Guid photoId,
        float[] embedding,
        string boundingBox,
        float confidence,
        float qualityScore = 50f,
        QualityTier qualityTier = QualityTier.Medium,
        int faceCountInPhoto = 1,
        string embeddingVersion = "arcface-512-v1")
    {
        if (eventId == Guid.Empty)
            throw new DomainException("EventId is required.");

        if (photoId == Guid.Empty)
            throw new DomainException("PhotoId is required.");

        if (embedding is null || embedding.Length != 512)
            throw new DomainException("Embedding must be a 512-dimensional vector.");

        if (confidence is < 0f or > 1f)
            throw new DomainException("Confidence must be between 0 and 1.");

        return new FaceEmbedding
        {
            EventId = eventId,
            PhotoId = photoId,
            Embedding = new Vector(embedding),
            BoundingBox = boundingBox,
            Confidence = confidence,
            QualityScore = Math.Clamp(qualityScore, 0f, 100f),
            QualityTier = qualityTier,
            FaceCountInPhoto = Math.Max(1, faceCountInPhoto),
            EmbeddingVersion = embeddingVersion
        };
    }
}
