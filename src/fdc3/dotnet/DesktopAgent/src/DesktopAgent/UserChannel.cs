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

using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent;

internal class UserChannel : IAsyncDisposable
{
    public string Id { get; }
    private readonly ConcurrentDictionary<string, byte[]> _contexts = new();
    private byte[]? _lastContext = null;
    private readonly UserChannelTopics _topics;
    private readonly IMessagingService _desktopAgentService;
    private IAsyncDisposable? _broadcastSubscription;
    private bool _disposed = false;
    private readonly ILogger _logger;

    public UserChannel(
        string id, 
        IMessagingService desktopAgentService,
        ILogger<UserChannel>? logger)
    {
        Id = id;
        _topics = Fdc3Topic.UserChannel(id);
        _desktopAgentService = desktopAgentService;
        _logger = (ILogger?) logger ?? NullLogger.Instance;
    }

    public async ValueTask Connect()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(UserChannel));
        }

        await _desktopAgentService.ConnectAsync(CancellationToken.None);

        var broadcastSubscribing = _desktopAgentService.SubscribeAsync(_topics.Broadcast, HandleBroadcast, CancellationToken.None);

        await _desktopAgentService.RegisterServiceAsync<GetCurrentContextRequest>(_topics.GetCurrentContext, GetCurrentContext);
        
        _broadcastSubscription = await broadcastSubscribing;

        LogConnected();
    }

    internal ValueTask HandleBroadcast(ReadOnlySpan<byte> payload)
    {
        if (payload == null || payload.Length == 0)
        {
            LogNullOrEmptyBroadcast();
            return ValueTask.CompletedTask;
        }

        LogPayload(payload);

        JsonNode ctx;
        try
        {
            ctx = JsonNode.Parse(payload, new JsonNodeOptions() { PropertyNameCaseInsensitive = true })!;
        }
        catch (JsonException)
        {
            LogInvalidPayloadJson();
            return ValueTask.CompletedTask;
        }
        var contextType = (string?) ctx!["type"];

        if (string.IsNullOrEmpty(contextType))
        {
            LogMissingContextType();
            return ValueTask.CompletedTask;
        }

        _contexts[contextType] = payload.ToArray();
        _lastContext = payload.ToArray();

        return ValueTask.CompletedTask;
    }

    internal ValueTask<byte[]?> GetCurrentContext(GetCurrentContextRequest? request)
    {
        if (request == null || request.ContextType == null)
        {
            return ValueTask.FromResult<byte[]?>(_lastContext);
        }

        if (_contexts.TryGetValue(request.ContextType, out var context))
        {
            return ValueTask.FromResult<byte[]?>(context);
        }

        return ValueTask.FromResult<byte[]?>(null);
    }

    public async ValueTask DisposeAsync()
    {
        if (_broadcastSubscription != null)
        {
            await _broadcastSubscription.DisposeAsync();
        }
        
        _broadcastSubscription = null;

        await _desktopAgentService.UnregisterServiceAsync(_topics.GetCurrentContext, CancellationToken.None);

        _disposed = true;
    }

    private void LogConnected()
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug($"UserChannel {Id} connected to messagerouter with client id {_desktopAgentService.Id}");
        }
    }

    private void LogNullOrEmptyBroadcast()
    {
        if (_logger.IsEnabled(LogLevel.Warning))
        {
            _logger.LogWarning($"UserChannel {Id} received a null or empty payload in broadcast. This broadcast will be ignored.");
        }
    }

    private void LogInvalidPayloadJson()
    {
        if (_logger.IsEnabled(LogLevel.Warning))
        {
            _logger.LogWarning($"UserChannel {Id} could not parse the incoming broadcasted payload. This broadcast will be ignored.");
        }
    }

    private void LogMissingContextType()
    {
        if (_logger.IsEnabled(LogLevel.Warning))
        {
            _logger.LogWarning($"UserChannel {Id} received broadcasted payload with no context type specified. This broadcast will be ignored.");
        }
    }

    private void LogPayload(ReadOnlySpan<byte> payload)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug($"UserChannel {Id} received broadcasted payload: {Encoding.UTF8.GetString(payload)}");
        }
    }
}