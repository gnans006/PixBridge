using EventPhoto.Api.Extensions;
using EventPhoto.Api.Hubs;
using EventPhoto.Api.Middleware;
using EventPhoto.Api.Services;
using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Application.Extensions;
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
builder.Services.AddTransient<IFileService, FileService>();
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
