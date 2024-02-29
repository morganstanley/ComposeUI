// /*
//  * Morgan Stanley makes this available to you under the Apache License,
//  * Version 2.0 (the "License"). You may obtain a copy of the License at
//  *
//  *      http://www.apache.org/licenses/LICENSE-2.0.
//  *
//  * See the NOTICE file distributed with this work for additional information
//  * regarding copyright ownership. Unless required by applicable law or agreed
//  * to in writing, software distributed under the License is distributed on an
//  * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
//  * or implied. See the License for the specific language governing permissions
//  * and limitations under the License.
//  */

using System;
using System.Windows.Media.Imaging;

namespace MorganStanley.ComposeUI.Shell.ImageSource;

public class ImageSourceProvider
{
    private readonly IImageSourcePolicy _imageSourcePolicy;
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
