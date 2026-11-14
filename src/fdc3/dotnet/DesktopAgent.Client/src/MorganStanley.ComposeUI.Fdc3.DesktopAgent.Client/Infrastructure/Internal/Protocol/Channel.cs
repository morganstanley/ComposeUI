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
using System.Text.Json;
using Finos.Fdc3;
using Finos.Fdc3.Context;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Contracts;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Exceptions;
using MorganStanley.ComposeUI.Messaging.Abstractions;
using DisplayMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.DisplayMetadata;
using ContextMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.ContextMetadata;
using AppIdentifier = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.AppIdentifier;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Infrastructure.Internal;

internal class Channel : IChannel
{
    private readonly string _channelId;
    private readonly ChannelType _channelType;
    private readonly string _instanceId;
    private readonly IMessaging _messaging;
    private readonly IContext? _openedAppContext;
    private readonly DisplayMetadata? _displayMetadata;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<Channel> _logger;
    private readonly SemaphoreSlim _lastContextLock = new(1,1);
    private IContext? _lastContext = null;
    private ContextMetadata? _lastContextMetadata = null;
    private readonly ConcurrentDictionary<string, IContext> _lastContexts = new();
    private readonly ConcurrentDictionary<string, ContextMetadata?> _lastContextMetadatas = new();
    private readonly JsonSerializerOptions _jsonSerializerOptions = SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization;

    public Channel(
        string channelId,
        ChannelType channelType,
        IMessaging messaging,
        string instanceId,
        IContext? openedAppContext = null,
        DisplayMetadata? displayMetadata = null,
        ILoggerFactory? loggerFactory = null)
    {
        _channelId = channelId ?? throw ThrowHelper.MissingChannelId();
        _channelType = channelType;
        _instanceId = instanceId;
        _messaging = messaging;
        _openedAppContext = openedAppContext;
        _displayMetadata = displayMetadata;
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _logger = _loggerFactory.CreateLogger<Channel>();
    }

    public string Id => _channelId;

    public ChannelType Type => _channelType;

    public IDisplayMetadata? DisplayMetadata => _displayMetadata;

    protected ILoggerFactory LoggerFactory => _loggerFactory;
    protected IMessaging Messaging => _messaging;
    protected string InstanceId => _instanceId;

    public virtual async Task<IListener> AddContextListener<T>(string? contextType, ContextHandler<T> handler) where T : IContext
    {
        var listener = new ContextListener<T>(
            instanceId: _instanceId,
            contextHandler: handler,
            messaging: _messaging,
            contextType: contextType,
            openedAppContext: _openedAppContext,
            logger: _loggerFactory.CreateLogger<ContextListener<T>>());

        await listener.SubscribeAsync(_channelId, _channelType).ConfigureAwait(false);
        return listener;
    }

    public virtual async Task Broadcast(IContext context)
    {
        try
        {
            await _lastContextLock.WaitAsync().ConfigureAwait(false);

            _lastContexts.AddOrUpdate(
                context.Type,
                (key) => context,
                (key, existingContext) => context);

            _lastContext = context;

            // FDC3 3.0: Create fresh metadata with source information for this broadcast
            var metadata = new ContextMetadata
            {
                Source = new AppIdentifier
                {
                    AppId = _instanceId,
                    InstanceId = _instanceId
                }
            };

            // Publish both context and metadata as a payload
            var payload = new BroadcastPayload
            {
                Context = JsonSerializer.Serialize(context, _jsonSerializerOptions),
                Metadata = metadata
            };

            await _messaging.PublishJsonAsync(
                new ChannelTopics(_channelId, _channelType).Broadcast,
                payload,
                _jsonSerializerOptions).ConfigureAwait(false);
        }
        finally
        {
            _lastContextLock.Release();
        }
    }

    public virtual async Task<IContext?> GetCurrentContext(string? contextType)
    {
        try
        {
            await _lastContextLock.WaitAsync().ConfigureAwait(false);
            
            var request = new GetCurrentContextRequest { ContextType = contextType };

            var response = await _messaging.InvokeJsonServiceAsync<GetCurrentContextRequest, GetCurrentContextResponse>(
                new ChannelTopics(_channelId, _channelType).GetCurrentContext,
                request,
                _jsonSerializerOptions).ConfigureAwait(false);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("GetCurrentContext response: {ContextJson}", response?.Context);
            }

            if (response?.Context == null)
            {
                return null;
            }

            var context = JsonSerializer.Deserialize<IContext>(response.Context, _jsonSerializerOptions);

            if (context != null)
            {
                _lastContext = context;
                _lastContextMetadata = response.Metadata;

                _lastContexts.AddOrUpdate(
                    context.Type,
                    (key) => context,
                    (key, existingContext) => context);

                _lastContextMetadatas.AddOrUpdate(
                    context.Type,
                    (key) => response.Metadata,
                    (key, existingMetadata) => response.Metadata);
            }

            if (string.IsNullOrEmpty(contextType))
            {
                return _lastContext;
            }

            if (!_lastContexts.TryGetValue(contextType!, out var lastContext))
            {
                _logger.LogDebug("No context of type '{ContextType}' has been broadcasted on channel '{ChannelId}' yet.", contextType, _channelId);
            }

            return lastContext;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, @"Error while getting current context from channel '{ChannelId}'", _channelId);
            throw;
        }
        finally
        {
            _lastContextLock.Release();
        }
    }
}
