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
using Microsoft.IO;

namespace MorganStanley.ComposeUI.Messaging;

/// <summary>
/// Extensions methods for handling JSON data in <see cref="MessageBuffer"/> objects.
/// </summary>
public static class MessageBufferJsonExtensions
{
    /// <summary>
    /// Deserializes the JSON content of the buffer.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="buffer"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static T? ReadJson<T>(this MessageBuffer buffer, JsonSerializerOptions? options = null)
    {
        var reader = new Utf8JsonReader(buffer.GetSpan());

        return JsonSerializer.Deserialize<T>(ref reader, options);
    }

    /// <summary>
    /// Creates a <see cref="MessageBuffer"/> from the provided value serialized to JSON.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="value"></param>
    /// <param name="options"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static MessageBuffer CreateJson<T>(
        this MessageBuffer.MessageBufferFactory factory,
        T value,
        JsonSerializerOptions? options = null)
    {
        using var bufferWriter = MessageBuffer.GetBufferWriter();
        using var jsonWriter = new Utf8JsonWriter(bufferWriter);
        JsonSerializer.Serialize(jsonWriter, value, options);
        jsonWriter.Flush();
        
        return MessageBuffer.Create(bufferWriter.WrittenMemory);
    }
}