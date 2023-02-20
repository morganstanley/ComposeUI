using System;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace Shell
{
    /// <summary>
    /// Interaction logic for WebWindow.xaml
    /// </summary>
    public partial class WebWindow : Window
    {
        public WebWindow(WebWindowOptions webWindowOptions)
        {
            InitializeComponent();

            Title = webWindowOptions.Title ?? WebWindowOptions.DefaultTitle;
            Width = webWindowOptions.Width ?? WebWindowOptions.DefaultWidth;
            Height = webWindowOptions.Height ?? WebWindowOptions.DefaultHeight;
            webView.Source = new Uri(webWindowOptions.Url ?? WebWindowOptions.DefaultUrl);

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
            var windowOptions = new WebWindowOptions { Url = e.Uri };
            
            if (e.WindowFeatures.HasSize)
            {
                windowOptions.Width = e.WindowFeatures.Width;
                windowOptions.Height = e.WindowFeatures.Height;
            }

            var window = new WebWindow(windowOptions);
            window.Show();
            await window.webView.EnsureCoreWebView2Async();
            e.NewWindow = window.webView.CoreWebView2;
            deferral.Complete();
        }
    }
}