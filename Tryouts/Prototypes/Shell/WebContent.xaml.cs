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
        private static string _uriString = "https://www.morganstanley.com";
        
        public void ShowWindow()
        {
            this.Show();
        }

        public WebContent()
        {
            InitializeComponent();

            webView2.Source = new Uri(_uriString);
        }

        private void ButtonGo_Click(object sender, RoutedEventArgs e)
        {
            if (webView2 != null && webView2.CoreWebView2 != null)
            {
                webView2.CoreWebView2.Navigate(addressBar.Text);
            }
        }
    }
}
