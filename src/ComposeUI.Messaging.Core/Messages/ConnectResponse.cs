namespace ComposeUI.Messaging.Core.Messages;

public sealed class ConnectResponse : Message
{
    public ConnectResponse(Guid clientId)
    {
        ClientId = clientId;
    }

    public override MessageType Type => MessageType.ConnectResponse;
    public Guid ClientId { get; init; }
}