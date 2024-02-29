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
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MorganStanley.ComposeUI.ModuleLoader;
using MorganStanley.ComposeUI.Shell.Layout;

namespace MorganStanley.ComposeUI.Shell.Modules;

internal sealed class ModuleService : IHostedService
{
    private readonly App _application;
    private readonly IModuleLoader _moduleLoader;
    private ConcurrentBag<object> _disposables = new();
    private readonly ILogger<ModuleService> _logger;

    public ModuleService(App application, IModuleLoader moduleLoader, ILogger<ModuleService>? logger = null)
    {
        _application = application;
        _moduleLoader = moduleLoader;
        _logger = logger ?? NullLogger<ModuleService>.Instance;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _disposables.Add(
            _moduleLoader.LifetimeEvents
                .OfType<LifetimeEvent.Started>()
                .Where(e => e.Instance.Manifest.ModuleType == ModuleType.Web)
                .Subscribe(OnWebModuleStarted));

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var disposable in _disposables)
        {
            (disposable as IDisposable)?.Dispose();
        }

        return Task.CompletedTask;
    }

    private async void OnWebModuleStarted(LifetimeEvent.Started e)
    {
        var properties = e.Instance.GetProperties().OfType<WebStartupProperties>().FirstOrDefault();
        if (properties == null) return;

        if (e.Instance.StartRequest.Parameters.Any(p => p.Key == WebWindow.XmlConstants.SerializationIdAttribute))
            return;

        var webWindowOptions = e.Instance.GetProperties().OfType<WebWindowOptions>().FirstOrDefault();

        try
        {
            await _application.Dispatcher.InvokeAsync(
                () =>
                {
                    DockingHelper.CreateDockingWindow<WebWindow>(
                        e.Instance,
                        webWindowOptions ?? new WebWindowOptions
                        {
                            Url = properties.Url.ToString(),
                            IconUrl = properties.IconUrl?.ToString()
                        });
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception thrown when trying to create a web window: {ExceptionType}: {ExceptionMessage}", ex.GetType().FullName, ex.Message);
        }
    }
}