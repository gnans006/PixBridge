using EventPhoto.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace EventPhoto.Infrastructure.Services.FaceRecognition;

// ── Internal DTOs for the Python FastAPI service ──────────────────────────────

file sealed record IndexPhotoApiRequest(string image_path);
file sealed record IndexPhotoApiResponse(int face_count, List<FaceApiResult> faces);
file sealed record FaceApiResult(float[] embedding, string bounding_box, float confidence);
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
            result.faces.Select(f => new FaceDetectionResult(f.embedding, f.bounding_box, f.confidence)).ToList());
    }

    /// <inheritdoc />
    public async Task<EmbeddingResult> GenerateEmbeddingAsync(
        byte[] selfieBytes,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Calling FaceRecognition /generate-embedding for selfie ({Bytes} bytes)", selfieBytes.Length);

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(selfieBytes), "selfie", "selfie.jpg");

        var response = await Client.PostAsync("/generate-embedding", content, cancellationToken);
        response.EnsureSuccessStatusCode();

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
