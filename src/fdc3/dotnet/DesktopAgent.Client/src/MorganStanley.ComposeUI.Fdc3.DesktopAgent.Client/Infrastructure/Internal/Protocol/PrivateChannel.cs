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
using DisplayMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.DisplayMetadata;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Infrastructure.Internal.Protocol;

internal class PrivateChannel : Channel, IPrivateChannel, IAsyncDisposable
{
    private readonly ILogger<PrivateChannel> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IMessaging _messaging;
    private readonly PrivateChannelTopics _privateChannelTopics;
    private readonly string _channelId;
    private readonly string _instanceId;
    private readonly string _internalEventsTopic;
    private readonly bool _isOriginalCreator;
    private readonly string _remoteContextListenersService;
    private readonly TaskCompletionSource<string> _initializationTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly SemaphoreSlim _contextHandlersLock = new(1, 1);
    private readonly List<IListener> _contextHandlers = new();

    private readonly SemaphoreSlim _addContextListenerHandlersLock = new(1, 1);
    private readonly List<PrivateChannelContextListenerEventListener> _addContextListenerHandlers = new();

    private readonly SemaphoreSlim _unsubscribeHandlersLock = new(1, 1);
    private readonly List<PrivateChannelContextListenerEventListener> _unsubscribeHandlers = new();

    private readonly SemaphoreSlim _disconnectHandlersLock = new(1, 1);
    private readonly List<PrivateChannelDisconnectEventListener> _disconnectHandlers = new();

    private readonly JsonSerializerOptions _jsonSerializerOptions = SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization;

    private IAsyncDisposable _subscription;
    private IAsyncDisposable _serviceRegistration;
    private bool _isDisconnected = false;
    private readonly SemaphoreSlim _isDisconnectedLock = new(1, 1);

    public PrivateChannel(
        string channelId, 
        IMessaging messaging, 
        string instanceId,
        bool isOriginalCreator,
        DisplayMetadata? displayMetadata = null,
        ILoggerFactory? loggerFactory = null) 
        : base(channelId, ChannelType.Private, messaging, instanceId, displayMetadata, loggerFactory)
    {
        _privateChannelTopics = Fdc3Topic.PrivateChannel(channelId);

        _internalEventsTopic = _privateChannelTopics.Events;
        _isOriginalCreator = isOriginalCreator;
        _remoteContextListenersService = _privateChannelTopics.GetContextHandlers(!isOriginalCreator);
        _messaging = messaging;
        _channelId = channelId;
        _instanceId = instanceId;
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _logger = _loggerFactory.CreateLogger<PrivateChannel>();

        Task.Run(() => InitializeAsync());
    }

    private async ValueTask InitializeAsync()
    {
        try
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug($"Subscribing to private channel internal events for channel '{_channelId}', instance '{_instanceId}', topic: '{_internalEventsTopic}'.");
            }

            _subscription = await _messaging.SubscribeAsync(_internalEventsTopic, HandleInternalEvent);


            var topic = _privateChannelTopics.GetContextHandlers(_isOriginalCreator);
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug($"Registering remote private channel context handlers service for channel '{_channelId}', instance '{_instanceId}', service: '{topic}'.");
            }

            _serviceRegistration = await _messaging.RegisterServiceAsync(topic, HandleRemoteContextListener);

            _initializationTaskCompletionSource.SetResult(_channelId);
        }
        catch (Exception exception)
        {
            _initializationTaskCompletionSource.SetException(exception);
        }
    }

    public override async Task Broadcast(IContext context)
    {
        await _initializationTaskCompletionSource.Task.ConfigureAwait(false);

        try
        {
            await _isDisconnectedLock.WaitAsync().ConfigureAwait(false);

            if (_isDisconnected)
            {
                throw ThrowHelper.PrivateChannelDisconnected(_channelId, _instanceId);
            }

            await base.Broadcast(context);
        }
        finally
        {
            _isDisconnectedLock.Release();
        }
    }

    public override async Task<IListener> AddContextListener<T>(string? contextType, ContextHandler<T> handler)
    {
        await _initializationTaskCompletionSource.Task.ConfigureAwait(false);

        try
        {
            await _isDisconnectedLock.WaitAsync().ConfigureAwait(false);
            await _contextHandlersLock.WaitAsync().ConfigureAwait(false);

            if (_isDisconnected)
            {
                throw ThrowHelper.PrivateChannelDisconnected(_channelId, _instanceId);
            }

            var listener = await base.AddContextListener(contextType, handler) as ContextListener<T>;

            if (listener != null)
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug($"Setting unsubscribe callback for {nameof(ContextListener<T>)} when adding the listener on the {nameof(PrivateChannel)}...");
                }

                listener.SetUnsubscribeCallback(contextListener => RemoveContextHandler(contextListener));

                _contextHandlers.Add(listener);
                
                _ = FireContextHandlerAdded(contextType);

                return listener;
            }

            throw ThrowHelper.PrivatChannelSubscribeFailure(contextType, _channelId, _instanceId);
        }
        finally
        {
            _isDisconnectedLock.Release();
            _contextHandlersLock.Release();
        }
    }

    public async void Disconnect()
    {
        await _initializationTaskCompletionSource.Task.ConfigureAwait(false);

        try
        {
            await _isDisconnectedLock.WaitAsync().ConfigureAwait(false);
            await _addContextListenerHandlersLock.WaitAsync().ConfigureAwait(false);
            await _unsubscribeHandlersLock.WaitAsync().ConfigureAwait(false);
            await _disconnectHandlersLock.WaitAsync().ConfigureAwait(false);
            await _contextHandlersLock.WaitAsync().ConfigureAwait(false);

            if (_isDisconnected)
            {
                return;
            }

            _isDisconnected = true;
            foreach (var handler in _addContextListenerHandlers)
            {
                handler.UnsubscribeCore(false);

                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    _logger.LogTrace($"Unsubscribed add context listener handler on private channel '{_channelId}'.");
                }
            }

            foreach (var handler in _unsubscribeHandlers)
            {
                handler.UnsubscribeCore(false);

                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    _logger.LogTrace($"Unsubscribed unsubscribe handler on private channel '{_channelId}'.");
                }
            }

            foreach (var handler in _disconnectHandlers)
            {
                handler.UnsubscribeCore(false);

                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    _logger.LogTrace($"Unsubscribed disconnect handler on private channel '{_channelId}'.");
                }
            }

            _addContextListenerHandlers.Clear();
            _unsubscribeHandlers.Clear();
            _disconnectHandlers.Clear();

            foreach (var listener in _contextHandlers)
            {
                //Fire and forget
                listener.Unsubscribe();

                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    _logger.LogTrace($"Unsubscribed context listener on private channel '{_channelId}'.");
                }
            }

            var request = PrivateChannelInternalEvents.Disconnected(_instanceId);
            var serializedRequest = JsonSerializer.Serialize(request, _jsonSerializerOptions);

            await _messaging.PublishAsync(_internalEventsTopic, serializedRequest);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug($"Private channel '{_channelId}' disconnected. Request: {serializedRequest} has been sent to the backend...");
            }
        }
        finally
        {
            _isDisconnectedLock.Release();
            _addContextListenerHandlersLock.Release();
            _unsubscribeHandlersLock.Release();
            _disconnectHandlersLock.Release();
            _contextHandlersLock.Release();
        }
    }

    public IListener OnAddContextListener(Action<string?> handler)
    {
        try
        {
            _isDisconnectedLock.Wait();
            _addContextListenerHandlersLock.Wait();

            if (_isDisconnected)
            {
                throw ThrowHelper.PrivateChannelDisconnected(_channelId, _instanceId);
            }

            var listener = new PrivateChannelContextListenerEventListener(
                onContext: (contextType) => handler(contextType),
                onUnsubscribe: RemoveAddContextListenerHandler,
                logger: _loggerFactory.CreateLogger<PrivateChannelContextListenerEventListener>());

            _addContextListenerHandlers.Add(listener);

            //Fire and forget
            _ = ExecuteForRemoteContextHandlers(listener);

            return listener;
        }
        finally
        {
            _isDisconnectedLock.Release();
            _addContextListenerHandlersLock.Release();
        }
    }

    public IListener OnDisconnect(Action handler)
    {
        try
        {
            _isDisconnectedLock.Wait();
            _disconnectHandlersLock.Wait();

            if (_isDisconnected)
            {
                throw ThrowHelper.PrivateChannelDisconnected(_channelId, _instanceId);
            }

            var listener = new PrivateChannelDisconnectEventListener(
                onDisconnectEventHandler: () => handler(),
                onUnsubscribeHandler: RemoveDisconnectListenerHandler,
                logger: _loggerFactory.CreateLogger<PrivateChannelDisconnectEventListener>());

            _disconnectHandlers.Add(listener);

            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace($"Added disconnect handler on private channel '{_channelId}'.");
            }

            return listener;
        }
        finally
        {
            _isDisconnectedLock.Release();
            _disconnectHandlersLock.Release();
        }
    }

    public IListener OnUnsubscribe(Action<string?> handler)
    {
        try
        {
            _isDisconnectedLock.Wait();
            _unsubscribeHandlersLock.Wait();

            if (_isDisconnected)
            {
                throw ThrowHelper.PrivateChannelDisconnected(_channelId, _instanceId);
            }

            var listener = new PrivateChannelContextListenerEventListener(
                onContext: (contextType) => handler(contextType),
                onUnsubscribe: RemoveUnsubscribeListenerHandler,
                logger: _loggerFactory.CreateLogger<PrivateChannelContextListenerEventListener>());

            _unsubscribeHandlers.Add(listener);

            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace($"Added unsubscribe handler on private channel '{_channelId}'.");
            }

            return listener;
        }
        finally
        {
            _isDisconnectedLock.Release();
            _unsubscribeHandlersLock.Release();
        }
    }

    private async ValueTask ExecuteForRemoteContextHandlers(PrivateChannelContextListenerEventListener listener)
    {
        await _initializationTaskCompletionSource.Task.ConfigureAwait(false);

        try
        {
            var listeners = await _messaging.InvokeServiceAsync(_remoteContextListenersService);

            var remoteListeners = string.IsNullOrEmpty(listeners)
                ? Array.Empty<string>()
                : JsonSerializer.Deserialize<IEnumerable<string>>(listeners!, _jsonSerializerOptions);

            foreach (var contextType in remoteListeners ?? Array.Empty<string>())
            {
                listener.Execute(contextType);

                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    _logger.LogTrace($"Executed remote context handler for context type '{contextType}' on private channel '{_channelId}' for remote listener.");
                }
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, $"Error executing remote context handlers on private channel: {_channelId}, {_instanceId}.");
            return;
        }
    }

    private void RemoveUnsubscribeListenerHandler(PrivateChannelContextListenerEventListener listener)
    {
        try
        {
            _unsubscribeHandlersLock.Wait();

            if (!_unsubscribeHandlers.Remove(listener))
            {
                _logger.LogWarning($"The {nameof(PrivateChannelContextListenerEventListener)} to remove was not found in the list of registered {nameof(PrivateChannelContextListenerEventListener)}.");
            }
        }
        finally
        {
            _unsubscribeHandlersLock.Release();
        }
    }

    private void RemoveDisconnectListenerHandler(PrivateChannelDisconnectEventListener listener)
    {
        try
        {
            _disconnectHandlersLock.Wait();

            if (!_disconnectHandlers.Remove(listener))
            {
                _logger.LogWarning($"The {nameof(PrivateChannelDisconnectEventListener)} to remove was not found in the list of registered {nameof(PrivateChannelDisconnectEventListener)}.");
            }
        }
        finally
        {
            _disconnectHandlersLock.Release();
        }
    }

    private void RemoveAddContextListenerHandler(PrivateChannelContextListenerEventListener privateChannelContextListener)
    {
        try
        {
            _addContextListenerHandlersLock.Wait();

            if (!_addContextListenerHandlers.Remove(privateChannelContextListener))
            {
                _logger.LogWarning($"The {nameof(PrivateChannelContextListenerEventListener)} to remove was not found in the list of registered {nameof(PrivateChannelContextListenerEventListener)}.");
            }
        }
        finally
        {
            _addContextListenerHandlersLock.Release();
        }
    }

    private async ValueTask FireContextHandlerAdded(string? contextType)
    {
        await _initializationTaskCompletionSource.Task.ConfigureAwait(false);

        var request = new PrivateChannelInternalEvents
        {
            ContextType = contextType,
            Event = PrivateChannelInternalEventType.ContextListenerAdded,
            InstanceId = _instanceId
        };

        var serializedRequest = JsonSerializer.Serialize(request, _jsonSerializerOptions);

        await _messaging.PublishAsync(_internalEventsTopic, serializedRequest);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug($"Fired context handler added event for context type '{contextType}' on private channel '{_channelId}'. \nRequest has been sent to backend: {serializedRequest}.");
        }
    }

    private void RemoveContextHandler<T>(ContextListener<T> contextListener) where T : IContext
    {
        try
        {
            _isDisconnectedLock.Wait();
            _contextHandlersLock.Wait();

            if (!_isDisconnected)
            {
                if (_contextHandlers.Remove(contextListener))
                {
                    _logger.LogWarning($"The context listener for context type '{contextListener.ContextType}' was removed from private channel '{_channelId}'.");
                }
            }
            //Fire and forget
            _ = FireUnsubscribed(contextListener.ContextType);
        }
        finally
        {
            _isDisconnectedLock.Release();
            _contextHandlersLock.Release();
        }
    }

    private async ValueTask FireUnsubscribed(string? contextType)
    {
        await _initializationTaskCompletionSource.Task.ConfigureAwait(false);

        var request = new PrivateChannelInternalEvents
        {
            ContextType = contextType,
            Event = PrivateChannelInternalEventType.Unsubscribed,
            InstanceId = _instanceId
        };

        var serializedRequest = JsonSerializer.Serialize(request, _jsonSerializerOptions);

        await _messaging.PublishAsync(_internalEventsTopic, serializedRequest);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug($"Fired unsubscribed event for context type '{contextType}' on private channel '{_channelId}'. \nRequest has been sent to backend: {serializedRequest}.");
        }
    }

    private async ValueTask<string?> HandleRemoteContextListener(string? request)
    {
        await _initializationTaskCompletionSource.Task.ConfigureAwait(false);

        try
        {
            await _contextHandlersLock.WaitAsync().ConfigureAwait(false);

            var contextListeners = _contextHandlers.AsEnumerable();

            return JsonSerializer.Serialize(contextListeners, _jsonSerializerOptions);
        }
        finally
        {
            _contextHandlersLock.Release();
        }
    }

    private async ValueTask HandleInternalEvent(string payload)
    {
        try
        {
            await _isDisconnectedLock.WaitAsync().ConfigureAwait(false);

            if (_isDisconnected
                || string.IsNullOrEmpty(payload))
            {
                return;
            }

            var internalEvent = JsonSerializer.Deserialize<PrivateChannelInternalEvents>(payload, _jsonSerializerOptions);

            if (internalEvent == null)
            {
                _logger.LogWarning($"Received invalid internal event on private channel '{_channelId}': {payload}");
                return;
            }

            if (internalEvent.InstanceId == _instanceId)
            {
                // Ignore events originating from this instance
                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    _logger.LogTrace($"Ignoring internal event from same instance '{_instanceId}' on private channel '{_channelId}'. Event: {payload}");
                }

                return;
            }

            if (internalEvent.Event == PrivateChannelInternalEventType.ContextListenerAdded)
            {
                try
                {
                    await _addContextListenerHandlersLock.WaitAsync().ConfigureAwait(false);

                    foreach (var handler in _addContextListenerHandlers)
                    {
                        handler.Execute(internalEvent.ContextType);
                    }
                }
                finally
                {
                    _addContextListenerHandlersLock.Release();
                }
            }
            else if (internalEvent.Event == PrivateChannelInternalEventType.Unsubscribed)
            {
                try
                {
                    await _unsubscribeHandlersLock.WaitAsync().ConfigureAwait(false);
                    foreach (var handler in _unsubscribeHandlers)
                    {
                        handler.Execute(internalEvent.ContextType);
                    }
                }
                finally
                {
                    _unsubscribeHandlersLock.Release();
                }
            }
            else if (internalEvent.Event == PrivateChannelInternalEventType.Disconnected)
            {
                try
                {
                    await _disconnectHandlersLock.WaitAsync().ConfigureAwait(false);
                    foreach (var handler in _disconnectHandlers)
                    {
                        handler.Execute();
                    }
                }
                finally
                {
                    _disconnectHandlersLock.Release();
                }
            }
        }
        finally
        {
            _isDisconnectedLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _initializationTaskCompletionSource.Task.ConfigureAwait(false);

        Disconnect();
        await _serviceRegistration.DisposeAsync().ConfigureAwait(false);
        await _subscription.DisposeAsync().ConfigureAwait(false);
    }
}
