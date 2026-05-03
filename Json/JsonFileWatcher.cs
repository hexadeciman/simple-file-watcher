using FileStreamer.Configuration;
using FileStreamer.Sockets;
using Microsoft.Extensions.Options;

namespace FileStreamer.Json;

internal sealed class JsonFileWatcher(
    IOptions<JsonFileOptions> options,
    JsonFileReader reader,
    WebSocketBroadcaster broadcaster,
    ILogger<JsonFileWatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (JsonWatchTarget.FromPath(options.Value.Path) is not { } watchTarget)
        {
            logger.LogError("JsonFile:Path must point to a file. Current value: {Path}", options.Value.Path);
            return;
        }

        if (!Directory.Exists(watchTarget.Directory))
        {
            logger.LogError("The configured JSON directory does not exist: {Directory}", watchTarget.Directory);
            return;
        }

        using var watcher = CreateWatcher(watchTarget);
        using var changeSignal = new SemaphoreSlim(0);

        FileSystemEventHandler onChange = (_, _) => changeSignal.Release();
        RenamedEventHandler onRename = (_, _) => changeSignal.Release();

        watcher.Changed += onChange;
        watcher.Created += onChange;
        watcher.Renamed += onRename;
        watcher.EnableRaisingEvents = true;

        while (!stoppingToken.IsCancellationRequested)
        {
            await changeSignal.WaitAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromMilliseconds(150), stoppingToken);
            await DrainPendingSignalsAsync(changeSignal, stoppingToken);

            await broadcaster.BroadcastAsync(await reader.ReadAsync(stoppingToken), stoppingToken);
        }
    }

    private static FileSystemWatcher CreateWatcher(JsonWatchTarget target) => new(target.Directory, target.FileName)
    {
        NotifyFilter = NotifyFilters.FileName
            | NotifyFilters.LastWrite
            | NotifyFilters.Size
            | NotifyFilters.CreationTime
    };

    private static async Task DrainPendingSignalsAsync(
        SemaphoreSlim changeSignal,
        CancellationToken cancellationToken)
    {
        while (changeSignal.CurrentCount > 0)
        {
            await changeSignal.WaitAsync(cancellationToken);
        }
    }
}

internal readonly record struct JsonWatchTarget(string Directory, string FileName)
{
    public static JsonWatchTarget? FromPath(string path)
    {
        var directory = Path.GetDirectoryName(path);
        var fileName = Path.GetFileName(path);

        return string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(fileName)
            ? null
            : new JsonWatchTarget(directory, fileName);
    }
}
