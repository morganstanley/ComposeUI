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

using System.Text.Json;

namespace MorganStanley.ComposeUI.Messaging;

public class MessageBufferJsonExtensionsTests
{
    [Fact]
    public void ReadJson_can_deserialize_an_object()
    {
        var buffer = MessageBuffer.Create(@"{ ""Name"": ""test-name"", ""Value"": ""test-value"" }");

        var payload = buffer.ReadJson<TestPayload>();

        payload.Should()
            .BeEquivalentTo(
                new TestPayload
                {
                    Name = "test-name",
                    Value = "test-value"
                });
    }

    [Fact]
    public void ReadJson_respects_the_provided_JsonSerializerOptions()
    {
        var buffer = MessageBuffer.Create(@"{ ""name"": ""test-name"", ""value"": ""test-value"" }");

        var jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var payload = buffer.ReadJson<TestPayload>(
            jsonSerializerOptions);

        payload.Should()
            .BeEquivalentTo(
                new TestPayload
                {
                    Name = "test-name",
                    Value = "test-value"
                });
    }

    [Fact]
    public void CreateJson_creates_a_MessageBuffer_with_the_JSON_string()
    {
        var payload = new TestPayload
        {
            Name = "test-name",
            Value = "test-value"
        };

        var buffer = MessageBuffer.Factory.CreateJson(payload);

        JsonSerializer.Deserialize<TestPayload>(buffer.GetString()).Should().BeEquivalentTo(payload);
    }

    private class TestPayload
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
}
