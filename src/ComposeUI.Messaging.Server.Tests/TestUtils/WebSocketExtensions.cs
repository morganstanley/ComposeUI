using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json.Linq;

namespace ComposeUI.Messaging.Server.Tests.TestUtils;

internal static class WebSocketExtensions
{
    public static Task SendJsonAsync(this WebSocket webSocket, JObject message)
    {
        return webSocket.SendUtf8BytesAsync(message.ToString());
    }

    public static async Task SendUtf8BytesAsync(this WebSocket webSocket, string message)
    {
        var messageBytes = Encoding.UTF8.GetBytes(message);
        await webSocket.SendAsync(
            messageBytes,
            WebSocketMessageType.Binary,
            WebSocketMessageFlags.EndOfMessage,
            CancellationToken.None);
    }

    public static async Task<JObject> ReceiveJsonAsync(this WebSocket webSocket)
    {
        var pipe = new Pipe();
        while (webSocket.State == WebSocketState.Open)
        {
            var buffer = pipe.Writer.GetMemory(1024);
            var receiveResult = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
            pipe.Writer.Advance(receiveResult.Count);
            if (receiveResult.EndOfMessage)
            {
                await pipe.Writer.FlushAsync();
                var readResult = await pipe.Reader.ReadAsync();
                var sequence = readResult.Buffer;
                //var message = JsonDocument.Parse(readResult.Buffer);
                var message = JObject.Parse(Encoding.UTF8.GetString(sequence));
                pipe.Reader.AdvanceTo(sequence.Start, sequence.End);
                return message;
            }
        }

        throw new InvalidOperationException("The WebSocket was closed before receiving a message");
    }
}