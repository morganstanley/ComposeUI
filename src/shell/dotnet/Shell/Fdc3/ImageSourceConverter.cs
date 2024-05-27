// Morgan Stanley makes this available to you under the Apache License,
// Version 2.0 (the "License"). You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0.
// 
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership. Unless required by applicable law or agreed
// to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
// or implied. See the License for the specific language governing permissions
// and limitations under the License.

using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Finos.Fdc3;

namespace MorganStanley.ComposeUI.Shell.Fdc3;

public class ImageSourceConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not IIcon icon)
        {
            return null;
        }

        var uri = new Uri(icon.Src);
        if (uri == null)
        {
            return null;
        }
        
        if (uri.Scheme.StartsWith("http") || uri.Scheme.StartsWith("https"))
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = uri;
            bitmap.DecodePixelHeight = 10;
            bitmap.DecodePixelWidth = 10;
            bitmap.EndInit();
            return bitmap;
        }

        //TODO native apps
        throw new NotImplementedException();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
