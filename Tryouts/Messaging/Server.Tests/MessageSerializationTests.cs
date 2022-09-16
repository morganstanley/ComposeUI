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
using MorganStanley.ComposeUI.Tryouts.Messaging.Core.Messages;
using MorganStanley.ComposeUI.Tryouts.Messaging.Core.Serialization;

namespace MorganStanley.ComposeUI.Tryouts.Messaging.Server.Tests;

public class MessageSerializationTests
{
    [Theory]
    [InlineData(@"{ ""type"": ""Connect"" }", typeof(ConnectRequest))]
    [InlineData(@"{ ""type"": ""ConnectResponse"" }", typeof(ConnectResponse))]
    public void Deserialize_creates_the_correct_message_type(string json, Type messageType)
    {
        var messageBytes = Encoding.UTF8.GetBytes(json);
        var buffer = new ReadOnlySequence<byte>(messageBytes);


        var message = JsonMessageSerializer.DeserializeMessage(ref buffer);


        message.Should().BeOfType(messageType);
        buffer.Length.Should().Be(0); // Assert that the whole JSON input was consumed
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
}