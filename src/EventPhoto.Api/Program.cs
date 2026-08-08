using EventPhoto.Api.Extensions;
using EventPhoto.Api.Hubs;
using EventPhoto.Api.Middleware;
using EventPhoto.Api.Services;
using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Application.Extensions;
using EventPhoto.Domain.Interfaces;
using EventPhoto.Infrastructure.Extensions;
using EventPhoto.Infrastructure.Persistence;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using System.Threading.RateLimiting;
using EventPhoto.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseWindowsService(options =>
{
    options.ServiceName = "PixBridge API";
});

builder.Host.UseSerilog();

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddSignalR();
builder.Services.AddScoped<IPhotoNotificationService, PhotoNotificationService>();
builder.Services.AddScoped<IFaceNotificationService, FaceNotificationService>();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization(options =>
{
    // ── Studio Role Policies ──────────────────────────────────────────────
    // These accept both legacy (Admin/Viewer) and new (StudioOwner/StudioManager/Operator)
    // claim values so tokens issued before the migration continue to work.
    options.AddPolicy("OwnerOnly", policy =>
        policy.RequireRole("StudioOwner", "Admin"));

    options.AddPolicy("ManagerOrOwner", policy =>
        policy.RequireRole("StudioOwner", "StudioManager", "Admin"));

    options.AddPolicy("OperatorOrAbove", policy =>
        policy.RequireRole("StudioOwner", "StudioManager", "Operator", "Admin", "Viewer"));
});
builder.Services.AddControllers();
builder.Services.AddSwaggerWithJwt();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
});
builder.Services.AddResponseCaching();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("downloads", limiterOptions =>
    {
        limiterOptions.PermitLimit = 30;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 5;
    });
    // Brute-force protection: max 5 login attempts per IP per minute
    options.AddFixedWindowLimiter("login", limiterOptions =>
    {
        limiterOptions.PermitLimit = 5;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    var passwordHasher = services.GetRequiredService<IPasswordHasher>();
    var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseSeeder");
    await AppDbContextSeeder.SeedAsync(context, passwordHasher, logger);

    // Auto-update ApplicationSettings.PublicBaseUrl and legacy app.serverUrl with current LAN IP.
    // Only updates when the stored URL uses a raw IP address (not a hostname like pixbridge.local).
    try
    {
        var startupLog = services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
        var networkSvc = services.GetRequiredService<EventPhoto.Application.Common.Interfaces.INetworkInformationService>();
        var appSettingsRepo = services.GetRequiredService<EventPhoto.Domain.Interfaces.IApplicationSettingsRepository>();

        var appSettings = await appSettingsRepo.GetOrCreateDefaultAsync();
        var networkInfo = networkSvc.GetCurrentNetworkInformation(appSettings.ServerPort);

        if (networkInfo.PrimaryIpAddress is not "127.0.0.1"
            && networkSvc.IsIpBasedUrl(appSettings.PublicBaseUrl))
        {
            var newBaseUrl = networkSvc.ReplaceIpInUrl(appSettings.PublicBaseUrl, networkInfo.PrimaryIpAddress);
            if (newBaseUrl != appSettings.PublicBaseUrl)
            {
                var oldBaseUrl = appSettings.PublicBaseUrl;
                startupLog.LogInformation(
                    "IP changed — updating PublicBaseUrl from {Old} to {New}",
                    oldBaseUrl, newBaseUrl);

                appSettings.UpdatePublicBaseUrl(newBaseUrl);
                await appSettingsRepo.UpdateAsync(appSettings);

                // Keep legacy app.serverUrl in sync for backward compatibility
                var settingRepo = services.GetRequiredService<EventPhoto.Domain.Interfaces.ISystemSettingRepository>();
                var legacySetting = await settingRepo.GetByKeyAsync("app.serverUrl");
                if (legacySetting is not null)
                {
                    legacySetting.UpdateValue(newBaseUrl);
                    await settingRepo.UpdateAsync(legacySetting);
                }

                // Regenerate QR codes for all active events
                var eventRepo = services.GetRequiredService<IEventRepository>();
                var qrService = services.GetRequiredService<IQrCodeService>();
                var events = await eventRepo.GetAllAsync();

                foreach (var ev in events.Where(e => e.QrCodePath != null))
                {
                    var newGalleryUrl = ev.QrCodeUrl?.Replace(oldBaseUrl, newBaseUrl)
                                     ?? $"{newBaseUrl}/gallery/{ev.Id}";
                    await qrService.GenerateAsync(newGalleryUrl, ev.QrCodePath!, ev.Name);
                    ev.SetQrCode(ev.QrCodePath!, newGalleryUrl);
                    await eventRepo.UpdateAsync(ev);
                    startupLog.LogInformation(
                        "QR regenerated for event {Name} → {Url}", ev.Name, newGalleryUrl);
                }

                await context.SaveChangesAsync();
            }
        }
    }
    catch (Exception ex)
    {
        var startupLog = services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
        startupLog.LogWarning(ex, "Could not auto-update PublicBaseUrl / regenerate QR codes");
    }
}

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "PixBridge API v1");
    options.DisplayRequestDuration();
});

app.UseCors();
app.UseResponseCaching();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();
app.MapHub<PhotoHub>("/hubs/photos");

var webRootPath = app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
if (File.Exists(Path.Combine(webRootPath, "index.html")))
{
    app.MapFallbackToFile("index.html");
}

app.Run();
