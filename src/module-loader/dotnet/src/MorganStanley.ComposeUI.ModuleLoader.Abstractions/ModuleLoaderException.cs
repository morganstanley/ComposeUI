using System.Runtime.Serialization;

namespace MorganStanley.ComposeUI.ModuleLoader;

public class ModuleLoaderException : Exception
{
    public ModuleLoaderException()
    {
    }

    protected ModuleLoaderException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public ModuleLoaderException(string? message) : base(message)
    {
    }

    public ModuleLoaderException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}