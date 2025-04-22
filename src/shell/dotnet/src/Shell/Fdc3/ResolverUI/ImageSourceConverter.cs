﻿// Morgan Stanley makes this available to you under the Apache License,
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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Finos.Fdc3;
using MorganStanley.ComposeUI.Shell.Utilities;
using Icon = System.Drawing.Icon;

namespace MorganStanley.ComposeUI.Shell.Fdc3.ResolverUI;

public class ImageSourceConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not IIcon icon)
        {
            return null;
        }

        try
        {
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

            var path = uri.IsAbsoluteUri ? uri.AbsolutePath : uri.ToString();
            if (!File.Exists(path))
            {
                return null;
            }

            using var nativeIcon =
                Icon.ExtractAssociatedIcon(path);

            if (icon != null)
            {
                using var bitmap = nativeIcon.ToBitmap();

                return (BitmapImage) bitmap.ToImageSource();
            }

            return null;
        }
        catch (UriFormatException exception)
        {
            Debug.WriteLine(exception.ToString());
            return null;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}