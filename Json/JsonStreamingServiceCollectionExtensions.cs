using FileStreamer.Configuration;
using FileStreamer.Sockets;

namespace FileStreamer.Json;

internal static class JsonStreamingServiceCollectionExtensions
{
    public static IServiceCollection AddJsonStreaming(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JsonFileOptions>(configuration.GetSection(JsonFileOptions.SectionName));
        services.AddSingleton<JsonFileReader>();
        services.AddSingleton<WebSocketBroadcaster>();
        services.AddHostedService<JsonFileWatcher>();

        return services;
    }
}
