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

using System.Text;
using System.Text.Json;

namespace MorganStanley.ComposeUI.MessagingAdapter.Abstractions;

/// <summary>
/// Provides factory methods for creating JSON representations of objects.
/// </summary>

public static class JsonFactory
{
    /// <summary>
    /// Serializes the specified value to its JSON string representation using <see cref="System.Text.Json"/>.
    /// </summary>
    /// <typeparam name="T">The type of the value to serialize.</typeparam>
    /// <param name="value">The value to serialize to JSON.</param>
    /// <param name="options">Optional <see cref="JsonSerializerOptions"/> to control the serialization behavior.</param>
    /// <returns>A JSON string representation of the specified value.</returns>
    public static string CreateJson<T>(
    T value,
    JsonSerializerOptions? options = null)
    {
        using var memoryStream = new System.IO.MemoryStream();
        using var jsonWriter = new Utf8JsonWriter(memoryStream);
        JsonSerializer.Serialize(jsonWriter, value, options);
        jsonWriter.Flush();
        return Encoding.UTF8.GetString(memoryStream.ToArray());
    }
}