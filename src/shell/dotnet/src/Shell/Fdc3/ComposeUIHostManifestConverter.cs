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

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MorganStanley.ComposeUI.Shell.Fdc3;

internal sealed class ComposeUIHostManifestConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(Dictionary<string, object>);
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var dictionary = new Dictionary<string, object>();
        var jObject = JObject.Load(reader);

        foreach (var property in jObject.Properties())
        {
            if (property.Name == "ComposeUI")
            {
                var hostManifest = property.Value.ToObject<ComposeUIHostManifest>(serializer);
                dictionary[property.Name] = hostManifest;
            }
            else
            {
                dictionary[property.Name] = property.Value.ToObject<object>(serializer);
            }
        }

        return dictionary;
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        var dictionary = (Dictionary<string, object>) value;
        writer.WriteStartObject();

        foreach (var kvp in dictionary)
        {
            writer.WritePropertyName(kvp.Key);
            serializer.Serialize(writer, kvp.Value);
        }

        writer.WriteEndObject();
    }
}
