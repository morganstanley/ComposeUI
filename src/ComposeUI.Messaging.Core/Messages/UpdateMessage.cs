namespace ComposeUI.Messaging.Core.Messages;

public sealed class UpdateMessage : Message
{
    public UpdateMessage(string topic, string? payload)
    {
        Topic = topic;
        Payload = payload;
    }

    public override MessageType Type => MessageType.Update;
    public string Topic { get; init; }
    public string? Payload { get; init; }
}