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
using AppIdentifier = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.AppIdentifier;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Infrastructure.Internal;

internal class OpenClient : IOpenClient
{
    private readonly string _instanceId;
    private readonly IMessaging _messaging;
    private readonly IDesktopAgent _desktopAgent;
    private readonly ILogger<OpenClient> _logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions = SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization;

    public OpenClient(
        string instanceId,
        IMessaging messaging,
        IDesktopAgent desktopAgent,
        ILogger<OpenClient>? logger = null)
    {
        _instanceId = instanceId;
        _messaging = messaging;
        _desktopAgent = desktopAgent;
        _logger = logger ?? NullLogger<OpenClient>.Instance;
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    /// <param name="openAppContextId"></param>
    /// <returns><see cref="IContext"/></returns>
    public async ValueTask<IContext> GetOpenAppContextAsync(string openAppContextId)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug($"OpenClient: Retrieving open app context for app {_instanceId}...");
        }

        if (string.IsNullOrEmpty(openAppContextId))
        {
            throw ThrowHelper.MissingOpenAppContext();
        }

        var request = new GetOpenedAppContextRequest
        {
            ContextId = openAppContextId
        };

        var response = await _messaging.InvokeJsonServiceAsync<GetOpenedAppContextRequest, GetOpenedAppContextResponse>(
            Fdc3Topic.GetOpenedAppContext,
            request,
            _jsonSerializerOptions);

        if (response == null)
        {
            throw ThrowHelper.MissingResponse();
        }

        if (!string.IsNullOrEmpty(response.Error))
        {
            throw ThrowHelper.ErrorResponseReceived(response.Error);
        }

        if (string.IsNullOrEmpty(response.Context))
        {
            throw ThrowHelper.MissingOpenedAppContext();
        }

        var context = JsonSerializer.Deserialize<IContext>(response.Context!, _jsonSerializerOptions);

        if (context == null)
        {
            throw ThrowHelper.MissingOpenedAppContext();
        }

        return context;
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    /// <param name="app"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public async ValueTask<IAppIdentifier> OpenAsync(IAppIdentifier app, IContext? context)
    {
        if (context != null
            && string.IsNullOrEmpty(context.Type))
        {
            throw ThrowHelper.MalformedContext();
        }

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("OpenClient: Opening app {App} with context {Context}", app, context);
        }

        var currentChannel = await _desktopAgent.GetCurrentChannel();

        var request = new OpenRequest
        {
            InstanceId = _instanceId,
            AppIdentifier = new AppIdentifier
            {
                AppId = app.AppId,
                InstanceId = app.InstanceId
            },
            Context = JsonSerializer.Serialize(context, _jsonSerializerOptions),
            ChannelId = currentChannel?.Id
        };

        var response = await _messaging.InvokeJsonServiceAsync<OpenRequest, OpenResponse>(
            Fdc3Topic.Open,
            request,
            _jsonSerializerOptions);

        if (response == null)
        {
            throw ThrowHelper.MissingResponse();
        }

        if (!string.IsNullOrEmpty(response.Error))
        {
            throw ThrowHelper.ErrorResponseReceived(response.Error);
        }

        if (response.AppIdentifier == null)
        {
            throw ThrowHelper.AppIdentifierNotRetrieved();
        }

        return response.AppIdentifier;
    }
}
