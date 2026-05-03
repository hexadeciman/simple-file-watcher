namespace FileStreamer.Configuration;

internal sealed record JsonFileOptions
{
    public const string SectionName = "JsonFile";

    public string Path { get; init; } = "/data/live.json";
}
