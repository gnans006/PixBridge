namespace EventPhoto.Application.Common.Interfaces;

/// <summary>
/// DTOs used to communicate with the Python PixBridge.FaceRecognition FastAPI service.
/// </summary>
public sealed record FaceDetectionResult(
    float[] Embedding,
    string BoundingBox,
    float Confidence);

public sealed record IndexPhotoResult(
    int FaceCount,
    List<FaceDetectionResult> Faces);

public sealed record EmbeddingResult(float[] Embedding);

/// <summary>
/// Contract for the local Python InsightFace service.
/// Implemented in Infrastructure by <c>FaceRecognitionService</c> (HttpClient-based).
/// </summary>
public interface IFaceRecognitionService
{
    /// <summary>
    /// Detects all faces in the given image file and returns one embedding per face.
    /// Calls <c>POST /index-photo</c> on the Python service.
    /// </summary>
    /// <param name="imagePath">Absolute path to the original photo on disk.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IndexPhotoResult> IndexPhotoAsync(string imagePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a 512-dimensional ArcFace embedding from a selfie image byte array.
    /// Calls <c>POST /generate-embedding</c> on the Python service.
    /// </summary>
    /// <param name="selfieBytes">Raw bytes of the uploaded selfie image.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<EmbeddingResult> GenerateEmbeddingAsync(byte[] selfieBytes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a health check against the Python service.
    /// Returns <c>true</c> when the service is online and ready.
    /// </summary>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}
