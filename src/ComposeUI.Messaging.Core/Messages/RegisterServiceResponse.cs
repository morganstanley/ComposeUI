namespace ComposeUI.Messaging.Core.Messages;

public sealed class RegisterServiceResponse : Message
{
    public RegisterServiceResponse(string serviceName, string? error = null)
    {
        ServiceName = serviceName;
        Error = error;
    }

    public override MessageType Type => MessageType.RegisterServiceResponse;
    public string ServiceName { get; init; }
    public string? Error { get; init; }
}