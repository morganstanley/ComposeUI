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
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Converters;
using System.Text.Json.Serialization;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared;

/// <summary>
/// Provides a helper for configuring <see cref="JsonSerializerOptionsWithContextSerialization"/> used for FDC3 Desktop Agent serialization.
/// </summary>
public static class SerializerOptionsHelper
{
    /// <summary>
    /// Gets the configured <see cref="JsonSerializerOptionsWithContextSerialization"/> for serializing and deserializing FDC3 Desktop Agent models.
    /// </summary>
    /// <remarks>
    /// The options include custom converters for FDC3 types, camel case enum serialization, and ignore null values when writing.
    /// </remarks>
    public static JsonSerializerOptions JsonSerializerOptionsWithContextSerialization => new(JsonSerializerDefaults.Web)
    {
#if DEBUG
        WriteIndented = true,
#endif
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new AppMetadataJsonConverter(),
            new IntentMetadataJsonConverter(),
            new AppIntentJsonConverter(),
            new DisplayMetadataJsonConverter(),
            new IconJsonConverter(),
            new ImageJsonConverter(),
            new IntentMetadataJsonConverter(),
            new ImplementationMetadataJsonConverter(),
            new IContextJsonConverter(),
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };
}
