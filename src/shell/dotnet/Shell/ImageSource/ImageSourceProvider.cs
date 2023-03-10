using System;
using System.Windows.Media.Imaging;

namespace Shell.ImageSource
{
    public class ImageSourceProvider
    {
        IImageSourcePolicy _imageSourcePolicy;
        public ImageSourceProvider(IImageSourcePolicy imageSourcePolicy)
        {
            _imageSourcePolicy = imageSourcePolicy;
        }

        public System.Windows.Media.ImageSource? GetImageSource(Uri uri, Uri appUri)
        {
            if (!uri.IsAbsoluteUri)
            {
                uri = new Uri(appUri, uri);
            }

            if (_imageSourcePolicy.IsAllowed(uri, appUri))
            {
                return BitmapFrame.Create(uri);
            }
            return null;
        }
    }
}
