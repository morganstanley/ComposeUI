/*
 * Morgan Stanley makes this available to you under the Apache License,
 * Version 2.0 (the "License"). You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0.
 *
 * See the NOTICE file distributed with this work for additional information
 * regarding copyright ownership. Unless required by applicable law or agreed
 * to in writing, software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
 * or implied. See the License for the specific language governing permissions
 * and limitations under the License.
 */

using System.Text.Json;
using Finos.Fdc3;
using Finos.Fdc3.Context;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Contracts;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Exceptions;
using MorganStanley.ComposeUI.Messaging.Abstractions;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Infrastructure;

internal class DesktopAgentClient : IDesktopAgent
{
    private readonly IMessaging _messaging;
    private readonly ILogger<DesktopAgentClient> _logger;
    private readonly string _appId;
    private readonly string _instanceId;
    private readonly JsonSerializerOptions _jsonSerializerOptions = SerializerOptionsHelper.JsonSerializerOptions;

    public DesktopAgentClient(
        IMessaging messaging,
        ILogger<DesktopAgentClient>? logger = null)
    {
        _messaging = messaging;
        _logger = logger ?? NullLogger<DesktopAgentClient>.Instance;

        _appId = Environment.GetEnvironmentVariable(nameof(AppIdentifier.AppId)) ?? throw new Fdc3DesktopAgentException("AppId for the app cannot be retrieved!");
        _instanceId = Environment.GetEnvironmentVariable(nameof(AppIdentifier.InstanceId)) ?? throw new Fdc3DesktopAgentException("InstanceId defined by the ModuleLoader/StartupAction for the app cannot be retrieved!");
    }

    public Task<IListener> AddContextListener<T>(string? contextType, ContextHandler<T> handler) where T : IContext
    {
        throw new NotImplementedException();
    }

    public Task<IListener> AddIntentListener<T>(string intent, IntentHandler<T> handler) where T : IContext
    {
        throw new NotImplementedException();
    }

    public Task Broadcast(IContext context)
    {
        throw new NotImplementedException();
    }

    public Task<IPrivateChannel> CreatePrivateChannel()
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<IAppIdentifier>> FindInstances(IAppIdentifier app)
    {
        throw new NotImplementedException();
    }

    public Task<IAppIntent> FindIntent(string intent, IContext? context = null, string? resultType = null)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<IAppIntent>> FindIntentsByContext(IContext context, string? resultType = null)
    {
        throw new NotImplementedException();
    }

    public async Task<IAppMetadata> GetAppMetadata(IAppIdentifier app)
    {
        var request = new GetAppMetadataRequest
        {
            Fdc3InstanceId = _instanceId,
            AppIdentifier = new Shared.Protocol.AppIdentifier
            {
                AppId = app.AppId,
                InstanceId = app.InstanceId,
            }
        };

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug($"Sending request to retrieve metadata for app: {app.AppId}, instanceId: {app.InstanceId}...");
        }

        var response = await _messaging.InvokeJsonServiceAsync<GetAppMetadataRequest, GetAppMetadataResponse>(
            Fdc3Topic.GetAppMetadata,
            request,
            _jsonSerializerOptions) ?? throw new Fdc3DesktopAgentException(Fdc3DesktopAgentErrors.NoResponse);

        if (response.Error != null)
        {
            throw new Fdc3DesktopAgentException($"{_appId} cannot return the {nameof(AppMetadata)} for app: {app.AppId} due to: {response.Error}.");
        }

        return response.AppMetadata!;
    }

    public Task<IChannel?> GetCurrentChannel()
    {
        throw new NotImplementedException();
    }

    public Task<IImplementationMetadata> GetInfo()
    {
        throw new NotImplementedException();
    }

    public Task<IChannel> GetOrCreateChannel(string channelId)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<IChannel>> GetUserChannels()
    {
        throw new NotImplementedException();
    }

    public Task JoinUserChannel(string channelId)
    {
        throw new NotImplementedException();
    }

    public Task LeaveCurrentChannel()
    {
        throw new NotImplementedException();
    }

    public Task<IAppIdentifier> Open(IAppIdentifier app, IContext? context = null)
    {
        throw new NotImplementedException();
    }

    public Task<IIntentResolution> RaiseIntent(string intent, IContext context, IAppIdentifier? app = null)
    {
        throw new NotImplementedException();
    }

    public Task<IIntentResolution> RaiseIntentForContext(IContext context, IAppIdentifier? app = null)
    {
        throw new NotImplementedException();
    }
}