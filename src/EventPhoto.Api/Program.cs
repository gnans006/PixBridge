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
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
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
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();
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
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    var passwordHasher = services.GetRequiredService<IPasswordHasher>();
    var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseSeeder");
    await AppDbContextSeeder.SeedAsync(context, passwordHasher, logger);

    // Auto-update app.serverUrl with current LAN IP; regenerate all QR images if IP changed
    try
    {
        var lanIp = NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up
                     && n.NetworkInterfaceType != NetworkInterfaceType.Loopback
                     && n.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
            .SelectMany(n => n.GetIPProperties().UnicastAddresses)
            .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork
                     && !IPAddress.IsLoopback(a.Address)
                     && !a.Address.ToString().StartsWith("169.254"))
            .Select(a => a.Address.ToString())
            .FirstOrDefault();

        if (lanIp != null)
        {
            var startupLog = services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
            var settingRepo = services.GetRequiredService<ISystemSettingRepository>();
            var setting = await settingRepo.GetByKeyAsync("app.serverUrl");
            if (setting != null)
            {
                var uri = new Uri(setting.Value);
                var newBaseUrl = $"{uri.Scheme}://{lanIp}:{uri.Port}";
                var oldBaseUrl = $"{uri.Scheme}://{uri.Host}:{uri.Port}";

                if (newBaseUrl != oldBaseUrl)
                {
                    startupLog.LogInformation("IP changed from {Old} to {New} — updating serverUrl and regenerating QR codes", oldBaseUrl, newBaseUrl);

                    // 1. Update the setting
                    setting.UpdateValue(newBaseUrl);
                    await settingRepo.UpdateAsync(setting);

                    // 2. Regenerate QR images for all events
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
                        startupLog.LogInformation("QR regenerated for event {Name} → {Url}", ev.Name, newGalleryUrl);
                    }
                }
            }
        }
    }
    catch (Exception ex)
    {
        var startupLog = services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
        startupLog.LogWarning(ex, "Could not auto-update app.serverUrl / regenerate QR codes");
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
