using System.Net.WebSockets;

namespace FileStreamer.Sockets;

internal static class WebSocketConnection
{
    public static async Task WaitForCloseAsync(
        WebSocket socket,
        CancellationToken cancellationToken = default)
    {
        var buffer = new byte[1024];

        while (socket.State is WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            var result = await socket.ReceiveAsync(buffer, cancellationToken);
            if (result.MessageType is WebSocketMessageType.Close)
            {
                await socket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Closed by client.",
                    cancellationToken);
            }
        }
    }
}
