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
using Finos.Fdc3;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Converters;

internal class OptionalDesktopAgentFeaturesJsonConverter : JsonConverter<OptionalDesktopAgentFeatures>
{
    public override OptionalDesktopAgentFeatures? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<OptionalDesktopAgentFeatures>(ref reader, options);
    }

    public override void Write(Utf8JsonWriter writer, OptionalDesktopAgentFeatures value, JsonSerializerOptions options)
    {
        //options is not used in this case as it would serialize all the property names to camelCase
        JsonSerializer.Serialize(writer, value);
    }
}