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

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.Converters;

//TODO: remove this class, when no longer is neccessary
public class AppMetadataJsonConverter : JsonConverter<AppMetadata>
{
    //This is needed as JsonConstructors are missing and wrongly set attributes are in the AppMetadata (in fdc3-dotnet repo)
    //Could not deserialize into AppMetadata type as could not find screenshots attribute, and no JsonConstructor is applied to the ctor
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

        result.TryGetProperty("icons", out var icons);
        result.TryGetProperty("screenshots", out var screenshots); //it has "images" property name in the created JSON 

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
            icons.Deserialize<IEnumerable<Icon>>(options),
            screenshots.Deserialize<IEnumerable<Image>>(options),
            resultType);
    }

    public override void Write(Utf8JsonWriter writer, AppMetadata value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
