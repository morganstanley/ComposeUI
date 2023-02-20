using System;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace Shell
{
    /// <summary>
    /// Interaction logic for MainWebWindow.xaml
    /// </summary>
    public partial class MainWebWindow : Window
    {
        public MainWebWindow(MainWebWindowOptions webWindowOptions)
        {
            InitializeComponent();

            Title = webWindowOptions.Title ?? MainWebWindowOptions.DefaultTitle;
            Width = webWindowOptions.Width ?? MainWebWindowOptions.DefaultWidth;
            Height = webWindowOptions.Height ?? MainWebWindowOptions.DefaultHeight;
            webView.Source = new Uri(webWindowOptions.Url ?? MainWebWindowOptions.DefaultUrl);

            webView.CoreWebView2InitializationCompleted += (sender, args) =>
            {
                if (args.IsSuccess)
                {
                    webView.CoreWebView2.NewWindowRequested += (sender, args) => OnNewWindowRequested(args);
                }
            };
        }

        private async void OnNewWindowRequested(CoreWebView2NewWindowRequestedEventArgs e)
        {
            e.Handled = true;
            var deferral = e.GetDeferral();
            var windowOptions = new MainWebWindowOptions { Url = e.Uri };
            
            if (e.WindowFeatures.HasSize)
            {
                windowOptions.Width = e.WindowFeatures.Width;
                windowOptions.Height = e.WindowFeatures.Height;
            }

            var window = new MainWebWindow(windowOptions);
            window.Show();
            await window.webView.EnsureCoreWebView2Async();
            e.NewWindow = window.webView.CoreWebView2;
            deferral.Complete();
        }
    }
}