using System.Text.Json;
using FileStreamer.Configuration;
using Microsoft.Extensions.Options;

namespace FileStreamer.Json;

internal sealed class JsonFileReader(IOptions<JsonFileOptions> options)
{
    public async Task<JsonReadResult> ReadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var content = await File.ReadAllTextAsync(options.Value.Path, cancellationToken);
            using var _ = JsonDocument.Parse(content);

            return JsonReadResult.Success(content);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return JsonReadResult.Failure(exception.Message);
        }
    }
}
