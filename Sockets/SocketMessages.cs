using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using FileStreamer.Json;

namespace FileStreamer.Sockets;

internal static class SocketMessages
{
    public static async Task SendReadResultAsync(
        WebSocket socket,
        JsonReadResult result,
        CancellationToken cancellationToken = default)
    {
        var message = result switch
        {
            { Content: not null } => result.Content,
            { Error: not null } => JsonSerializer.Serialize(new { error = result.Error }),
            _ => JsonSerializer.Serialize(new { error = "Unknown JSON read error." })
        };

        await SendTextAsync(socket, message, cancellationToken);
    }

    private static async Task SendTextAsync(
        WebSocket socket,
        string message,
        CancellationToken cancellationToken)
    {
        if (socket.State is not WebSocketState.Open)
        {
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(message);
        await socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);
    }
}
