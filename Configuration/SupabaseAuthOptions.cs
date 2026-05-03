namespace FileStreamer.Configuration;

internal sealed record SupabaseAuthOptions
{
    public const string SectionName = "Supabase";

    public string Url { get; init; } = "";

    public string AllowedUserId { get; init; } = "";

    public string Audience { get; init; } = "authenticated";
}
