using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.CommandLine;
using System.IO;
using System.CommandLine.Binding;
using System.Security.Policy;
using Shell.ImageSource;

namespace Shell
{
    /// <summary>
    /// Interaction logic for MainWebWindow.xaml
    /// </summary>
    public partial class MainWebWindow : Window
    {
        ImageSourceProvider _iconProvider = new ImageSourceProvider(new EnvironmentImageSourcePolicy());

        public MainWebWindow(MainWebWindowOptions webWindowOptions)
        {
            InitializeComponent();

            var appUrl = new Uri(webWindowOptions.Url ?? MainWebWindowOptions.DefaultUrl);
            var iconUrl = webWindowOptions.IconUrl != null ? new Uri(webWindowOptions.IconUrl, UriKind.RelativeOrAbsolute) : null;

            Title = webWindowOptions.Title ?? MainWebWindowOptions.DefaultTitle;
            Width = webWindowOptions.Width ?? MainWebWindowOptions.DefaultWidth;
            Height = webWindowOptions.Height ?? MainWebWindowOptions.DefaultHeight;
            if (iconUrl != null)
            {
                Icon = _iconProvider.GetImageSource(iconUrl, appUrl);
            }
            webView.Source = appUrl;
        }
    }
}