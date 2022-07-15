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
using System.Text.Json.Serialization;
using ComposeUI.Messaging.Core.Messages;

namespace ComposeUI.Messaging.Core.Serialization;

/// <summary>
///     Serializes/deserializes messages to/from JSON
/// </summary>
public static class JsonMessageSerializer
{
    public static readonly JsonSerializerOptions Options =
        new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = {new MessageConverter(), new Utf8ByteArrayConverter(), new JsonStringEnumConverter()},
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
    ///     Deserializes a message from UTF8-encoded JSON
    /// </summary>
    /// <param name="buffer"></param>
    /// <returns></returns>
    public static Message DeserializeMessage(ref ReadOnlySequence<byte> buffer)
    {
        var reader = new Utf8JsonReader(buffer);
        var message = JsonSerializer.Deserialize<Message>(ref reader, Options);
        buffer = buffer.Slice(reader.Position);
        return message;
    }

    private class MessageConverter : JsonConverter<Message>
    {
        public override Message? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var innerReader = reader;
            if (innerReader.TokenType != JsonTokenType.StartObject) goto InvalidJson;
            while (innerReader.Read())
                switch (innerReader.TokenType)
                {
                    case JsonTokenType.PropertyName:
                        if (innerReader.ValueTextEquals(TypePropertyNameBytes))
                        {
                            if (!innerReader.Read()) goto InvalidJson;
                            MessageType messageType;
                            switch (innerReader.TokenType)
                            {
                                case JsonTokenType.String:
                                    messageType = Enum.Parse<MessageType>(innerReader.GetString()!, ignoreCase: true);
                                    break;
                                case JsonTokenType.Number:
                                    messageType = (MessageType) innerReader.GetInt32();
                                    break;
                                default:
                                    goto InvalidJson;
                            }

                            var type = Message.ResolveMessageType(messageType);
                            return (Message) JsonSerializer.Deserialize(ref reader, type, options)!;
                        }

                        innerReader.Skip();
                        break;
                }

            InvalidJson:
            throw new JsonException();
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

    // TODO: Allow raw byte payloads
    private class Base64ByteArrayConverter : JsonConverter<byte[]>
    {
        public override byte[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType == JsonTokenType.String
                ? reader.GetBytesFromBase64()
                : null;
        }

        public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options)
        {
            writer.WriteBase64StringValue(value);
        }
    }
}