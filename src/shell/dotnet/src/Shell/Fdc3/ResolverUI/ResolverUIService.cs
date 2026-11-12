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
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.DependencyInjection;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Contracts;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Converters;
using MorganStanley.ComposeUI.Messaging.Abstractions;

namespace MorganStanley.ComposeUI.Shell.Fdc3.ResolverUI;

internal class ResolverUIService : IHostedService
{
    private readonly object _disposeLock = new();
    private IAsyncDisposable? _resolverUiService;
    private IAsyncDisposable? _legacyResolverUiService;
    private IAsyncDisposable? _resolverUiIntentService;
    private readonly List<Func<ValueTask>> _disposeTask = new();
    private readonly IHost _host;
    private readonly ILogger<ResolverUIService> _logger;

    private readonly JsonSerializerOptions _jsonSerializerOptions = SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization;

    private readonly IResolverUIProjector _resolverUIWindow;

    public ResolverUIService(
        IHost host,
        IResolverUIProjector resolverUIWindow,
        ILogger<ResolverUIService>? logger = null)
    {
        _host = host;
        _resolverUIWindow = resolverUIWindow;
        _logger = logger ?? NullLogger<ResolverUIService>.Instance;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await StartMessageRouterServiceAsync(cancellationToken);
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

    private async Task StartMessageRouterServiceAsync(CancellationToken cancellationToken)
    {
        var messageRouter = _host.Services.GetRequiredService<IMessaging>();

        _resolverUiService = await messageRouter.RegisterJsonServiceAsync<ResolverUIRequest, ResolverUIResponse>(
            Fdc3Topic.ResolverUI,
            async (request) =>
            {
                if (request == null)
                {
                    throw new ArgumentNullException(nameof(request));
                }

                return await ShowResolverUI(request.Candidates);
            },
            _jsonSerializerOptions,
            cancellationToken);

        if (_host.Services.GetService<IOptions<Fdc3DesktopAgentOptions>>()?.Value.AllowLegacyResolverUI == true)
        {
            _legacyResolverUiService = await messageRouter.RegisterJsonServiceAsync<ResolverUIRequest, ResolverUIResponse>(
                Fdc3Topic.LegacyResolverUI,
                async (request) =>
                {
                    if (request == null)
                    {
                        throw new ArgumentNullException(nameof(request));
                    }

                    if (_logger.IsEnabled(LogLevel.Warning))
                    {
                        _logger.LogWarning("The legacy ResolverUI contract is used, in which the ResolverUI returns the selected app itself. It is deprecated and is going to be removed.");
                    }

                    return await ShowResolverUI(request.AppMetadata);
                },
                _jsonSerializerOptions,
                cancellationToken);
        }

        _resolverUiIntentService = await messageRouter.RegisterJsonServiceAsync<ResolverUIIntentRequest, ResolverUIIntentResponse>(
              Fdc3Topic.ResolverUIIntent,
              async (request) =>
              {
                  if (request == null)
                  {
                      throw new ArgumentNullException(nameof(request));
                  }

                  return await ShowResolverUI(request.Intents!);
              },
              _jsonSerializerOptions,
              cancellationToken: cancellationToken);

        lock (_disposeLock)
        {
            _disposeTask.Add(
                async () =>
                {
                    var resolverUiTask = _resolverUiService == null ? ValueTask.CompletedTask : _resolverUiService.DisposeAsync();
                    var legacyResolverUiTask = _legacyResolverUiService == null ? ValueTask.CompletedTask : _legacyResolverUiService.DisposeAsync();
                    var resolverUiIntentTask = _resolverUiIntentService == null ? ValueTask.CompletedTask : _resolverUiIntentService.DisposeAsync();
                    await resolverUiTask;
                    await legacyResolverUiTask;
                    await resolverUiIntentTask;
                });
        }
    }

    private ValueTask<ResolverUIIntentResponse> ShowResolverUI(IEnumerable<string> intents)
    {
        return _resolverUIWindow.ShowResolverUI(intents, TimeSpan.FromMinutes(1));
    }

    private ValueTask<ResolverUIResponse> ShowResolverUI(IEnumerable<IAppMetadata> apps)
    {
        return _resolverUIWindow.ShowResolverUI(apps, TimeSpan.FromMinutes(1));
    }

    private async ValueTask<ResolverUIResponse> ShowResolverUI(IEnumerable<ResolverUICandidate> candidates)
    {
        var offered = candidates.ToArray();

        var response = await ShowResolverUI(offered.Select(candidate => candidate.AppMetadata).ToArray());

        if (response.Error != null)
        {
            return new ResolverUIResponse { Error = response.Error };
        }

        // The window gives back the very app object it was shown, so the selection is mapped back to the id of that candidate.
        var selected = offered.FirstOrDefault(candidate => ReferenceEquals(candidate.AppMetadata, response.AppMetadata));

        return selected == null
            ? new ResolverUIResponse { Error = ResolveError.ResolverUnavailable }
            : new ResolverUIResponse { SelectedCandidateId = selected.Id };
    }
}