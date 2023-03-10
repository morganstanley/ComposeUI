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
using System.Text.Json.Serialization;
using FluentAssertions.Json;
using Newtonsoft.Json.Linq;

namespace MorganStanley.ComposeUI.Messaging;

public class MessageRouterJsonExtensionsTests
{
    [Fact]
    public async Task PublishJsonAsync_serializes_the_payload_to_json()
    {
        var messageRouter = new Mock<IMessageRouter>();
        var receivedMessages = new List<MessageBuffer>();

        messageRouter.Setup(
                _ => _.PublishAsync(
                    It.IsAny<string>(),
                    Capture.In(receivedMessages),
                    It.IsAny<PublishOptions>(),
                    It.IsAny<CancellationToken>()))
            .Verifiable();

        var testPayload = new TestPayload
        {
            Name = "test-name",
            Value = "test-value"
        };

        await messageRouter.Object.PublishJsonAsync("test", testPayload);

        messageRouter.Verify();

        var receivedJson = JObject.Parse(receivedMessages.Single().GetString());
        var expectedJson = JObject.Parse(@"{ ""key"": ""test-name"", ""value"": ""test-value"" }");

        receivedJson.Should().BeEquivalentTo(expectedJson);
    }

    [Fact]
    public async Task PublishJsonAsync_respects_the_provided_JsonSerializerOptions()
    {
        var messageRouter = new Mock<IMessageRouter>();
        var receivedMessages = new List<MessageBuffer>();

        messageRouter.Setup(
                _ => _.PublishAsync(
                    It.IsAny<string>(),
                    Capture.In(receivedMessages),
                    It.IsAny<PublishOptions>(),
                    It.IsAny<CancellationToken>()))
            .Verifiable();

        var testPayload = new TestPayloadWithoutAnnotations
        {
            Name = "test-name",
            Value = "test-value"
        };

        var jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
        };

        await messageRouter.Object.PublishJsonAsync(
            "test",
            testPayload,
            jsonSerializerOptions);

        messageRouter.Verify();

        var receivedJson = JObject.Parse(receivedMessages.Single().GetString());
        var expectedJson = JObject.Parse(@"{ ""name"": ""test-name"", ""value"": ""test-value"" }");

        receivedJson.Should().BeEquivalentTo(expectedJson);
    }

    private class TestPayload
    {
        [JsonPropertyName("key")]
        public string Name { get; set; }

        [JsonPropertyName("value")]
        public string Value { get; set; }
    }

    private class TestPayloadWithoutAnnotations
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public int IntValue { get; set; }
        public bool BoolValue { get; set; }
    }
}
