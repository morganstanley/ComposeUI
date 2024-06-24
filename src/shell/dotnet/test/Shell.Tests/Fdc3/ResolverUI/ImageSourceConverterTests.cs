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

using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Windows.Media.Imaging;
using FluentAssertions;
using Icon = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.Icon;

namespace MorganStanley.ComposeUI.Shell.Fdc3.ResolverUI;

public class ImageSourceConverterTests
{
    [Fact]
    public void Convert_returns_null_when_UriFormatException()
    {
        var converter = new ImageSourceConverter();

        var result = converter.Convert(new Icon() {Src = "www.morganstanley.com"}, typeof(Icon), null, CultureInfo.CurrentCulture);

        result.Should().BeNull();
    }

    [Fact]
    public void Convert_returns_null_if_not_IIcon()
    {
        var converter = new ImageSourceConverter();

        var result = converter.Convert(new ResolverUIAppData(), typeof(ResolverUIAppData), null, CultureInfo.CurrentCulture);

        result.Should().BeNull();
    }

    [Fact]
    public void Convert_returns_null_if_image_not_found()
    {
        var converter = new ImageSourceConverter();
        var result =  converter.Convert(
            new Icon() { Src = "C:\\mydisk\\myfolder\\myicon.jpg" }, 
            typeof(Icon),
            null,
            CultureInfo.CurrentCulture);

        result.Should().BeNull();
    }

    [Fact]
    public void Convert_returns_Bitmap_when_web_app()
    {
        var converter = new ImageSourceConverter();

        var result = (BitmapImage)converter.Convert(new Icon() { Src = "https://www.microsoft.com/favicon.ico?v2" }, typeof(Icon), null, CultureInfo.CurrentCulture);

        result.UriSource.Should().Be(new Uri("https://www.microsoft.com/favicon.ico?v2"));
    }

    [Fact]
    public void Convert_returns_Bitmap_when_native_app()
    {
        var converter = new ImageSourceConverter();

        var path = string.Format($"{Directory.GetCurrentDirectory()}\\\\MorganStanley.ComposeUI.Shell.exe");
        var result = (BitmapImage) converter.Convert(new Icon() { Src = path }, typeof(Icon), null, CultureInfo.CurrentCulture);

        result.Should().NotBeNull();
    }
}