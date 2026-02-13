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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Contracts;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Exceptions;
using MorganStanley.ComposeUI.Messaging.Abstractions;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent;

internal class ModuleChannelSelector : IModuleChannelSelector
{
    private readonly ILogger<ModuleChannelSelector> _logger;
    private readonly IMessaging _messaging;
    private IAsyncDisposable? _handler;
    private readonly JsonSerializerOptions _jsonSerializerOptions = SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization;

    public ModuleChannelSelector(
        IMessaging messaging,
        ILogger<ModuleChannelSelector>? logger = null)
    {
        _messaging = messaging;
        _logger = logger ?? NullLogger<ModuleChannelSelector>.Instance;
    }

    public async ValueTask RegisterChannelSelectorHandlerInitiatedFromClientsAsync(
        string fdc3InstanceId,
        Action<string?> onChannelJoined,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(fdc3InstanceId))
        {
            throw ThrowHelper.MissingInstanceId(nameof(RegisterChannelSelectorHandlerInitiatedFromClientsAsync));
        }

        _handler = await _messaging.RegisterServiceAsync(
            Fdc3Topic.ChannelSelectorFromAPI(fdc3InstanceId),
            (channelId) =>
            {
                _logger.LogDebug("Request for instance: {InstanceId} was received with content: {ChannelId}", fdc3InstanceId, channelId);

                onChannelJoined(channelId);

                return new ValueTask<string?>(channelId);
            },
            cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<string?> InvokeJoinUserChannelFromUIAsync(string fdc3InstanceId, string channelId, CancellationToken cancellationToken = default)
    {
        var request = new JoinUserChannelRequest
        {
            InstanceId = fdc3InstanceId,
            ChannelId = channelId
        };

        var result = await _messaging.InvokeServiceAsync(Fdc3Topic.ChannelSelectorFromUI(fdc3InstanceId), JsonSerializer.Serialize(request, _jsonSerializerOptions), cancellationToken).ConfigureAwait(false);
        return result;
    }

    public ValueTask DisposeAsync()
    {
        if (_handler != null)
        {
            return _handler.DisposeAsync();
        }

        return new ValueTask();
    }
}