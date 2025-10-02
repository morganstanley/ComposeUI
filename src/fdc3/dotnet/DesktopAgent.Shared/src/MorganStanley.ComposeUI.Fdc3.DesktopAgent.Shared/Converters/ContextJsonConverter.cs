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

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Converters;

/// <summary>
/// This converter is necessary to keep all Context data on deserialization.
/// At the moment we are using it to handle the Context properties as a raw JSON string, in the future we might want to consider a more sophisticated way.
/// </summary>
internal class ContextJsonConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var jsonDoc = JsonDocument.ParseValue(ref reader);
        return jsonDoc.RootElement.GetRawText();
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteRawValue(value);
    }
}
