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
using MorganStanley.Fdc3.Context;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Converters;

public class IntentResultJsonConverter : JsonConverter<IIntentResult>
{
    /// <summary>
    /// <inheritdoc cref="System.Text.Json.Serialization.JsonConverter{MorganStanley.Fdc3.IIntentResult}.Read(ref Utf8JsonReader, Type, JsonSerializerOptions)"/>
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="typeToConvert"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public override IIntentResult? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var typeReader = reader;
        var context = JsonSerializer.Deserialize<Context>(ref typeReader, options);
        if (context?.Type != null) return JsonSerializer.Deserialize<Context>(ref reader, options);
        else return JsonSerializer.Deserialize<IChannel>(ref reader, options);
    }

    /// <summary>
    /// <inheritdoc cref="System.Text.Json.Serialization.JsonConverter{MorganStanley.Fdc3.IIntentResult}.Write(Utf8JsonWriter, IIntentResult, JsonSerializerOptions)"/>
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="value"></param>
    /// <param name="options"></param>
    public override void Write(Utf8JsonWriter writer, IIntentResult value, JsonSerializerOptions options)
    {
        if (value is Context context)
        {
            JsonSerializer.Serialize(writer, context, options);
        }
        else if (value is IChannel channel)
        {
            JsonSerializer.Serialize(writer, channel, options);
        }
    }
}
