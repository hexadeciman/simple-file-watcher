using System.Text;
using FileStreamer.Auth;
using FileStreamer.Json;

namespace FileStreamer.Endpoints;

internal static class JsonFileEndpoints
{
    public static IEndpointRouteBuilder MapJsonFileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet("/data", ReadJsonFileAsync)
            .RequireAuthorization(AuthPolicies.AllowedUser);

        return endpoints;
    }

    private static async Task<IResult> ReadJsonFileAsync(
        JsonFileReader reader,
        CancellationToken cancellationToken)
    {
        var result = await reader.ReadAsync(cancellationToken);

        return result switch
        {
            { Content: not null } => Results.Content(result.Content, "application/json", Encoding.UTF8),
            { Error: not null } => Results.Problem(
                result.Error,
                statusCode: StatusCodes.Status500InternalServerError),
            _ => Results.Problem(
                "Unknown JSON read error.",
                statusCode: StatusCodes.Status500InternalServerError)
        };
    }
}
