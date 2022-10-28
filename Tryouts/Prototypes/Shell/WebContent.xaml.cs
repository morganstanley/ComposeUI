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

namespace Shell
{
    /// <summary>
    /// Interaction logic for WebContent.xaml
    /// </summary>
    public partial class WebContent : Window
    {
        private static string _uriString = "https://www.morganstanley.com";
        private WebView2 _webView2 = new WebView2 { Name = "WebView2", Source = new Uri(_uriString) };

        private DockPanel _webViewDockPanel = new DockPanel();
        private DockPanel _navBarDockPanel = new DockPanel();
        private TextBox _textBox = new TextBox { Name = "addressBar" };

        public void SetUpAndShowWindow()
        {
            DockPanel.SetDock(_navBarDockPanel, Dock.Top);
            var button = new Button { Name = "ButtonGo", Content = "Go" };
            button.Click += ButtonGo_Click;
            DockPanel.SetDock(button, Dock.Right);

            _navBarDockPanel.Children.Add(button);
            _navBarDockPanel.Children.Add(_textBox);

            _webViewDockPanel.Children.Add(_navBarDockPanel);
            _webViewDockPanel.Children.Add(_webView2);

            this.Content = _webViewDockPanel;
            this.Title = "Compose Webmodule";
            this.Show();
        }

        public WebContent()
        {
            InitializeComponent();
            SetUpAndShowWindow();

        }

        private void ButtonGo_Click(object sender, RoutedEventArgs e)
        {
            if (_webView2 != null && _webView2.CoreWebView2 != null)
            {
                _webView2.CoreWebView2.Navigate(_textBox.Text);
            }
        }
    }
}
