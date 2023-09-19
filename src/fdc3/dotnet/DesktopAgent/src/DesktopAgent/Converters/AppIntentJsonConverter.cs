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
using System.Text.Json.Serialization;
using MorganStanley.Fdc3;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Converters;

public class AppIntentJsonConverter : JsonConverter<AppIntent>
{
    /// <summary>
    /// <inheritdoc cref="System.Text.Json.Serialization.JsonConverter{MorganStanley.Fdc3.AppIntent}.Read(ref Utf8JsonReader, Type, JsonSerializerOptions)"/>
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="typeToConvert"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public override AppIntent? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<AppIntent>(ref reader, options);
    }

    /// <summary>
    /// <inheritdoc cref="System.Text.Json.Serialization.JsonConverter{MorganStanley.Fdc3.AppIntent}.Write(Utf8JsonWriter, AppIntent, JsonSerializerOptions)"/>
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="value"></param>
    /// <param name="options"></param>
    public override void Write(Utf8JsonWriter writer, AppIntent value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WritePropertyName(
            (options.PropertyNamingPolicy?.ConvertName(nameof(value.Intent))) ?? nameof(value.Intent));

        ((JsonConverter<IIntentMetadata>) options.GetConverter(typeof(IIntentMetadata))).Write(writer, value.Intent, options);

        writer.WritePropertyName(
            (options.PropertyNamingPolicy?.ConvertName(nameof(value.Apps))) ?? nameof(value.Apps));

        writer.WriteStartArray();

        var appConverter = (JsonConverter<AppMetadata>) options.GetConverter(typeof(AppMetadata));
        foreach (var app in value.Apps)
        {
            if (app is AppMetadata appMetadata)
                appConverter.Write(writer, appMetadata, options);
            else
                ((JsonConverter<IAppMetadata>) options.GetConverter(typeof(IAppMetadata))).Write(writer, app, options);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}
