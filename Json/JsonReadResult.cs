namespace FileStreamer.Json;

internal sealed record JsonReadResult
{
    private JsonReadResult(string? content, string? error)
    {
        Content = content;
        Error = error;
    }

    public string? Content { get; }

    public string? Error { get; }

    public bool IsValid => Content is not null;

    public static JsonReadResult Success(string content) => new(content, null);

    public static JsonReadResult Failure(string error) => new(null, error);
}
