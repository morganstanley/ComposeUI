// Morgan Stanley makes this available to you under the Apache License,
// Version 2.0 (the "License"). You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0.
// 
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership. Unless required by applicable law or agreed
// to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
// or implied. See the License for the specific language governing permissions
// and limitations under the License.

using FluentAssertions;
using MorganStanley.ComposeUI.ModuleLoader;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace MorganStanley.ComposeUI.Shell.Fdc3;

public class ComposeUIHostManifestJsonConverterTests
{
    private readonly JsonSerializerSettings _serializerSettings;

    public ComposeUIHostManifestJsonConverterTests()
    {
        _serializerSettings = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter> { new StringEnumConverter(new CamelCaseNamingStrategy()), new ComposeUIHostManifestConverter() },
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            }
        };
    }

    [Fact]
    public void CanConvert_Should_Return_True_For_DictionaryStringObject()
    {
        var converter = new ComposeUIHostManifestConverter();

        var canConvert = converter.CanConvert(typeof(Dictionary<string, object>));

        canConvert.Should().BeTrue();
    }

    [Fact]
    public void CanConvert_Should_Return_False_For_Other_Types()
    {
        var converter = new ComposeUIHostManifestConverter();

        var canConvert = converter.CanConvert(typeof(string));

        canConvert.Should().BeFalse();
    }

    [Fact]
    public void ReadJson_Should_Deserialize_ComposeUI_HostManifest()
    {
        var json = @"{
                ""ComposeUI"": {
                    ""InitialModulePosition"": ""Floating"",
                    ""Height"": 400,
                    ""Width"": 600,
                    ""Coordinates"": { ""X"": 100, ""Y"": 200 }
                },
                ""OtherProperty"": ""OtherValue""
            }";

        var result = JsonConvert.DeserializeObject<Dictionary<string, object>>(json, _serializerSettings);

        result.Should().ContainKey("ComposeUI");
        result["ComposeUI"].Should().BeOfType<ComposeUIHostManifest>();
        var composeUIHostManifest = (ComposeUIHostManifest) result["ComposeUI"];
        composeUIHostManifest.InitialModulePosition.Should().Be(InitialModulePosition.Floating);
        composeUIHostManifest.Height.Should().Be(400);
        composeUIHostManifest.Width.Should().Be(600);
        composeUIHostManifest.Coordinates.X.Should().Be(100);
        composeUIHostManifest.Coordinates.Y.Should().Be(200);

        result.Should().ContainKey("OtherProperty");
        result["OtherProperty"].Should().Be("OtherValue");
    }

    [Fact]
    public void WriteJson_Should_Serialize_ComposeUIHostManifest()
    {
        var dictionary = new Dictionary<string, object>
            {
                { "ComposeUI", new ComposeUIHostManifest
                    {
                        InitialModulePosition = InitialModulePosition.Floating,
                        Height = 400,
                        Width = 600,
                        Coordinates = new Coordinates { X = 100, Y = 200 }
                    }
                },
                { "OtherProperty", "OtherValue" }
            };

        var json = JsonConvert.SerializeObject(dictionary, _serializerSettings);

        var expectedJson = @"{
                ""ComposeUI"": {
                    ""initialModulePosition"": ""Floating"",
                    ""height"": 400.0,
                    ""width"": 600.0,
                    ""coordinates"": { ""x"": 100.0, ""y"": 200.0 }
                },
                ""OtherProperty"": ""OtherValue""
            }".Replace("\r\n", "").Replace(" ", "");

        json.Replace("\r\n", "").Replace(" ", "").Should().Be(expectedJson);
    }
}
