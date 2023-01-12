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

using System.Buffers;
using System.Text;
using MorganStanley.ComposeUI.Messaging.Protocol.Messages;

namespace MorganStanley.ComposeUI.Messaging.Protocol.Json;

public class JsonMessageSerializerTests
{
    [Theory]
    [ClassData(typeof(SerializeDeserializeTheoryData))]
    public void Serialize_Deserialize_roundtrip_test(Message message)
    {
        var messageBytes = JsonMessageSerializer.SerializeMessage(message);
        var sequence = new ReadOnlySequence<byte>(messageBytes);
        var deserializedMessage = JsonMessageSerializer.DeserializeMessage(ref sequence);

        deserializedMessage.Should().BeOfType(message.GetType());
        deserializedMessage.Should().BeEquivalentTo(message);
    }

    [Fact]
    public void Deserialize_works_when_the_type_property_is_not_the_first_element()
    {
        var json = @"{ ""junk1"": ""junkText"", ""topic"": ""a/b/c"", ""type"": ""subscribe"" }";
        var messageBytes = Encoding.UTF8.GetBytes(json);
        var buffer = new ReadOnlySequence<byte>(messageBytes);


        var message = JsonMessageSerializer.DeserializeMessage(ref buffer);


        message.Should().BeOfType<SubscribeMessage>();
        ((SubscribeMessage)message).Topic.Should().Be("a/b/c");
    }

    private class SerializeDeserializeTheoryData : TheoryData<Message>
    {
        public SerializeDeserializeTheoryData()
        {
            Add(new ConnectRequest());

            Add(
                new ConnectRequest
                {
                    AccessToken = "abc",
                });

            Add(
                new ConnectResponse
                {
                    ClientId = "clientId",
                });

            Add(
                new ConnectResponse
                {
                    Error = new Error("errorType", "errorMessage"),
                });

            Add(
                new InvokeRequest
                {
                    Endpoint = "testEndpoint",
                    Payload = MessageBuffer.Create("testPayload"),
                    RequestId = "testRequestId",
                    CorrelationId = "testCorrelationId",
                });

            Add(
                new InvokeRequest
                {
                    Endpoint = "testEndpoint",
                    Payload = null,
                    RequestId = "testRequestId",
                });

            Add(
                new InvokeRequest
                {
                    Endpoint = "testEndpoint",
                    RequestId = "testRequestId",
                });

            Add(
                new InvokeRequest
                {
                    Endpoint = "testEndpoint",
                    RequestId = "testRequestId",
                    Scope = default,
                });

            Add(
                new InvokeRequest
                {
                    Endpoint = "testEndpoint",
                    RequestId = "testRequestId",
                    Scope = MessageScope.Parse(""),
                });

            Add(
                new InvokeRequest
                {
                    Endpoint = "testEndpoint",
                    RequestId = "testRequestId",
                    Scope = MessageScope.Parse("testScope"),
                });

            Add(
                new InvokeResponse
                {
                    RequestId = "testRequestId",
                    Payload = MessageBuffer.Create("testResponse"),
                });

            Add(
                new InvokeResponse
                {
                    RequestId = "testRequestId",
                    Error = new Error("errorType", "errorMessage"),
                });

            Add(
                new PublishMessage
                {
                    Topic = "testTopic",
                    Payload = MessageBuffer.Create("testPayload"),
                    Scope = default,
                    CorrelationId = "testCorrelationId",
                });

            Add(
                new RegisterServiceRequest
                {
                    Endpoint = "testEndpoint",
                });

            Add(
                new RegisterServiceResponse
                {
                    RequestId = "testRequestId",
                });

            Add(
                new RegisterServiceResponse
                {
                    RequestId = "testRequestId",
                    Error = new Error("errorType", "errorMessage"),
                });

            Add(
                new SubscribeMessage
                {
                    Topic = "testTopic",
                });

            Add(
                new UnregisterServiceRequest
                {
                    Endpoint = "testEndpoint",
                });

            Add(
                new UnsubscribeMessage
                {
                    Topic = "testTopic",
                });
        }
    }
}
