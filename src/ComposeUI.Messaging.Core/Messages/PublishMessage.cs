namespace ComposeUI.Messaging.Core.Messages;

public sealed class PublishMessage : Message
{
    public PublishMessage(string topic, string? payload)
    {
        Topic = topic;
        Payload = payload;
    }

    public override MessageType Type => MessageType.Publish;
    public string Topic { get; init; }
    public string? Payload { get; init; }
}