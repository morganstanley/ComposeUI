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

using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;
using MorganStanley.ComposeUI.Utilities;

namespace MorganStanley.ComposeUI.Shell.Utilities;
public static class IconUtilities
{
    public static System.Windows.Media.ImageSource ToImageSource(this Bitmap bitmap)
    {
        try
        {
            using var memoryStream = new MemoryStream();

            bitmap.Save(memoryStream, ImageFormat.Png);
            memoryStream.Position = 0;

            var bitmapImage = new BitmapImage();

            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memoryStream;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze();

            return bitmapImage;
        }
        finally
        {
            NativeMethods.DeleteObject(bitmap.GetHbitmap());
        }
    }
}

