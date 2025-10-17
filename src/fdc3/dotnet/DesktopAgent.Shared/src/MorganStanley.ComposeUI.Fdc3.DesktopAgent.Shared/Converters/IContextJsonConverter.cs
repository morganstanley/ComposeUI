/*
 * Morgan Stanley makes this available to you under the Apache License,
 * Version 2.0 (the "License"). You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0.
 *
 * See the NOTICE file distributed with this work for additional information
 * regarding copyright ownership. Unless required by applicable law or agreed
 * to in writing, software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
 * or implied. See the License for the specific language governing permissions
 * and limitations under the License.
 */

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Finos.Fdc3.Context;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Converters;

/// <summary>
/// Converts <see cref="IContext"/> objects to and from JSON, handling polymorphic serialization and deserialization.
/// </summary>
public class IContextJsonConverter : JsonConverter<IContext>
{
    /// <summary>
    /// Reads and deserializes a JSON representation of an <see cref="IContext"/> object.
    /// Determines the concrete type using the "type" property in the JSON payload and the <see cref="Finos.Fdc3.Context.ContextTypes"./>.
    /// </summary>
    /// <param name="reader">The reader to read from.</param>
    /// <param name="typeToConvert">The type to convert (ignored, as the actual type is determined at runtime).</param>
    /// <param name="options">Options to control the deserialization behavior.</param>
    /// <returns>The deserialized <see cref="IContext"/> instance.</returns>
    /// <exception cref="JsonException">Thrown if the "type" property is missing or empty.</exception>
    public override IContext? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var context = JsonNode.Parse(ref reader, new JsonNodeOptions() { PropertyNameCaseInsensitive = true });
        var contextType = (string?)context!["type"];

        if (string.IsNullOrEmpty(contextType))
        {
            throw new JsonException("Context type is missing or empty.");
        }

        var typeTo = ContextTypes.GetType(contextType!);

        var ctx = (IContext?)context!.Deserialize(typeTo, options);

        return ctx;
    }

    /// <summary>
    /// Writes an <see cref="IContext"/> object to JSON, using its actual runtime type for serialization.
    /// </summary>
    /// <param name="writer">The writer to write to.</param>
    /// <param name="value">The <see cref="IContext"/> value to serialize.</param>
    /// <param name="options">Options to control the serialization behavior.</param>
    public override void Write(Utf8JsonWriter writer, IContext value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
