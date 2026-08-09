using EventPhoto.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace EventPhoto.Infrastructure.Services.FaceRecognition;

// ── Internal DTOs for the Python FastAPI service ──────────────────────────────

file sealed record IndexPhotoApiRequest(string image_path);
file sealed record IndexPhotoApiResponse(int face_count, List<FaceApiResult> faces);
file sealed record FaceApiResult(float[] embedding, string bounding_box, float confidence, float[]? pose_angles = null);
file sealed record EmbeddingApiResponse(float[] embedding);

/// <summary>
/// HTTP client wrapper for the local Python PixBridge.FaceRecognition FastAPI service.
/// Uses a named HttpClient registered with <c>AddHttpClient("FaceRecognition")</c>
/// and a Polly retry + circuit-breaker policy.
/// </summary>
public sealed class FaceRecognitionService(
    IHttpClientFactory httpClientFactory,
    ILogger<FaceRecognitionService> logger)
    : IFaceRecognitionService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private HttpClient Client => httpClientFactory.CreateClient("FaceRecognition");

    private static bool IsLikelyImage(byte[] bytes)
    {
        if (bytes is null || bytes.Length < 16)
            return false;

        var header = bytes.Take(16).ToArray();
        return header.Length >= 8 && (
            (header[0] == 0xFF && header[1] == 0xD8) ||
            (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47) ||
            (header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46) ||
            (header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46));
    }

    /// <inheritdoc />
    public async Task<IndexPhotoResult> IndexPhotoAsync(
        string imagePath,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Calling FaceRecognition /index-photo for {Path}", imagePath);

        var response = await Client.PostAsJsonAsync(
            "/index-photo",
            new IndexPhotoApiRequest(imagePath),
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content
            .ReadFromJsonAsync<IndexPhotoApiResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Empty response from FaceRecognition service.");

        return new IndexPhotoResult(
            result.face_count,
            result.faces.Select(f => new FaceDetectionResult(f.embedding, f.bounding_box, f.confidence, f.pose_angles)).ToList());
    }

    /// <inheritdoc />
    public Task<FaceSearchPrecheckResult> PrecheckSelfieAsync(byte[] selfieBytes, CancellationToken cancellationToken = default)
    {
        if (selfieBytes is null || selfieBytes.Length < 16)
            return Task.FromResult(new FaceSearchPrecheckResult(false, "Selfie image is required. Please upload a non-empty image."));

        if (!IsLikelyImage(selfieBytes))
            return Task.FromResult(new FaceSearchPrecheckResult(false, "The uploaded file is not a valid image. Please upload a JPG, PNG, or WEBP image."));

        if (selfieBytes.Length > 10 * 1024 * 1024)
            return Task.FromResult(new FaceSearchPrecheckResult(false, "Selfie image must not exceed 10 MB."));

        return Task.FromResult(new FaceSearchPrecheckResult(true));
    }

    /// <inheritdoc />
    public async Task<EmbeddingResult> GenerateEmbeddingAsync(
        byte[] selfieBytes,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Calling FaceRecognition /generate-embedding for selfie ({Bytes} bytes)", selfieBytes.Length);

        using var content = new MultipartFormDataContent();
        // Detect MIME type from magic bytes so FastAPI's content_type check passes
        var mimeType = selfieBytes.Length >= 4
            && selfieBytes[0] == 0x89 && selfieBytes[1] == 0x50 && selfieBytes[2] == 0x4E && selfieBytes[3] == 0x47
            ? "image/png"
            : "image/jpeg"; // JPEG / RIFF / GIF all acceptable; opencv decodes from bytes
        var imageContent = new ByteArrayContent(selfieBytes);
        imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);
        content.Add(imageContent, "selfie", "selfie.jpg");

        var response = await Client.PostAsync("/generate-embedding", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            // Python returns 422 when no face is detected — surface that as a clear user message
            if (response.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                try
                {
                    using var errDoc = JsonDocument.Parse(errorBody);
                    if (errDoc.RootElement.TryGetProperty("detail", out var detail))
                        throw new InvalidOperationException(detail.GetString() ?? "No face detected in the selfie.");
                }
                catch (JsonException) { /* fall through to generic message */ }
                throw new InvalidOperationException("No face detected in the selfie. Please try again with a clearer photo facing the camera.");
            }
            response.EnsureSuccessStatusCode();
        }

        var result = await response.Content
            .ReadFromJsonAsync<EmbeddingApiResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Empty response from FaceRecognition service.");

        return new EmbeddingResult(result.embedding);
    }

    /// <inheritdoc />
    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await Client.GetAsync("/health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "FaceRecognition service health check failed.");
            return false;
        }
    }
}
