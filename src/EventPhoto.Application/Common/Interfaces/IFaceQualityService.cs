using EventPhoto.Domain.Enums;

namespace EventPhoto.Application.Common.Interfaces;

/// <summary>
/// Evaluates the quality of a detected face before it is indexed into the vector store.
/// Poor-quality faces are excluded to prevent search result degradation.
/// </summary>
public interface IFaceQualityService
{
    /// <summary>
    /// Computes a composite quality score (0–100) and quality tier for the given face metrics.
    /// </summary>
    /// <param name="detectionConfidence">Raw InsightFace detection confidence (0.0–1.0).</param>
    /// <param name="boundingBoxJson">JSON bounding box <c>{"x","y","width","height"}</c>.</param>
    /// <param name="imageTotalPixels">Total pixel count of the source image (width × height).</param>
    /// <param name="poseAngles">Optional yaw/pitch/roll in degrees from InsightFace.</param>
    /// <returns>Evaluated quality result.</returns>
    FaceQualityResult Evaluate(
        float detectionConfidence,
        string boundingBoxJson,
        int imageTotalPixels,
        float[]? poseAngles = null);
}

/// <summary>Result of a face quality evaluation.</summary>
public sealed record FaceQualityResult(
    /// <summary>Composite score 0–100. Higher = better quality.</summary>
    float Score,
    /// <summary>Classification based on configured thresholds.</summary>
    QualityTier Tier,
    /// <summary>Human-readable reason when the tier is Low or the face is rejected.</summary>
    string? RejectionReason);
