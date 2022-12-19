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

using System.Text.Json;
using System.Text.Json.Serialization;

namespace MorganStanley.ComposeUI.Messaging.Protocol.Json;

/// <summary>
/// A JSON converter that reads and writes <see cref="Utf8Buffer"/> objects.
/// </summary>
internal class Utf8BufferConverter : JsonConverter<Utf8Buffer>
{
    public override Utf8Buffer? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;

            case JsonTokenType.String:
                {
                    var length = reader.HasValueSequence ? checked((int)reader.ValueSequence.Length) : reader.ValueSpan.Length;
                    var buffer = Utf8Buffer.GetBuffer(length);
                    length = reader.CopyString(buffer);

                    return new Utf8Buffer(buffer, length);
                }
        }

        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, Utf8Buffer value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.GetSpan());
    }
}
