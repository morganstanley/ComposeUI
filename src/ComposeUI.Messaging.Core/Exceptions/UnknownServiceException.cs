namespace ComposeUI.Messaging.Core.Exceptions;

public sealed class UnknownServiceException : MessageRouterException
{
    public UnknownServiceException() : base("Unknown service")
    {
    }
}