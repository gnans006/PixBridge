using EventPhoto.Application.Photos.Commands;
using EventPhoto.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace EventPhoto.Worker.Services.FileWatcher;

/// <summary>
/// Background service that monitors active event watch folders and registers newly detected photo files.
/// </summary>
public sealed class FileWatcherService : BackgroundService
{
    private static readonly string[] WatchedExtensions = [".jpg", ".jpeg", ".png", ".cr2", ".nef", ".arw", ".dng", ".tiff"];

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FileWatcherService> _logger;
    private readonly List<FileSystemWatcher> _watchers = [];
    private readonly ConcurrentDictionary<string, byte> _processingFiles = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="FileWatcherService"/> class.
    /// </summary>
    /// <param name="scopeFactory">The scope factory.</param>
    /// <param name="logger">The logger instance.</param>
    public FileWatcherService(IServiceScopeFactory scopeFactory, ILogger<FileWatcherService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("File watcher service starting.");

        await RefreshWatchersAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RefreshWatchersAsync(stoppingToken);
        }
    }

    private async Task RefreshWatchersAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
        var activeEvents = await eventRepository.GetAllActiveAsync(cancellationToken);

        var activePaths = activeEvents.Select(item => Path.GetFullPath(item.WatchFolder)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var staleWatchers = _watchers.Where(watcher => !activePaths.Contains(watcher.Path)).ToList();
        foreach (var watcher in staleWatchers)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
            _watchers.Remove(watcher);
            _logger.LogInformation("Stopped watching folder {Path}.", watcher.Path);
        }

        var existingPaths = _watchers.Select(watcher => watcher.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var eventEntity in activeEvents.Where(item => !existingPaths.Contains(Path.GetFullPath(item.WatchFolder))))
        {
            Directory.CreateDirectory(eventEntity.WatchFolder);

            var watcher = new FileSystemWatcher(eventEntity.WatchFolder)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.LastWrite,
                IncludeSubdirectories = false,
                EnableRaisingEvents = true
            };

            var eventId = eventEntity.Id;
            watcher.Created += (_, args) => QueueFile(args.FullPath, eventId);
            watcher.Renamed += (_, args) => QueueFile(args.FullPath, eventId);

            _watchers.Add(watcher);
            _logger.LogInformation("Watching folder {Path} for event {EventId}.", eventEntity.WatchFolder, eventId);

            // Scan files that already exist in the folder so they are not missed
            // on first start or after a restart. IngestPhotoCommand deduplicates by path.
            var preExisting = Directory.GetFiles(eventEntity.WatchFolder)
                .Where(f => WatchedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .ToList();

            if (preExisting.Count > 0)
            {
                _logger.LogInformation("Scanning {Count} pre-existing file(s) in {Path}.", preExisting.Count, eventEntity.WatchFolder);
                foreach (var file in preExisting)
                {
                    QueueFile(file, eventId);
                }
            }
        }
    }

    private void QueueFile(string filePath, Guid eventId)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        if (!WatchedExtensions.Contains(extension))
        {
            return;
        }

        if (!_processingFiles.TryAdd(filePath, 0))
        {
            return;
        }

        _logger.LogInformation("Queued new file {FilePath} for event {EventId}.", filePath, eventId);
        _ = Task.Run(async () =>
        {
            try
            {
                await ProcessNewFileAsync(filePath, eventId);
            }
            finally
            {
                _processingFiles.TryRemove(filePath, out _);
            }
        });
    }

    private async Task ProcessNewFileAsync(string filePath, Guid eventId)
    {
        await Task.Delay(500);

        var fileReady = false;
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
                fileReady = true;
                break;
            }
            catch (IOException)
            {
                await Task.Delay(TimeSpan.FromSeconds(attempt + 1));
            }
        }

        if (!fileReady)
        {
            _logger.LogWarning("File {FilePath} could not be read after multiple attempts.", filePath);
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var fileInfo = new FileInfo(filePath);
            var mimeType = GetMimeType(fileInfo.Extension);

            var result = await mediator.Send(
                new CreatePhotoCommand(
                    eventId,
                    fileInfo.Name,
                    fileInfo.FullName,
                    fileInfo.Length,
                    mimeType,
                    null),
                CancellationToken.None);

            if (result.IsFailure)
            {
                _logger.LogWarning("Failed to register photo {FileName}: {Error}", fileInfo.Name, result.Error);
                return;
            }

            _logger.LogInformation("Registered photo {FileName} for event {EventId}.", fileInfo.Name, eventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register photo {FilePath} for event {EventId}.", filePath, eventId);
        }
    }

    private static string GetMimeType(string extension) => extension.ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".tiff" => "image/tiff",
        _ => "application/octet-stream"
    };

    /// <inheritdoc />
    public override void Dispose()
    {
        foreach (var watcher in _watchers)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }

        _watchers.Clear();
        base.Dispose();
    }
}
