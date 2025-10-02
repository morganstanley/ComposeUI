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
using Finos.Fdc3.Context;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared;

/// <summary>
/// JsonConverter to fix the deserialization of the CURRENCY_ISOCODE
/// </summary>
public class ValuationJsonConverter : JsonConverter<Valuation>
{
    public override Valuation? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        string? currencyISOCode = null;
        string? expiryTime = null, valuationTime = null, name = null;
        float? price = null, value = null;
        object? id = null;

        if (root.TryGetProperty("CURRENCY_ISOCODE", out var val)
            || root.TryGetProperty("currency_isocode", out val)
            || root.TryGetProperty("currencY_ISOCODE", out val))
        {
            currencyISOCode = val.GetString();
        }

        if (string.IsNullOrEmpty(currencyISOCode))
        {
            throw new JsonException($"{nameof(Valuation)} cannot be desrialized as {nameof(currencyISOCode)} is null.");
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

        return new Valuation(currencyISOCode!, price, value, expiryTime, valuationTime, id, name);
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