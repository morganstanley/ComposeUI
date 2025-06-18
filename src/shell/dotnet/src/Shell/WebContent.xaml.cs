// /*
//  * Morgan Stanley makes this available to you under the Apache License,
//  * Version 2.0 (the "License"). You may obtain a copy of the License at
//  *
//  *      http://www.apache.org/licenses/LICENSE-2.0.
//  *
//  * See the NOTICE file distributed with this work for additional information
//  * regarding copyright ownership. Unless required by applicable law or agreed
//  * to in writing, software distributed under the License is distributed on an
//  * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
//  * or implied. See the License for the specific language governing permissions
//  * and limitations under the License.
//  */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Web.WebView2.Core;
using MorganStanley.ComposeUI.ModuleLoader;
using MorganStanley.ComposeUI.Shell.ImageSource;
using MorganStanley.ComposeUI.Shell.Popup;
using System.Windows;

namespace MorganStanley.ComposeUI.Shell;

/// <summary>
///     Interaction logic for WebContent.xaml
/// </summary>
internal partial class WebContent : ContentPresenter, IDisposable
{
    public WebContent(
        WebWindowOptions options,
        IModuleLoader moduleLoader,
        IModuleInstance? moduleInstance = null,
        IImageSourcePolicy? imageSourcePolicy = null,
        IWindowPolicy? windowPolicy = null,
        ILoggerFactory? loggerFactory = null)
    {
        _moduleLoader = moduleLoader;
        _moduleInstance = moduleInstance;
        _imageSourcePolicy = imageSourcePolicy ?? new DefaultImageSourcePolicy();
        _iconProvider = new ImageSourceProvider(_imageSourcePolicy);
        _options = options;
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _logger = _loggerFactory.CreateLogger<WebContent>();
        _windowPolicy = windowPolicy;

        InitializeComponent();

        // TODO: When no title is set from options, we should show the HTML document's title instead
        Title = options.Title ?? WebWindowOptions.DefaultTitle;
        Width = options.Width ?? WebWindowOptions.DefaultWidth;
        Height = options.Height ?? WebWindowOptions.DefaultHeight;
        TrySetIconUrl(options);

        if (moduleInstance != null)
        {
            DisposeWhenClosed(
                _moduleLoader.LifetimeEvents
                    .Where(
                        e => e.Instance == moduleInstance
                             && e.EventType is LifetimeEventType.Stopping or LifetimeEventType.Stopped)
                    .ObserveOn(SynchronizationContext.Current!)
                    .Subscribe(
                        Observer.Create(
                            (LifetimeEvent e) =>
                            {
                                _lifetimeEvent = e.EventType;
                            })));
        }

        _ = InitializeAsync();
    }

    public IModuleInstance? ModuleInstance => _moduleInstance;
    public LifetimeEventType LifetimeEvent => _lifetimeEvent;

    public event EventHandler CloseRequested;

    public string Title { get; private set; }

    public System.Windows.Media.ImageSource? Icon { get; private set; }

    private readonly IModuleLoader _moduleLoader;
    private readonly IModuleInstance? _moduleInstance;
    private readonly IImageSourcePolicy _imageSourcePolicy;
    private readonly WebWindowOptions _options;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<WebContent> _logger;
    private readonly IWindowPolicy? _windowPolicy;
    private readonly ImageSourceProvider _iconProvider;
    private bool _scriptsInjected;
    private LifetimeEventType _lifetimeEvent = LifetimeEventType.Started;
    private readonly object _popupWindowLock = new object();
    private readonly List<Window> _childPopupWindows = new();
    private readonly TaskCompletionSource _scriptInjectionCompleted = new();
    private readonly List<IDisposable> _disposables = new();

    public WebWindowOptions Options => _options;

    private async Task InitializeAsync()
    {
        var environment = await CoreWebView2Environment.CreateAsync(options: new CoreWebView2EnvironmentOptions());

        await WebView.EnsureCoreWebView2Async(environment);
        await InitializeCoreWebView2(WebView.CoreWebView2);
        await LoadWebContentAsync(_options);
    }

    private void DisposeWhenClosed(IDisposable disposable)
    {
        _disposables.Add(disposable);
    }

    private void TrySetIconUrl(WebWindowOptions webWindowOptions)
    {
        if (webWindowOptions.IconUrl == null)
        {
            return;
        }

        // TODO: What's the default URL if the app is running from a manifest? We should probably not allow relative urls in that case.
        var appUrl = new Uri(webWindowOptions.Url ?? WebWindowOptions.DefaultUrl);

        var iconUrl = webWindowOptions.IconUrl != null
            ? new Uri(webWindowOptions.IconUrl, UriKind.RelativeOrAbsolute)
            : null;

        if (iconUrl != null)
        {
            Icon = _iconProvider.GetImageSource(iconUrl, appUrl, new(16, 16));
        }
    }

    private async Task InitializeCoreWebView2(CoreWebView2 coreWebView)
    {
        coreWebView.NewWindowRequested += (sender, args) => OnNewWindowRequested(args);
        coreWebView.WindowCloseRequested += (sender, args) => OnWindowCloseRequested(args);
        coreWebView.NavigationStarting += (sender, args) => OnNavigationStarting(args);
        coreWebView.DocumentTitleChanged += (sender, args) => OnDocumentTitleChanged(args);

        await Dispatcher.InvokeAsync(
            async () =>
            {
                await InjectScriptsAsync(coreWebView);
            });
    }

    private void OnDocumentTitleChanged(object args)
    {
        if (_options.Title == null)
        {
            Title = WebView.CoreWebView2.DocumentTitle;
        }
    }

    private void OnNavigationStarting(CoreWebView2NavigationStartingEventArgs args)
    {
        if (_scriptsInjected)
        {
            return;
        }

        args.Cancel = true;

        WebView.CoreWebView2.Navigate(args.Uri);
    }

    private Task LoadWebContentAsync(WebWindowOptions options)
    {
        WebView.Source = new Uri(options.Url ?? WebWindowOptions.DefaultUrl);

        return Task.CompletedTask;
    }

    private async Task InjectScriptsAsync(CoreWebView2 coreWebView)
    {
        if (_windowPolicy != null && !_windowPolicy.IsScriptInjectionAllowed())
        {
            _scriptsInjected = true;
            _scriptInjectionCompleted.SetResult();
            return;
        }

        if (_scriptsInjected)
        {
            return;
        }

        _scriptsInjected = true;
        var webProperties = _moduleInstance?.GetProperties().OfType<WebStartupProperties>().FirstOrDefault();

        if (webProperties != null)
        {
            await Task.WhenAll(
                webProperties.ScriptProviders.Select(
                    async scriptProvider =>
                    {
                        var script = await scriptProvider(_moduleInstance!);
                        await coreWebView.AddScriptToExecuteOnDocumentCreatedAsync(script);
                    })
                );

            if (webProperties.Fdc3ChannelSelectorControl is UIElement element)
            {
                LayoutRoot.Children.Add(element);
            }
        }

        _scriptInjectionCompleted.SetResult();
    }

    private void OnNewWindowRequested(CoreWebView2NewWindowRequestedEventArgs e)
    {
        lock (_popupWindowLock)
        {
            using var deferral = e.GetDeferral();
            e.Handled = true;

            var windowOptions = new WebWindowOptions
            {
                Url = e.Uri,
                InitialModulePostion = InitialModulePosition.FloatingOnly
            };

            if (e.WindowFeatures.HasSize)
            {
                windowOptions.Width = e.WindowFeatures.Width;
                windowOptions.Height = e.WindowFeatures.Height;
            }

            var webContent = new WebContent(
                options: windowOptions,
                moduleLoader: _moduleLoader,
                imageSourcePolicy: _imageSourcePolicy,
                windowPolicy: new DefaultPopupPolicy(),
                loggerFactory: _loggerFactory);

            var window = App.Current.CreateWindow<PopupWindow>();

            if (window == null)
            {
                _logger.LogError($"Popup window cannot be tracked!");
                return;
            }

            _childPopupWindows.Add(window);
            window.SetContent(webContent);
            window.Show();
        }
    }

    private void OnWindowCloseRequested(object args)
    {
        CloseRequested?.Invoke(args, EventArgs.Empty);
    }

    public void Dispose()
    {
        lock (_popupWindowLock)
        {
            foreach (var window in _childPopupWindows)
            {
                window.Close();
            }

            _childPopupWindows.Clear();

            RemoveLogicalChild(WebView);
            WebView.Dispose();

            var disposables = _disposables.AsEnumerable().Reverse().ToArray();
            _disposables.Clear();

            foreach (var disposable in disposables)
            {
                disposable.Dispose();
            }
        }
    }
}