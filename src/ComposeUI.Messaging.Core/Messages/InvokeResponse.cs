namespace ComposeUI.Messaging.Core.Messages;

public sealed class InvokeResponse : Message
{
    public InvokeResponse(string requestId, string? payload, string? error = null)
    {
        RequestId = requestId;
        Payload = payload;
        Error = error;
    }

    public override MessageType Type => MessageType.InvokeResponse;
    public string RequestId { get; init; }
    public string? Payload { get; init; }
    public string? Error { get; init; }
}