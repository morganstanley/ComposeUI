using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using Accessibility;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Core.Raw;
using Shell.ImageSource;

namespace Shell;

/// <summary>
///     Interaction logic for WebWindow.xaml
/// </summary>
public partial class WebWindow : Window
{
    public WebWindow(WebWindowOptions options)
    {
        _options = options;
        InitializeComponent();

        // TODO: When no title is set from options, we should show the HTML document's title instead
        Title = options.Title ?? WebWindowOptions.DefaultTitle;
        Width = options.Width ?? WebWindowOptions.DefaultWidth;
        Height = options.Height ?? WebWindowOptions.DefaultHeight;
        TrySetIconUrl(options);

        _ = InitializeAsync();
    }

    public static void AddPreloadScript(string script)
    {
        PreloadScripts.Add(script);
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        RemoveLogicalChild(webView);
        webView.Dispose();
    }

    private static readonly HashSet<string> PreloadScripts = new();

    private readonly WebWindowOptions _options;
    private readonly ImageSourceProvider _iconProvider = new(new EnvironmentImageSourcePolicy());
    private bool _scriptsInjected;
    private readonly TaskCompletionSource _scriptInjectionCompleted = new();

    private async Task InitializeAsync()
    {
        await webView.EnsureCoreWebView2Async();
        await InitializeCoreWebView(webView.CoreWebView2);
        await LoadWebContentAsync(_options);
    }

    private void TrySetIconUrl(WebWindowOptions webWindowOptions)
    {
        if (webWindowOptions.IconUrl == null)
            return;

        // TODO: What's the default URL if the app is running from a manifest? We should probably not allow relative urls in that case.
        var appUrl = new Uri(webWindowOptions.Url ?? WebWindowOptions.DefaultUrl);

        var iconUrl = webWindowOptions.IconUrl != null
            ? new Uri(webWindowOptions.IconUrl, UriKind.RelativeOrAbsolute)
            : null;

        if (iconUrl != null)
        {
            Icon = _iconProvider.GetImageSource(iconUrl, appUrl);
        }
    }

    private async Task InitializeCoreWebView(CoreWebView2 coreWebView)
    {
        coreWebView.NewWindowRequested += (sender, args) => OnNewWindowRequested(args);
        coreWebView.WindowCloseRequested += (sender, args) => OnWindowCloseRequested(args);
        coreWebView.NavigationStarting += (sender, args) => OnNavigationStarting(args);
    }

    private void OnNavigationStarting(CoreWebView2NavigationStartingEventArgs args)
    {
        if (_scriptsInjected)
            return;

        args.Cancel = true;

        Dispatcher.InvokeAsync(
            async () =>
            {
                await InjectScriptsAsync(webView.CoreWebView2);

                webView.CoreWebView2.Navigate(args.Uri.ToString());
            });
    }

    private async Task LoadWebContentAsync(WebWindowOptions options)
    {
        webView.Source = new Uri(options.Url ?? WebWindowOptions.DefaultUrl);
    }

    private async Task InjectScriptsAsync(CoreWebView2 coreWebView)
    {
        if (_scriptsInjected)
            return;

        _scriptsInjected = true;
        await Task.WhenAll(PreloadScripts.Select(coreWebView.AddScriptToExecuteOnDocumentCreatedAsync));
        _scriptInjectionCompleted.SetResult();
    }

    private async void OnNewWindowRequested(CoreWebView2NewWindowRequestedEventArgs e)
    {
        using var deferral = e.GetDeferral();
        e.Handled = true;

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
    }

    private void OnWindowCloseRequested(object args)
    {
        Close();
    }
}
