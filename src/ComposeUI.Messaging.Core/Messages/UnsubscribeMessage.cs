namespace ComposeUI.Messaging.Core.Messages;

public sealed class UnsubscribeMessage : Message
{
    public UnsubscribeMessage(string topic)
    {
        Topic = topic;
    }

    public override MessageType Type => MessageType.Unsubscribe;
    public string Topic { get; init; }
}