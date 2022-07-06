using System.Buffers;
using System.Text;
using ComposeUI.Messaging.Core.Messages;
using ComposeUI.Messaging.Prototypes.Serialization;

namespace ComposeUI.Messaging.Server.Tests;

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
        ((SubscribeMessage) message).Topic.Should().Be("a/b/c");
    }
}