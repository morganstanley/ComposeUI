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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace Shell
{
    /// <summary>
    /// Interaction logic for WebContent.xaml
    /// </summary>
    public partial class WebContent : Window
    {
        private static string? _uriString;

        public WebContent(string str)
        {
            InitializeComponent();
            _uriString = str;

            webView2.Source = new Uri(_uriString);
        }
    }
}
