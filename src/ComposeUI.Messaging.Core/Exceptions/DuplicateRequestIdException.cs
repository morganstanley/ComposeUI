namespace ComposeUI.Messaging.Core.Exceptions;

public sealed class DuplicateRequestIdException : MessageRouterException
{
    public DuplicateRequestIdException() : base("Duplicate request ID")
    {
    }
}