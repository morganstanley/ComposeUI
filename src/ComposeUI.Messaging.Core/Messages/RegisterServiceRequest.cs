namespace ComposeUI.Messaging.Core.Messages;

public sealed class RegisterServiceRequest : Message
{
    public RegisterServiceRequest(string serviceName)
    {
        ServiceName = serviceName;
    }

    public override MessageType Type => MessageType.RegisterService;
    public string ServiceName { get; init; }
}