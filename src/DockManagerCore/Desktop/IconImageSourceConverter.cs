/*
 * Morgan Stanley makes this available to you under the Apache License,
 * Version 2.0 (the "License"). You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0.
 *
 * See the NOTICE file distributed with this work for additional information
 * regarding copyright ownership. Unless required by applicable law or agreed
 * to in writing, software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
 * or implied. See the License for the specific language governing permissions
 * and limitations under the License.
 */
using System;
using System.Drawing;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Resources;

namespace DockManagerCore.Desktop
{
  public class IconImageSourceConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (!(value is Icon))
      {
        return null;
      }
      Icon icon = (Icon)value;
      return IconToImageSource16X16(icon);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (!(value is ImageSource))
      {
        return null;
      }
      ImageSource imageSource = (ImageSource)value;
      Uri uri = new Uri(imageSource.ToString());

      return UriToIcon(uri);
    }

    public static ImageSource IconToImageSource16X16(Icon icon_)
    {
      if (icon_ ==  null)
      {
        return null;
      }
      
      return Imaging.CreateBitmapSourceFromHIcon(
        icon_.Handle, 
        Int32Rect.Empty, 
        BitmapSizeOptions.FromWidthAndHeight(16, 16));
    }

    public static ImageSource IconToImageSource(Icon icon_)
    {
      if (icon_ == null)
      {
        return null;
      }

      return Imaging.CreateBitmapSourceFromHIcon(
        icon_.Handle,
        Int32Rect.Empty,
        BitmapSizeOptions.FromEmptyOptions());
    }

    public static Icon UriToIcon(Uri uri_)
    {      
      StreamResourceInfo streamInfo = Application.GetResourceStream(uri_);

      if (streamInfo == null)
      {
        return null;
      }

      return new Icon(streamInfo.Stream);
    }   
  }
}