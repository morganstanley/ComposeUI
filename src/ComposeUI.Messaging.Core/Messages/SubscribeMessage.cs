namespace ComposeUI.Messaging.Core.Messages;

public sealed class SubscribeMessage : Message
{
    public override MessageType Type => MessageType.Subscribe;
    public string Topic { get; init; }
}