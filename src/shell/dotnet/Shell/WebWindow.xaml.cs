// Morgan Stanley makes this available to you under the Apache License,
// Version 2.0 (the "License"). You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0.
// 
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership. Unless required by applicable law or agreed
// to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
// or implied. See the License for the specific language governing permissions
// and limitations under the License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Web.WebView2.Core;
using MorganStanley.ComposeUI.ModuleLoader;
using MorganStanley.ComposeUI.Shell.ImageSource;
using MorganStanley.ComposeUI.Shell.Modules;
using MorganStanley.ComposeUI.Shell.Utilities;
using Nito.AsyncEx;

namespace MorganStanley.ComposeUI.Shell;

/// <summary>
///     Interaction logic for WebWindow.xaml
/// </summary>
public partial class WebWindow : Window
{                                                                                                
    public WebWindow(
        WebWindowOptions options,
        IModuleLoader moduleLoader,
        IModuleInstance? moduleInstance = null,
        IImageSourcePolicy? imageSourcePolicy = null,
        ILogger<WebWindow>? logger = null)
    {
        _webWindowId = Interlocked.Increment(ref _lastWebWindowId);
        _moduleLoader = moduleLoader;
        _moduleInstance = moduleInstance;
        _iconProvider = new ImageSourceProvider(imageSourcePolicy ?? new DefaultImageSourcePolicy());
        _options = options;
        _logger = logger ?? NullLogger<WebWindow>.Instance;
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
                                Close();
                            })));

            DisposeWhenClosed(
                _moduleLoader.LifetimeEvents
                    .Where(
                        e => e.Instance == moduleInstance
                             && e.EventType is LifetimeEventType.Started)
                    .ObserveOn(SynchronizationContext.Current!)
                    .Subscribe(Observer.Create((LifetimeEvent e) =>
                    {
                        Show();
                        _moduleStarted.Set();
                    })));
        }
        else
        {
            _moduleStarted.Set();
        }

        _ = InitializeAsyncCore();
    }

    public IModuleInstance? ModuleInstance => _moduleInstance;

    public Task InitializeAsync() => _initialized.WaitAsync();

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
                Hide();
                return;

            default:
                args.Cancel = true;
                Hide();
                Task.Run(() => _moduleLoader.StopModule(new StopRequest(_moduleInstance.InstanceId)));
                return;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        RemoveLogicalChild(WebView);
        WebView.Dispose();

        var disposables = _disposables.AsEnumerable().Reverse().ToArray();
        _disposables.Clear();

        foreach (var disposable in disposables)
        {
            disposable.Dispose();
        }
    }

    private static int _lastWebWindowId;
    private readonly int _webWindowId;
    private readonly IModuleLoader _moduleLoader;
    private readonly IModuleInstance? _moduleInstance;
    private readonly ILogger<WebWindow> _logger;
    private readonly WebWindowOptions _options;
    private readonly ImageSourceProvider _iconProvider;
    private bool _scriptsInjected;
    private LifetimeEventType _lifetimeEvent = LifetimeEventType.Started;
    private readonly List<IDisposable> _disposables = new();
    private readonly AsyncManualResetEvent _initialized = new();
    private readonly AsyncManualResetEvent _moduleStarted = new();
    private readonly string _hostObjectSecret = Guid.NewGuid().ToString();

    private async Task InitializeAsyncCore()
    {
        await _moduleStarted.WaitAsync();
        await WebView.EnsureCoreWebView2Async();
        await InitializeCoreWebView2(WebView.CoreWebView2);
        await LoadWebContentAsync(_options);
        _initialized.Set();
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
            Icon = _iconProvider.GetImageSource(iconUrl, appUrl);
        }
    }

    private async Task InitializeCoreWebView2(CoreWebView2 coreWebView)
    {
        AddHostObjects(coreWebView);
        await InjectScriptsAsync(coreWebView); 

        coreWebView.NewWindowRequested += (sender, args) => OnNewWindowRequested(args);
        coreWebView.WindowCloseRequested += (sender, args) => OnWindowCloseRequested(args);
        coreWebView.NavigationStarting += (sender, args) => OnNavigationStarting(args);
        coreWebView.NavigationCompleted += (sender, args) => OnNavigationCompleted(args);
        coreWebView.FrameCreated += (sender, args) => OnFrameCreated(args);
        coreWebView.FrameNavigationStarting += (sender, args) => OnFrameNavigationStarting(args);
        coreWebView.FrameNavigationCompleted += (sender, args) => OnFrameNavigationCompleted(args);
        coreWebView.ContentLoading += (sender, args) => OnContentLoading(args);
        coreWebView.DocumentTitleChanged += (sender, args) => OnDocumentTitleChanged(args);
    }

    private void OnDocumentTitleChanged(object args)
    {
        LogWebView2Event();

        if (_options.Title == null)
        {
            Title = WebView.CoreWebView2.DocumentTitle;
        }
    }

    private void OnNavigationCompleted(CoreWebView2NavigationCompletedEventArgs args)
    {
        LogWebView2Event(args.NavigationId);
    }

    private void OnFrameNavigationCompleted(CoreWebView2NavigationCompletedEventArgs args)
    {
        LogWebView2Event(args.NavigationId);
    }

    private void OnFrameNavigationStarting(CoreWebView2NavigationStartingEventArgs args)
    {
        LogWebView2Event(args.NavigationId);
    }

    private void OnContentLoading(CoreWebView2ContentLoadingEventArgs args)
    {
        LogWebView2Event(args.NavigationId);
    }

    private void OnNavigationStarting(CoreWebView2NavigationStartingEventArgs args)
    {
        LogWebView2Event(args.NavigationId);
    }

    private Task LoadWebContentAsync(WebWindowOptions options)
    {
        WebView.Source = new Uri(options.Url ?? WebWindowOptions.DefaultUrl);

        return Task.CompletedTask;
    }

    private void AddHostObjects(CoreWebView2 coreWebView)
    {
        if (_moduleInstance != null)
        {
            coreWebView.AddHostObjectToScript(
                ScriptProviderHostObject.HostObjectName,
                new ScriptProviderHostObject(_moduleInstance, _hostObjectSecret));
        }
    }

    private void AddHostObjects(CoreWebView2Frame frame)
    {
        if (_moduleInstance != null)
        {
            // TODO: adr-013
            // In accordance with the current state of adr-013, we don't inject scripts into iframes
            //frame.AddHostObjectToScript(
            //    ScriptProviderHostObject.HostObjectName,
            //    new ScriptProviderHostObject(_moduleInstance, _hostObjectSecret),
            //    new[] { "*" }); // TODO: TrustedOrigins
        }
    }

    private async Task InjectScriptsAsync(CoreWebView2 coreWebView)
    {
        if (_scriptsInjected)
            return;

        _scriptsInjected = true;
        var webProperties = _moduleInstance?.GetProperties().OfType<WebStartupProperties>().FirstOrDefault();

        if (webProperties == null) return;
        
        var flags = _options.IsPopup == true ? $"['{WebModuleScriptProviderFlags.Popup}']" : "[]";

        var script = $$"""
                       (function() {
                           
                           let flags = {{flags}};
                           
                           if (window.top !== window) {
                               flags.push('{{WebModuleScriptProviderFlags.Frame}}');
                           }
                           
                           console.debug('Injecting scripts with flags ' + (flags.length ? flags : '{{WebModuleScriptProviderFlags.None}}'));
                           const scripts = window.chrome.webview.hostObjects.sync.{{ScriptProviderHostObject.HostObjectName}}.{{nameof(ScriptProviderHostObject.GetScripts)}}(window.location.href, flags, '{{_hostObjectSecret}}');
                           
                           console.debug('Scripts found: ', scripts.length);
                           
                           scripts.forEach((script) => {
                               try {
                                   (new Function(script))();
                               }
                               catch (e) {
                                   console.error('Error while executing the injected script: ', { error: e, script: script });
                               }
                           });
                       })();
                       //# sourceURL=//composeui-preload
                       """;

        await coreWebView.AddScriptToExecuteOnDocumentCreatedAsync(script);
    }

    private async void OnNewWindowRequested(CoreWebView2NewWindowRequestedEventArgs e)
    {
        LogWebView2Event();
        using var deferral = e.GetDeferral();
        e.Handled = true;

        var uri = new Uri(e.Uri);
        var windowOptions = new WebWindowOptions {Url = e.Uri, IsPopup = true};

        if (e.WindowFeatures.HasSize)
        {
            windowOptions.Width = e.WindowFeatures.Width;
            windowOptions.Height = e.WindowFeatures.Height;
        }

        WebWindow? newWindow = null;

        // TODO: adr-013
        // In accordance with the current state of adr-013, we don't inject scripts into popup windows
        //var webProperties = _moduleInstance?.GetProperties<WebStartupProperties>().FirstOrDefault();
        //
        //if (webProperties != null && IsTrustedOrigin(uri, webProperties))
        //{
        //    var newInstance = await _moduleLoader.StartModule(
        //        new StartRequest(
        //            _moduleInstance!.Manifest.Id,
        //            new[]
        //            {
        //                new KeyValuePair<string, string>(
        //                    WebWindowOptions.ParameterName,
        //                    JsonSerializer.Serialize(windowOptions))
        //            }));

        //    newWindow = newInstance.GetProperties<WebWindow>().SingleOrDefault();
        //}

        if (newWindow == null)
        {
            newWindow = App.Current.CreateWindow<WebWindow>(windowOptions);
            newWindow.Show();
        }

        await newWindow.InitializeAsync();

        e.NewWindow = newWindow.WebView.CoreWebView2;
    }

    private bool IsTrustedOrigin(Uri uri, WebStartupProperties webProperties)
    {
        // TODO: Add TrustedOrigins to the manifest from Lilla's PR
        return webProperties.Url.IsSameOrigin(uri);
    }

    private void OnFrameCreated(CoreWebView2FrameCreatedEventArgs e)
    {
        LogWebView2Event();
        AddHostObjects(e.Frame);
    }

    private void OnWindowCloseRequested(object args)
    {
        LogWebView2Event();
        Close();
    }

    private void LogWebView2Event(ulong? navigationId = null, [CallerMemberName]string? methodName = null)
    {
        if (methodName == null) return;

        _logger.LogDebug($"[{_webWindowId}] " + (navigationId.HasValue ? $"{methodName}({navigationId})": methodName));
    }

    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public sealed class ScriptProviderHostObject
    {
        // flags is an object array, see https://github.com/MicrosoftEdge/WebView2Feedback/issues/2188#issuecomment-1119638117
        // This might not be necessary with later WV2 versions.
        public ScriptProviderHostObject(IModuleInstance moduleInstance, string secret)
        {
            _moduleInstance = moduleInstance;
            _secret = secret;
        }

        public async Task<string[]> GetScripts(string url, object[] flags, string secret)
        {
            if (secret != _secret) return new[] {"console.error('Invalid host object secret')"};

            var result = new List<string>();
            var parsedFlags = flags.Aggregate(
                WebModuleScriptProviderFlags.None,
                (f, obj) => obj is string s && Enum.TryParse<WebModuleScriptProviderFlags>(s, out var x) ? f | x : f);

            // TODO: TrustedOrigins
            if (_moduleInstance.Manifest is WebModuleManifest { Details: var details } && details.Url.IsSameOrigin(new Uri(url)))
            {
                result.Add(
                    $$"""
                      window.composeui = {
                         frameId: "{{Guid.NewGuid()}}"
                      }
                      """
                );
            }

            var scriptProviderParams = new WebModuleScriptProviderParameters(new Uri(url), parsedFlags);

            foreach (var scriptProvider in _moduleInstance.GetProperties<WebStartupProperties>()
                         .SelectMany(p => p.ScriptProviders))
            {
                var script = await scriptProvider(_moduleInstance, scriptProviderParams);
                
                if (!string.IsNullOrWhiteSpace(script))
                {
                    result.Add(script);
                }
            }

            return result.ToArray();
        }

        public const string HostObjectName = "ComposeUI_ScriptProvider";

        private readonly IModuleInstance _moduleInstance;
        private readonly string _secret;
    }
}
