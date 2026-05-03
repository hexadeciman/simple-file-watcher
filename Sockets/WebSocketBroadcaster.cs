using System.Collections.Concurrent;
using System.Net.WebSockets;
using FileStreamer.Json;

namespace FileStreamer.Sockets;

internal sealed class WebSocketBroadcaster
{
    private readonly ConcurrentDictionary<WebSocket, byte> sockets = new();

    public void Add(WebSocket socket) => sockets.TryAdd(socket, 0);

    public async Task BroadcastAsync(
        JsonReadResult result,
        CancellationToken cancellationToken = default)
    {
        foreach (var socket in sockets.Keys)
        {
            if (socket.State is not WebSocketState.Open)
            {
                sockets.TryRemove(socket, out _);
                continue;
            }

            try
            {
                await SocketMessages.SendReadResultAsync(socket, result, cancellationToken);
            }
            catch (WebSocketException)
            {
                sockets.TryRemove(socket, out _);
            }
        }
    }
}
