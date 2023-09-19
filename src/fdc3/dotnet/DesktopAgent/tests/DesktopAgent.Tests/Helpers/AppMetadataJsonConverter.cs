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

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.Helpers;

internal class AppMetadataJsonConverter : JsonConverter<AppMetadata>
{
    public override AppMetadata? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var result = JsonSerializer.Deserialize<JsonElement>(ref reader, options);

        string? appId = null, instanceId = null, name = null, version = null, title = null, tooltip = null, description = null, resultType = null;

        if (result.TryGetProperty("appId", out var idElement))
        {
            if (!string.IsNullOrEmpty(idElement.ToString())) appId = idElement.ToString();
        }

        if (result.TryGetProperty("instanceId", out var instanceIdElement))
        {
            if (!string.IsNullOrEmpty(instanceIdElement.ToString())) instanceId = instanceIdElement.ToString();
        }

        if (result.TryGetProperty("name", out var nameElement))
        {
            if (!string.IsNullOrEmpty(nameElement.ToString())) name = nameElement.ToString();
        }

        if (result.TryGetProperty("version", out var versionElement))
        {
            if (!string.IsNullOrEmpty(versionElement.ToString())) version = versionElement.ToString();
        }

        if (result.TryGetProperty("title", out var titleElement))
        {
            if (!string.IsNullOrEmpty(titleElement.ToString())) title = titleElement.ToString();
        }

        if (result.TryGetProperty("tooltip", out var tooltipElement))
        {
            if (!string.IsNullOrEmpty(tooltipElement.ToString())) tooltip = tooltipElement.ToString();
        }

        if (result.TryGetProperty("description", out var descriptionElement))
        {
            if (!string.IsNullOrEmpty(descriptionElement.ToString())) description = descriptionElement.ToString();
        }

        //TODO: implement IIcon and IImages JsonConverters
        result.TryGetProperty("icons", out var icons);
        result.TryGetProperty("screenshots", out var screenshots);

        if (result.TryGetProperty("resultType", out var resultTypeElement))
        {
            if (!string.IsNullOrEmpty(resultTypeElement.ToString())) resultType = resultTypeElement.ToString();
        }

        return new AppMetadata(
            appId!,
            instanceId,
            name,
            version,
            title,
            tooltip,
            description,
            icons.Deserialize<IEnumerable<IIcon>>(options),
            screenshots.Deserialize<IEnumerable<IImage>>(options),
            resultType);
    }

    public override void Write(Utf8JsonWriter writer, AppMetadata value, JsonSerializerOptions options)
    {
        var stringConverter = (JsonConverter<string>) options.GetConverter(typeof(string));
        writer.WriteStartObject();

        writer.WritePropertyName(
            (options.PropertyNamingPolicy?.ConvertName(nameof(value.AppId))) ?? nameof(value.AppId));

        stringConverter.Write(writer, value.AppId, options);

        writer.WritePropertyName(
            (options.PropertyNamingPolicy?.ConvertName(nameof(value.InstanceId))) ?? nameof(value.InstanceId));

        stringConverter.Write(writer, value.InstanceId, options);

        writer.WritePropertyName(
            (options.PropertyNamingPolicy?.ConvertName(nameof(value.Name))) ?? nameof(value.Name));

        stringConverter.Write(writer, value.Name, options);

        writer.WritePropertyName(
            (options.PropertyNamingPolicy?.ConvertName(nameof(value.Version))) ?? nameof(value.Version));

        stringConverter.Write(writer, value.Version, options);

        writer.WritePropertyName(
            (options.PropertyNamingPolicy?.ConvertName(nameof(value.Title))) ?? nameof(value.Title));

        stringConverter.Write(writer, value.Title, options);

        writer.WritePropertyName(
            (options.PropertyNamingPolicy?.ConvertName(nameof(value.Tooltip))) ?? nameof(value.Tooltip));

        stringConverter.Write(writer, value.Tooltip, options);

        writer.WritePropertyName(
            (options.PropertyNamingPolicy?.ConvertName(nameof(value.Description))) ?? nameof(value.Description));

        stringConverter.Write(writer, value.Description, options);


        //TODO(Lilla): check output, need to implement IIcon and IImage jsonconverters as well
        writer.WritePropertyName(
            (options.PropertyNamingPolicy?.ConvertName(nameof(value.Icons))) ?? nameof(value.Icons));

        ((JsonConverter<IEnumerable<IIcon>>) options.GetConverter(typeof(IEnumerable<IIcon>))).Write(writer, value.Icons, options);

        writer.WritePropertyName(
            (options.PropertyNamingPolicy?.ConvertName(nameof(value.Screenshots))) ?? nameof(value.Screenshots));

        ((JsonConverter<IEnumerable<IImage>>) options.GetConverter(typeof(IEnumerable<IImage>))).Write(writer, value.Screenshots, options);


        writer.WritePropertyName(
            (options.PropertyNamingPolicy?.ConvertName(nameof(value.ResultType))) ?? nameof(value.ResultType));

        stringConverter.Write(writer, value.ResultType, options);

        writer.WriteEndObject();
    }
}
