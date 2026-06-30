using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Application.Extensions;
using EventPhoto.Infrastructure.Extensions;
using EventPhoto.Worker.Extensions;
using EventPhoto.Worker.Services;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "PixBridge Worker";
});
builder.Services.AddSerilog();
builder.Services.AddHttpClient();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddWorkerServices();
builder.Services.AddSingleton<IPhotoNotificationService, NoOpPhotoNotificationService>();

var host = builder.Build();
await host.RunAsync();
