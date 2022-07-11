// Morgan Stanley makes this available to you under the Apache License,
// Version 2.0 (the "License"). You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0.
// 
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership. Unless required by applicable law or agreed
// to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
// or implied. See the License for the specific language governing permissions
// and limitations under the License.

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
            WebSocketMessageType.Text,
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