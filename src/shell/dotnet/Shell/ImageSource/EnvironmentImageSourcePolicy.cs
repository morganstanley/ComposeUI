using System;
using System.Linq;

namespace Shell.ImageSource;

public sealed class EnvironmentImageSourcePolicy : IImageSourcePolicy
{
    private const string _allowListEnvVar = "COMPOSE_ALLOWED_IMAGE_SOURCES";
    public bool IsAllowed(Uri uri, Uri appUri)
    {
        var allowListString = Environment.GetEnvironmentVariable(_allowListEnvVar);

        // Only allow http or https sources. If no sources are allowed, 
        if (!uri.Scheme.StartsWith("http"))
        {
            return false;
        }
        // If the source host is the same as the app host, allow it.            
        if (uri.Host == appUri.Host)
        {
            return true;
        }
        if (string.IsNullOrEmpty(allowListString))
        {
            return false;
        }

        var allowedSources = allowListString.Split(';');
        return allowedSources.Contains(uri.Host);
    }
}
