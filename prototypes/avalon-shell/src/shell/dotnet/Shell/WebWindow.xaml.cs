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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Security.Policy;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using AvalonDock.Layout;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Web.WebView2.Core;
using MorganStanley.ComposeUI.ModuleLoader;
using MorganStanley.ComposeUI.Shell.ImageSource;
using MorganStanley.ComposeUI.Shell.Layout;

namespace MorganStanley.ComposeUI.Shell;

/// <summary>
///     Interaction logic for WebWindow.xaml
/// </summary>

public partial class WebWindow : LayoutAnchorable
{
    public WebWindow(
        WebWindowOptions options,
        IModuleLoader moduleLoader,
        IModuleInstance? moduleInstance = null,
        ILogger<WebWindow>? logger = null,
        IImageSourcePolicy? imageSourcePolicy = null) : this(false, options, moduleLoader, moduleInstance, logger, imageSourcePolicy)
    {
    }

    public WebWindow() : this(
        true,
        new WebWindowOptions(),
        App.Current.GetRequiredService<IModuleLoader>(),
        logger: App.Current.GetService<ILogger<WebWindow>>(),
        imageSourcePolicy: App.Current.GetService<IImageSourcePolicy>()
    ) { }

    private WebWindow(
        bool skipInitialize,
        WebWindowOptions options,
        IModuleLoader moduleLoader,
        IModuleInstance? moduleInstance = null,
        ILogger<WebWindow>? logger = null,
        IImageSourcePolicy? imageSourcePolicy = null)
    {
        CanHide = false;
        CanClose = true;
        _moduleLoader = moduleLoader;
        _moduleInstance = moduleInstance;
        _iconProvider = new ImageSourceProvider(imageSourcePolicy ?? new DefaultImageSourcePolicy());
        _options = options;
        _logger = logger ?? NullLogger<WebWindow>.Instance;
        InitializeComponent();

        // TODO: When no title is set from options, we should show the HTML document's title instead
        Title = options.Title ?? WebWindowOptions.DefaultTitle;
        FloatingWidth = options.Width ?? WebWindowOptions.DefaultWidth;
        FloatingHeight = options.Height ?? WebWindowOptions.DefaultHeight;

        if (!skipInitialize)
        {
            _ = InitializeAsync();
        }
    }

    public IModuleInstance? ModuleInstance => _moduleInstance;

    protected override void OnClosing(CancelEventArgs args)
    {
        // TODO: Send the closing event to the page, allow it to cancel

        if (_moduleInstance == null)
            return;

        switch (_lifetimeEvent)
        {
            case LifetimeEventType.Stopped:
                return;

            case LifetimeEventType.Stopping:
                args.Cancel = true;
                //Hide();
                return;

            default:
                args.Cancel = true;
                //Hide();
                Task.Run(() => _moduleLoader.StopModule(new StopRequest(_moduleInstance.InstanceId)));
                return;
        }
    }

    protected override void OnClosed()
    {
        base.OnClosed();
        //RemoveLogicalChild(WebView);
        WebView.Dispose();

        var disposables = _disposables.AsEnumerable().Reverse().ToArray();
        _disposables.Clear();

        foreach (var disposable in disposables)
        {
            disposable.Dispose();
        }
    }

    public override void ReadXml(XmlReader reader)
    {
        ReadAttribute(XmlConstants.ModuleIdAttribute, out var moduleId);
        ReadAttribute(XmlConstants.UrlAttribute, out var url);

        base.ReadXml(reader);

        if (moduleId == null)
        {
            Dispatcher.InvokeAsync(() =>
            {
                _options.Url = url;
                return InitializeAsync();
            });

            return;
        }

        var serializationId = Guid.NewGuid().ToString();

        _moduleLoader.LifetimeEvents
            .Where(
                e => e.EventType == LifetimeEventType.Started
                     && e.Instance.StartRequest.Parameters.Any(p => p.Key == XmlConstants.SerializationIdAttribute && p.Value == serializationId))
            .Subscribe(
                e =>
                {
                    Dispatcher.InvokeAsync(() =>
                    {
                        _moduleInstance = e.Instance;
                        var properties = _moduleInstance.GetProperties<WebStartupProperties>().FirstOrDefault();

                        if (properties != null)
                        {
                            _options.Url = properties.Url.ToString();
                            _options.IconUrl = properties.IconUrl?.ToString();
                        }

                        _options = _moduleInstance.GetProperties<WebWindowOptions>().FirstOrDefault() ?? _options;

                        return InitializeAsync();
                    });
                });

        _ = _moduleLoader.StartModule(
            new StartRequest(
                moduleId,
                new[] { new KeyValuePair<string, string>(XmlConstants.SerializationIdAttribute, serializationId) }));

        bool ReadAttribute(string attrName, out string? value)
        {
            if (reader.MoveToAttribute(attrName))
            {
                value = reader.Value;
                return true;
            }

            value = null;
            return false;
        }
    }

    public override void WriteXml(XmlWriter writer)
    {
        base.WriteXml(writer);

        if (_moduleInstance != null)
        {
            writer.WriteAttributeString(XmlConstants.ModuleIdAttribute, _moduleInstance.Manifest.Id);

            if (WebView.Source != null && WebView.Source != ((IModuleManifest<WebManifestDetails>)_moduleInstance.Manifest).Details.Url)
            {
                writer.WriteAttributeString(XmlConstants.UrlAttribute, WebView.Source.ToString());
            }
        }
        else if (WebView.Source != null)
        {
            writer.WriteAttributeString(XmlConstants.UrlAttribute, WebView.Source.ToString());
        }
    }

    internal static class XmlConstants
    {
        public const string ModuleIdAttribute = "ModuleId";
        public const string UrlAttribute = "Url";
        public const string SerializationIdAttribute = "SerializationId";
    }

    private readonly IModuleLoader _moduleLoader;
    private IModuleInstance? _moduleInstance;
    private WebWindowOptions _options;
    private readonly ILogger<WebWindow> _logger;
    private readonly ImageSourceProvider _iconProvider;
    private bool _scriptsInjected;
    private LifetimeEventType _lifetimeEvent = LifetimeEventType.Started;
    private readonly TaskCompletionSource _scriptInjectionCompleted = new();
    private readonly List<IDisposable> _disposables = new();

    private async Task InitializeAsync()
    {
        TrySetIconUrl(_options);

        if (_moduleInstance != null)
        {
            DisposeWhenClosed(
                _moduleLoader.LifetimeEvents
                    .Where(
                        e => e.Instance == _moduleInstance
                             && e.EventType is LifetimeEventType.Stopping or LifetimeEventType.Stopped)
                    .ObserveOn(SynchronizationContext.Current!)
                    .Subscribe(
                        Observer.Create(
                            (LifetimeEvent e) =>
                            {
                                _lifetimeEvent = e.EventType;
                                Close();
                            })));
        }

        await WebView.EnsureCoreWebView2Async();
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
            return;

        // TODO: What's the default URL if the app is running from a manifest? We should probably not allow relative urls in that case.
        var appUrl = new Uri(webWindowOptions.Url ?? WebWindowOptions.DefaultUrl);

        var iconUrl = webWindowOptions.IconUrl != null
            ? new Uri(webWindowOptions.IconUrl, UriKind.RelativeOrAbsolute)
            : null;

        if (iconUrl != null)
        {
            IconSource = _iconProvider.GetImageSource(iconUrl, appUrl);
        }
    }

    private Task InitializeCoreWebView2(CoreWebView2 coreWebView)
    {
        coreWebView.NewWindowRequested += (sender, args) => OnNewWindowRequested(args);
        coreWebView.WindowCloseRequested += (sender, args) => OnWindowCloseRequested(args);
        coreWebView.NavigationStarting += (sender, args) => OnNavigationStarting(args); 
        coreWebView.DocumentTitleChanged += (sender, args) => OnDocumentTitleChanged(args);

        return Task.CompletedTask;
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
            return;

        args.Cancel = true;

        Dispatcher.InvokeAsync(
            async () =>
            {
                await InjectScriptsAsync(WebView.CoreWebView2);

                WebView.CoreWebView2.Navigate(args.Uri);
            });
    }

    private Task LoadWebContentAsync(WebWindowOptions options)
    {
        WebView.Source = new Uri(options.Url ?? WebWindowOptions.DefaultUrl);

        return Task.CompletedTask;
    }

    private async Task InjectScriptsAsync(CoreWebView2 coreWebView)
    {
        if (_scriptsInjected)
            return;

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
                    }));
        }

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

        var window = DockingHelper.CreateDockingWindow<WebWindow>(new[]{ windowOptions });
        await window.WebView.EnsureCoreWebView2Async();
        e.NewWindow = window.WebView.CoreWebView2;
    }

    private void OnWindowCloseRequested(object args)
    {
        Close();
    }
}