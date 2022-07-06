using System.Text.Json.Serialization;

namespace ComposeUI.Messaging.Core.Messages;

public abstract class Message
{
    [JsonPropertyOrder(0)]
    public abstract MessageType Type { get; }

    public static Type ResolveMessageType(MessageType messageType)
    {
        return messageType switch
        {
            MessageType.Connect => typeof(ConnectRequest),
            MessageType.ConnectResponse => typeof(ConnectResponse),
            MessageType.Subscribe => typeof(SubscribeMessage),
            MessageType.Unsubscribe => typeof(UnsubscribeMessage),
            MessageType.Publish => typeof(PublishMessage),
            MessageType.Update => typeof(UpdateMessage),
            MessageType.Invoke => typeof(InvokeRequest),
            MessageType.RegisterService => typeof(RegisterServiceRequest),
            MessageType.InvokeResponse => typeof(InvokeResponse),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}