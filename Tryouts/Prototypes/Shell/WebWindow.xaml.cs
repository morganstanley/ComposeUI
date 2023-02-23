using System;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Shell.ImageSource;

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
            TrySetIconUrl(webWindowOptions);

            webView.CoreWebView2InitializationCompleted += (sender, args) =>
            {
                if (args.IsSuccess)
                {
                    webView.CoreWebView2.NewWindowRequested += (sender, args) => OnNewWindowRequested(args);
                    webView.CoreWebView2.WindowCloseRequested += (sender, args) => OnWindowCloseRequested(args);
                }
            };
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            this.RemoveLogicalChild(webView);
            webView.Dispose();
        }

        private readonly ImageSourceProvider _iconProvider = new ImageSourceProvider(new EnvironmentImageSourcePolicy());

        private void TrySetIconUrl(WebWindowOptions webWindowOptions)
        {
            if (webWindowOptions.IconUrl == null)
                return;

            // TODO: What's the default URL if the app is running from a manifest? We should probably not allow relative urls in that case.
            var appUrl = new Uri(webWindowOptions.Url ?? WebWindowOptions.DefaultUrl);
            var iconUrl = webWindowOptions.IconUrl != null ? new Uri(webWindowOptions.IconUrl, UriKind.RelativeOrAbsolute) : null;

            if (iconUrl != null)
            {
                Icon = _iconProvider.GetImageSource(iconUrl, appUrl);
            }
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

        private void OnWindowCloseRequested(object args)
        {
            this.Close();
        }
    }
}