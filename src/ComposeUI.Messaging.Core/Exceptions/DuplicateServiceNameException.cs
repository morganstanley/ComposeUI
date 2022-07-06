namespace ComposeUI.Messaging.Core.Exceptions;

public sealed class DuplicateServiceNameException : MessageRouterException
{
    public DuplicateServiceNameException() : base("Duplicate service name")
    {
    }
}