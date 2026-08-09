namespace EventPhoto.Application.Common.Interfaces;

/// <summary>
/// Extended face detection result returned by the Python quality-aware endpoint.
/// Carries quality metrics alongside the embedding vector.
/// </summary>
public sealed record FaceDetectionResultV2(
    float[] Embedding,
    string BoundingBox,
    float Confidence,
    float QualityScore,
    int FaceCountInPhoto,
    float[]? PoseAngles = null);

/// <summary>Extended index result that includes quality metadata per detected face.</summary>
public sealed record IndexPhotoResultV2(
    int FaceCount,
    List<FaceDetectionResultV2> Faces);
