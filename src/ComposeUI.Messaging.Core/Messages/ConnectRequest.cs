namespace ComposeUI.Messaging.Core.Messages;

public sealed class ConnectRequest : Message
{
    public ConnectRequest()
    {
    }

    public ConnectRequest(Guid clientId)
    {
        ClientId = clientId;
    }

    public override MessageType Type => MessageType.Connect;
    public Guid? ClientId { get; init; }
}