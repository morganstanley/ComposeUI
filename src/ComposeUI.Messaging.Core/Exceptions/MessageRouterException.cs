using System;
using System.Runtime.Serialization;

namespace ComposeUI.Messaging.Core.Exceptions;

public class MessageRouterException : Exception
{
    public MessageRouterException()
    {
    }

    protected MessageRouterException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public MessageRouterException(string? message) : base(message)
    {
    }

    public MessageRouterException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}