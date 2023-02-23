using System;

namespace Shell.ImageSource
{
    public interface IImageSourcePolicy
    {
        bool IsAllowed(Uri uri, Uri appUri);
    }
}
