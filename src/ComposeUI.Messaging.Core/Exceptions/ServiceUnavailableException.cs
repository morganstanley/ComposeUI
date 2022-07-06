namespace ComposeUI.Messaging.Core.Exceptions;

public sealed class ServiceUnavailableException : MessageRouterException
{
    public ServiceUnavailableException() : base("Service unavailable")
    {
    }
}