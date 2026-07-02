using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Domain.Interfaces;
using EventPhoto.Infrastructure.Persistence;
using EventPhoto.Infrastructure.Persistence.Repositories;
using EventPhoto.Infrastructure.Services.Auth;
using EventPhoto.Infrastructure.Services.FileSystem;
using EventPhoto.Infrastructure.Services.QrCode;
using EventPhoto.Infrastructure.Services.Thumbnails;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
                npgsql => npgsql.MigrationsAssembly(typeof(InfrastructureServiceExtensions).Assembly.GetName().Name)));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IPhotoRepository, PhotoRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IDownloadLogRepository, DownloadLogRepository>();
        services.AddScoped<ISystemSettingRepository, SystemSettingRepository>();

        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IThumbnailService, ThumbnailService>();
        services.AddScoped<IQrCodeService, QrCodeService>();
        services.AddScoped<IFileStorageService, FileStorageService>();
        services.AddTransient<IFileService, FileService>();

        return services;
    }
}
