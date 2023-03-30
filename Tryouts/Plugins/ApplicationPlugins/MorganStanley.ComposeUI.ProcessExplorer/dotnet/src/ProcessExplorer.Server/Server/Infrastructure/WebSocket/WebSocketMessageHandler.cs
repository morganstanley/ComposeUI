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

using System.Buffers;
using System.Text;
using System.Text.Json;

namespace ProcessExplorer.Server.Server.Infrastructure.WebSocket;

public static class WebSocketMessageHandler
{
    public static void Serialize(this WebSocketMessage message, IBufferWriter<byte> writer)
    {
        var serializedMessage = JsonSerializer.Serialize(message);
        WriteMessage(writer, serializedMessage);
    }

    private static void WriteMessage(IBufferWriter<byte> writer, string jsonValue)
    {
        var length = Encoding.UTF8.GetByteCount(jsonValue);
        var span = writer.GetSpan(length);
        Encoding.UTF8.GetBytes(jsonValue, span);
        writer.Advance(length);
    }
}
