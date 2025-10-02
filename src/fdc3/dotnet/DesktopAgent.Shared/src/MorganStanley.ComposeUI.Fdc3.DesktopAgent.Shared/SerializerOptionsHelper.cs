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
using Finos.Fdc3.Context;
using System.Xml.Linq;

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
        IncludeFields = true,
        PropertyNameCaseInsensitive = true,
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
            new ValuationJsonConverter(),
            new IContextJsonConverter(),
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    public class ValuationJsonConverter : JsonConverter<Valuation>
    {
        public override Valuation? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            string? currencyIsoCode = null;
            string? expiryTime = null, valuationTime = null, name = null;
            float? price = null, value = null;
            object? id = null;

            if (root.TryGetProperty("CURRENCY_ISOCODE", out var val)
                || root.TryGetProperty("currency_isocode", out val) 
                || root.TryGetProperty("currencY_ISOCODE", out val))
            {
                currencyIsoCode = val.GetString();
            }

            if (string.IsNullOrEmpty(currencyIsoCode))
            {
                throw new JsonException($"{nameof(Valuation)} cannot be desrialized as {nameof(currencyIsoCode)} is null.");
            }

            if (root.TryGetProperty("price", out var priceValue))
            {
                price = priceValue.GetSingle();
            }

            if (root.TryGetProperty("value", out var valueValue))
            {
                value = valueValue.GetSingle();
            }

            if (root.TryGetProperty("expiryTime", out var expiryTimeValue))
            {
                expiryTime = expiryTimeValue.GetString();
            }

            if (root.TryGetProperty("valuationTime", out var valuationTimeValue))
            {
                valuationTime = valuationTimeValue.GetString();
            }

            if (root.TryGetProperty("name", out var nameValue))
            {
                name = nameValue.GetString();
            }

            if (root.TryGetProperty("id", out var idValue))
            {
                id = idValue.GetString();
            }

            return new Valuation(currencyIsoCode, price, value, expiryTime, valuationTime, id, name);
        }

        public override void Write(Utf8JsonWriter writer, Valuation value, JsonSerializerOptions options)
        {
            // Create a copy of options without this converter
            var defaultOptions = new JsonSerializerOptions(options);

            // Remove this converter to avoid recursion
            var thisConverter = defaultOptions.Converters.FirstOrDefault(c => c.GetType() == typeof(ValuationJsonConverter));

            if (thisConverter != null)
            {
                defaultOptions.Converters.Remove(thisConverter);
            }

            JsonSerializer.Serialize(writer, value, defaultOptions);
        }
    }
}
