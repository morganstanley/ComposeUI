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
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MorganStanley.ComposeUI.Messaging.Protocol.Messages;

namespace MorganStanley.ComposeUI.Messaging.Protocol.Json;

/// <summary>
///     Serializes/deserializes messages to/from JSON
/// </summary>
public static class JsonMessageSerializer
{
    public static readonly JsonSerializerOptions Options =
        new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new MessageConverter(), new MessageBufferConverter(), new JsonStringEnumConverter() },
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

    /// <summary>
    ///     Serializes a message to UTF8-encoded JSON.
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public static byte[] SerializeMessage(Message message)
    {
        return JsonSerializer.SerializeToUtf8Bytes(message, Options);
    }

    /// <summary>
    ///     Serializes a message to UTF8-encoded JSON.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="bufferWriter"></param>
    /// <returns></returns>
    public static void SerializeMessage(Message message, IBufferWriter<byte> bufferWriter)
    {
        using var jsonWriter = new Utf8JsonWriter(bufferWriter);
        JsonSerializer.Serialize(jsonWriter, message, Options);
    }

    /// <summary>
    ///     Deserializes a message from UTF8-encoded JSON
    /// </summary>
    /// <param name="buffer"></param>
    /// <returns></returns>
    public static Message DeserializeMessage(ref ReadOnlySequence<byte> buffer)
    {
        var reader = new Utf8JsonReader(buffer);
        var message = JsonSerializer.Deserialize<Message>(ref reader, Options);
        buffer = buffer.Slice(reader.Position);

        return message ?? throw new JsonException($"Expected a {nameof(Message)} object, but found null");
    }

    private sealed class MessageConverter : JsonConverter<Message>
    {
        public override Message? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (!TryReadMessage(ref reader, options, out var message))
            {
                throw new JsonException();
            }

            return message;
        }

        private static bool TryReadMessage(
            ref Utf8JsonReader reader,
            JsonSerializerOptions options,
            [MaybeNullWhen(false)] out Message message)
        {
            var innerReader = reader;
            message = null!;

            if (innerReader.TokenType != JsonTokenType.StartObject)
                return false;

            while (innerReader.Read())
            {
                switch (innerReader.TokenType)
                {
                    case JsonTokenType.PropertyName:
                        if (innerReader.ValueTextEquals(TypePropertyNameBytes))
                        {
                            if (!innerReader.Read())
                                return false;

                            var messageType = JsonSerializer.Deserialize<MessageType>(ref innerReader, options);
                            var type = Message.ResolveMessageType(messageType);
                            message = (Message)JsonSerializer.Deserialize(ref reader, type, options)!;

                            return true;
                        }

                        innerReader.Skip();

                        break;
                }
            }

            return false;
        }

        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(Message);
        }

        public override void Write(Utf8JsonWriter writer, Message value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }

        private static readonly byte[] TypePropertyNameBytes = Encoding.UTF8.GetBytes("type");
    }
}