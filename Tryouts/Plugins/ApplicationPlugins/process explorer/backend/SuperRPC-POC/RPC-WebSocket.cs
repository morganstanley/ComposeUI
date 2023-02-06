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
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Text;
using Nerdbank.Streams;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProcessExplorer.Abstraction;
using ProcessExplorer.Abstraction.Infrastructure;
using Super.RPC;

namespace SuperRPC_POC;

public record SuperRPCWebSocket(WebSocket webSocket, object? context)
{
    public RPCReceiveChannel ReceiveChannel;
    public IRPCSendAsyncChannel SendChannel;
    
    // This is for the websocket client case. You need to call StartReceivingAsync()
    // after connecting the SuperRPC instance to SuperRPCWebSocket.ReceiveChannel
    public static SuperRPCWebSocket CreateHandler(WebSocket webSocket, object? context = null)
    {
        var rpcWebSocket = new SuperRPCWebSocket(webSocket, context);
        var sendAndReceiveChannel = new RPCSendAsyncAndReceiveChannel(rpcWebSocket.ScheduleMessage);

        rpcWebSocket.SendChannel = sendAndReceiveChannel;
        rpcWebSocket.ReceiveChannel = sendAndReceiveChannel;

        return rpcWebSocket;
    }

    // This is useful when handling a server-side WebSocket connection.
    // The replyChannel will be passed to the message event automatically.
    public static Task HandleWebsocketConnectionAsync(WebSocket webSocket,
        RPCReceiveChannel receiveChannel,
        object? context = null,
        IProcessInfoAggregator? aggregator = null, IUIHandler? uiHandler = null)
    {
        var rpcWebSocket = new SuperRPCWebSocket(webSocket, context);
        rpcWebSocket.ReceiveChannel = receiveChannel;
        rpcWebSocket.SendChannel = new RPCSendAsyncChannel(rpcWebSocket.ScheduleMessage);
        return rpcWebSocket.StartReceivingAsync(aggregator, uiHandler);
    }

    public static void RegisterCustomDeserializer(SuperRPC rpc)
    {
        rpc.RegisterDeserializer(typeof(object), ConvertTo);
    }

    private static object? ConvertTo(object? obj, Type targetType)
    {
        if (obj is JArray array)
        {
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
            {
                var elementType = targetType.GetGenericArguments()[0];
                var list = (IList)Activator.CreateInstance(targetType);
                foreach (var item in array)
                {
                    list.Add(ConvertTo(item, elementType));
                }
                return list;
            }
            if (targetType.IsArray)
            {
                var elementType = targetType.GetElementType();
                var arr = (IList)Activator.CreateInstance(targetType, array.Count);
                for (var i = 0; i < array.Count; i++)
                {
                    arr[i] = ConvertTo(array[i], elementType);
                }
                return arr;
            }
            return obj;
        }
        return (obj is JToken jToken) ? jToken.ToObject(targetType) : obj;
    }

    private const int ReceiveBufferSize = 4 * 1024;
    private JsonSerializer jsonSerializer = new JsonSerializer();
    private ArrayBufferWriter<byte> responseBuffer = new ArrayBufferWriter<byte>();
    private BlockingCollection<RPC_Message> messageQueue = new BlockingCollection<RPC_Message>(new ConcurrentQueue<RPC_Message>());

    async Task SendMessage(RPC_Message message)
    {
        try
        {
            TextWriter textWriter = new StreamWriter(responseBuffer.AsStream());
            jsonSerializer.Serialize(textWriter, message);
            await textWriter.FlushAsync();
            await webSocket.SendAsync(responseBuffer.WrittenMemory, WebSocketMessageType.Text, true, CancellationToken.None);
            responseBuffer.Clear();
        }
        catch (Exception e)
        {
            Debug.WriteLine("Error during SendMessage " + e.ToString());
        }
    }

    void ScheduleMessage(RPC_Message message)
    {
        messageQueue.Add(message);
    }

    async Task ProcessMessageQueue()
    {
        while (!webSocket.CloseStatus.HasValue)
        {
            var message = messageQueue.Take();
            await SendMessage(message);
        }
    }

    public async Task StartReceivingAsync(IProcessInfoAggregator? aggregator = null, IUIHandler? uiHandler = null)
    {
        Debug.WriteLine("WebSocket connected");

        var pipe = new Pipe(new PipeOptions(pauseWriterThreshold: 0));
        var messageLength = 0;

        Task.Run(ProcessMessageQueue);

        while (!webSocket.CloseStatus.HasValue)
        {
            var mem = pipe.Writer.GetMemory(ReceiveBufferSize);

            var receiveResult = await webSocket.ReceiveAsync(mem, CancellationToken.None);

            if (receiveResult.MessageType == WebSocketMessageType.Close) break;

            messageLength += receiveResult.Count;
            pipe.Writer.Advance(receiveResult.Count);

            if (receiveResult.EndOfMessage)
            {
                await pipe.Writer.FlushAsync();
                while (pipe.Reader.TryRead(out var readResult))
                {
                    if (readResult.Buffer.Length >= messageLength)
                    {
                        var messageBuffer = readResult.Buffer.Slice(readResult.Buffer.Start, messageLength);
                        var message = ParseMessage(messageBuffer);
                        if (message != null)
                        {
                            ReceiveChannel.Received(message, SendChannel, context ?? SendChannel);
                        }
                        pipe.Reader.AdvanceTo(messageBuffer.End);
                        messageLength = 0;
                        break;
                    }

                    if (readResult.IsCompleted) break;
                }
            }
        }
        Debug.WriteLine($"WebSocket closed with status {webSocket.CloseStatus} {webSocket.CloseStatusDescription}");
        if(aggregator != null && uiHandler != null)
            aggregator.RemoveUiConnection(uiHandler);
    }


    private RPC_Message? ParseMessage(ReadOnlySequence<byte> messageBuffer)
    {
        var jsonReader = new JsonTextReader(new SequenceTextReader(messageBuffer, Encoding.UTF8));
        var obj = jsonSerializer.Deserialize<JObject>(jsonReader);

        if (obj == null)
        {
            throw new InvalidOperationException("Received data is not JSON");
        }

        var action = obj["action"]?.Value<string>();
        if (action == null)
        {
            throw new ArgumentNullException("The action field is null.");
        }

        Type? messageType;
        if (RPC_Message.MessageTypesByAction.TryGetValue(action, out messageType) && messageType != null)
        {
            return (RPC_Message?)obj.ToObject(messageType);
        }

        throw new ArgumentException($"Invalid action value {action}");
    }
}
