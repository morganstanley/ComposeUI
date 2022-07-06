namespace ComposeUI.Messaging.Core.Messages;

public sealed class InvokeRequest : Message
{
    public InvokeRequest(string requestId, string serviceName, string? payload)
    {
        RequestId = requestId;
        ServiceName = serviceName;
        Payload = payload;
    }

    public override MessageType Type => MessageType.Invoke;
    public string RequestId { get; init; }
    public string ServiceName { get; init; }
    public string? Payload { get; init; }
}