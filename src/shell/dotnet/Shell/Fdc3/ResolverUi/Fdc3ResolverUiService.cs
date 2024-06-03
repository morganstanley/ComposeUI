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
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Finos.Fdc3;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Converters;
using MorganStanley.ComposeUI.Messaging;

namespace MorganStanley.ComposeUI.Shell.Fdc3.ResolverUi;

internal class Fdc3ResolverUiService : IHostedService
{
    private readonly Fdc3Options _options;
    private readonly IHost _host;
    private readonly IResolverUiWindow _resolverUiWindow;
    private readonly ILogger<Fdc3ResolverUiService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        Converters = { new AppMetadataJsonConverter(), new IconJsonConverter() },
    };

    private readonly List<Func<ValueTask>> _disposeTask = new();
    private readonly object _disposeLock = new();

    public Fdc3ResolverUiService(
        IOptions<Fdc3Options> options,
        IHost host,
        IResolverUiWindow resolverUiWindow,
        ILogger<Fdc3ResolverUiService>? logger = null,
        ILoggerFactory? loggerFactory = null)
    {
        _options = options.Value;
        _host = host;
        _resolverUiWindow = resolverUiWindow;
        _logger = logger ?? NullLogger<Fdc3ResolverUiService>.Instance;
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await StartMessageRouterAsync(cancellationToken);
    }

    private async Task StartMessageRouterAsync(CancellationToken cancellationToken)
    {
        var messageRouter = _host.Services.GetRequiredService<IMessageRouter>();
        var topic = "ComposeUI/fdc3/v2.0/resolverUI";

        await messageRouter.RegisterServiceAsync(topic,
                async (endpoint, payload, context) =>
                {
                    var request = payload?.ReadJson<ResolverUiRequest>(_jsonSerializerOptions);
                    if (request == null)
                    {
                        return null;
                    }

                    var response = await ShowResolverUi(request.AppMetadata);

                    return response is null ? null : MessageBuffer.Factory.CreateJson(response, _jsonSerializerOptions);
                }, cancellationToken: cancellationToken);

        lock (_disposeLock)
        {
            _disposeTask.Add(async () =>
            {
                if (messageRouter != null)
                {
                    await messageRouter.UnregisterServiceAsync(topic, cancellationToken);
                    await messageRouter.DisposeAsync();
                }
            });
        }
    }

    private ValueTask<ResolverUiResponse> ShowResolverUi(IEnumerable<IAppMetadata> apps)
    {
        return _resolverUiWindow.ShowResolverUi(apps, TimeSpan.FromMinutes(1));
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Func<ValueTask>[]? reversedList = null;

        lock (_disposeLock)
        {
            reversedList = _disposeTask.AsEnumerable().Reverse().ToArray();
        }


        if (reversedList != null)
        {
            for (var i = 0; i < reversedList.Length; i++)
            {
                await reversedList[i].Invoke();
            }
        }
    }
}
