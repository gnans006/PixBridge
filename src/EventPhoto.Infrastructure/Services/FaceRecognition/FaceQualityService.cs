using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Domain.Enums;
using System.Text.Json;

namespace EventPhoto.Infrastructure.Services.FaceRecognition;

/// <summary>
/// Evaluates composite quality score for a detected face based on configurable thresholds.
/// No external service call — all computation is done in-process.
///
/// <para>Scoring breakdown (total 100 points):</para>
/// <list type="bullet">
///   <item>Detection confidence: up to 50 pts</item>
///   <item>Face size ratio: up to 25 pts</item>
///   <item>Pose (yaw/pitch/roll): up to 25 pts</item>
/// </list>
/// </summary>
public sealed class FaceQualityService : IFaceQualityService
{
    // Quality tier thresholds — configurable in future via IOptions<>
    private const float HighTierThreshold = 70f;
    private const float MediumTierThreshold = 40f;

    // Face size: face area / image area (faces < 2% of frame are too small)
    private const double MinFaceSizeRatio = 0.02;
    private const double GoodFaceSizeRatio = 0.05;

    // Pose angles: faces rotated > 35° lose embedding quality
    private const float MaxPoseAngle = 35f;

    /// <inheritdoc />
    public FaceQualityResult Evaluate(
        float detectionConfidence,
        string boundingBoxJson,
        int imageTotalPixels,
        float[]? poseAngles = null)
    {
        var score = 0f;
        string? rejectionReason = null;

        // ── Component 1: Detection confidence (0–50 pts) ──────────────────────
        score += Math.Clamp(detectionConfidence * 50f, 0f, 50f);

        // ── Component 2: Face size ratio (0–25 pts) ────────────────────────────
        if (imageTotalPixels > 0 && !string.IsNullOrEmpty(boundingBoxJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(boundingBoxJson);
                var root = doc.RootElement;
                var w = root.GetProperty("width").GetInt32();
                var h = root.GetProperty("height").GetInt32();
                var faceArea = (double)(w * h);
                var ratio = faceArea / imageTotalPixels;

                if (ratio >= GoodFaceSizeRatio)
                    score += 25f;
                else if (ratio >= MinFaceSizeRatio)
                    score += (float)(ratio / GoodFaceSizeRatio * 25f);
                else
                    rejectionReason = $"Face too small (occupies {ratio * 100:F1}% of frame).";
            }
            catch (JsonException)
            {
                // Cannot parse bounding box — skip size component
            }
        }

        // ── Component 3: Pose angles (0–25 pts) ────────────────────────────────
        if (poseAngles is { Length: >= 3 })
        {
            var maxAngle = poseAngles.Take(3).Max(Math.Abs);
            if (maxAngle <= MaxPoseAngle)
                score += 25f * (1f - maxAngle / MaxPoseAngle);
            else
                rejectionReason ??= $"Face pose too extreme ({maxAngle:F0}° from frontal).";
        }
        else
        {
            // No pose data — grant partial credit (assume frontal)
            score += 12f;
        }

        var tier = score switch
        {
            >= HighTierThreshold => QualityTier.High,
            >= MediumTierThreshold => QualityTier.Medium,
            _ => QualityTier.Low
        };

        return new FaceQualityResult(
            Score: Math.Clamp(score, 0f, 100f),
            Tier: tier,
            RejectionReason: tier == QualityTier.Low ? (rejectionReason ?? "Quality score too low.") : null);
    }
}
