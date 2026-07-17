using EventPhoto.Domain.Common;
using EventPhoto.Domain.Exceptions;
using Pgvector;

namespace EventPhoto.Domain.Entities;

/// <summary>
/// Stores the face embedding vector for a single detected face within a photo.
/// One photo can have multiple <see cref="FaceEmbedding"/> records (one per detected face).
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

    /// <summary>Gets the navigation property to the parent photo.</summary>
    public Photo? Photo { get; private set; }

    /// <summary>
    /// Creates a new <see cref="FaceEmbedding"/> record from InsightFace output.
    /// </summary>
    /// <param name="eventId">Parent event identifier.</param>
    /// <param name="photoId">Parent photo identifier.</param>
    /// <param name="embedding">512-dimensional embedding vector from ArcFace.</param>
    /// <param name="boundingBox">JSON-serialised bounding box.</param>
    /// <param name="confidence">Detection confidence (0.0–1.0).</param>
    public static FaceEmbedding Create(
        Guid eventId,
        Guid photoId,
        float[] embedding,
        string boundingBox,
        float confidence)
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
            Confidence = confidence
        };
    }
}
