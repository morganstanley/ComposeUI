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

using Finos.Fdc3.Context;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared;
using System.Text.Json;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.Shared.Converters;

public class ValuationJsonConverterTests
{
    private readonly JsonSerializerOptions _options;

    public ValuationJsonConverterTests()
    {
        _options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        _options.Converters.Add(new ValuationJsonConverter());
    }

    [Theory]
    [InlineData("CURRENCY_ISOCODE")]
    [InlineData("currency_isocode")]
    [InlineData("currencY_ISOCODE")]
    public void Read_Should_Deserialize_With_Any_CurrencyIsoCode_Casing(string currencyKey)
    {
        var json = $@"{{
            ""{currencyKey}"": ""USD"",
            ""price"": 123.45,
            ""value"": 678.90,
            ""expiryTime"": ""2024-01-01T00:00:00Z"",
            ""valuationTime"": ""2024-01-02T00:00:00Z"",
            ""id"": ""abc123"",
            ""name"": ""TestValuation""
        }}";

        var result = JsonSerializer.Deserialize<Valuation>(json, _options);

        result.Should().NotBeNull();
        result!.CURRENCY_ISOCODE.Should().Be("USD");
        result.Price.Should().Be(123.45f);
        result.Value.Should().Be(678.90f);
        result.ExpiryTime.Should().Be("2024-01-01T00:00:00Z");
        result.ValuationTime.Should().Be("2024-01-02T00:00:00Z");
        result.ID.Should().Be("abc123");
        result.Name.Should().Be("TestValuation");
    }

    [Fact]
    public void Read_Should_Throw_If_CurrencyIsoCode_Missing()
    {
        var json = @"{
            ""price"": 123.45
        }";

        Action act = () => JsonSerializer.Deserialize<Valuation>(json, _options);

        act.Should().Throw<JsonException>()
            .WithMessage("*currencyISOCode*null*");
    }

    [Fact]
    public void Read_Should_Handle_Missing_Optional_Properties()
    {
        var json = @"{
            ""CURRENCY_ISOCODE"": ""EUR""
        }";

        var result = JsonSerializer.Deserialize<Valuation>(json, _options);

        result.Should().NotBeNull();
        result!.CURRENCY_ISOCODE.Should().Be("EUR");
        result.Price.Should().BeNull();
        result.Value.Should().BeNull();
        result.ExpiryTime.Should().BeNull();
        result.ValuationTime.Should().BeNull();
        result.ID.Should().BeNull();
        result.Name.Should().BeNull();
    }

    [Fact]
    public void Write_Should_Serialize_Valuation()
    {
        var valuation = new Valuation(
            "JPY", 1.23f, 4.56f, "2024-06-01T00:00:00Z", "2024-06-02T00:00:00Z", "id-xyz", "ValName"
        );

        var json = JsonSerializer.Serialize(valuation, _options);

        json.Should().Contain("\"currencY_ISOCODE\":\"JPY\"")
            .And.Contain("\"price\":1.23")
            .And.Contain("\"value\":4.56")
            .And.Contain("\"expiryTime\":\"2024-06-01T00:00:00Z\"")
            .And.Contain("\"valuationTime\":\"2024-06-02T00:00:00Z\"")
            .And.Contain("\"id\":\"id-xyz\"")
            .And.Contain("\"name\":\"ValName\"");
    }
}
