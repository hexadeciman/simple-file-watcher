using FileStreamer.Auth;
using FileStreamer.Json;
using FileStreamer.Sockets;

namespace FileStreamer.Endpoints;

internal static class JsonWebSocketEndpoints
{
    public static IEndpointRouteBuilder MapJsonWebSocketEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints
            .Map(JsonWebSocketRoutes.Path, StreamJsonFileAsync)
            .RequireAuthorization(AuthPolicies.AllowedUser);

        return endpoints;
    }

    private static async Task StreamJsonFileAsync(
        HttpContext context,
        JsonFileReader reader,
        WebSocketBroadcaster broadcaster,
        CancellationToken cancellationToken)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Expected a WebSocket request.", cancellationToken);
            return;
        }

        using var socket = await context.WebSockets.AcceptWebSocketAsync();

        broadcaster.Add(socket);

        await SocketMessages.SendReadResultAsync(socket, await reader.ReadAsync(cancellationToken), cancellationToken);
        await WebSocketConnection.WaitForCloseAsync(socket, cancellationToken);
    }
}
