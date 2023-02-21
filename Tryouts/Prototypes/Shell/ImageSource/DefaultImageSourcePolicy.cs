using System;

namespace Shell.ImageSource
{
    public sealed class DefaultImageSourcePolicy : IImageSourcePolicy

    {
        public bool IsAllowed(Uri uri, Uri appUri)
        {
            return uri.Scheme.StartsWith("http") && uri.Host == appUri.Host;
        }
    }
}
