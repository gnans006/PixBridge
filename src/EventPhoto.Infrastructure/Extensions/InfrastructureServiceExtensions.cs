using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Domain.Interfaces;
using EventPhoto.Infrastructure.Persistence;
using EventPhoto.Infrastructure.Persistence.Repositories;
using EventPhoto.Infrastructure.Services.Auth;
using EventPhoto.Infrastructure.Services.FaceRecognition;
using EventPhoto.Infrastructure.Services.FileSystem;
using EventPhoto.Infrastructure.Services.QrCode;
using EventPhoto.Infrastructure.Services.Thumbnails;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pgvector.EntityFrameworkCore;
using Polly;
using Polly.Extensions.Http;

namespace EventPhoto.Infrastructure.Extensions;

/// <summary>DI registration extensions for the Infrastructure layer.</summary>
public static class InfrastructureServiceExtensions
{
    /// <summary>Registers the PostgreSQL DbContext, repositories, and all infrastructure services.</summary>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(InfrastructureServiceExtensions).Assembly.GetName().Name);
                    npgsql.UseVector();  // enable pgvector support
                }));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Existing repositories
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IPhotoRepository, PhotoRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IDownloadLogRepository, DownloadLogRepository>();
        services.AddScoped<ISystemSettingRepository, SystemSettingRepository>();

        // Face Recognition repositories
        services.AddScoped<IFaceEmbeddingRepository, FaceEmbeddingRepository>();
        services.AddScoped<IGuestFaceSessionRepository, GuestFaceSessionRepository>();
        services.AddScoped<IPhotoMatchRepository, PhotoMatchRepository>();

        // Existing services
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IThumbnailService, ThumbnailService>();
        services.AddScoped<IQrCodeService, QrCodeService>();
        services.AddScoped<IFileStorageService, FileStorageService>();
        services.AddTransient<IFileService, FileService>();

        // Face Recognition HTTP client with retry + circuit-breaker
        var faceRecognitionBaseUrl = configuration["FaceRecognition:BaseUrl"] ?? "http://localhost:8080";

        services.AddHttpClient("FaceRecognition", client =>
        {
            client.BaseAddress = new Uri(faceRecognitionBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());

        services.AddScoped<IFaceRecognitionService, FaceRecognitionService>();

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        => HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));  // 2s, 4s, 8s

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
        => HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30));
}
