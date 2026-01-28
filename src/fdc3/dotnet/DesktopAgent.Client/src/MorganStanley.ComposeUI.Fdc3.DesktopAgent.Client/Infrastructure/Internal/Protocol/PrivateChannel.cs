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
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Contracts;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Exceptions;
using MorganStanley.ComposeUI.Messaging.Abstractions;
using DisplayMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.DisplayMetadata;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Infrastructure.Internal.Protocol;

internal class PrivateChannel : Channel, IPrivateChannel, IAsyncDisposable
{
    private readonly ILogger<PrivateChannel> _logger;
    private readonly PrivateChannelTopics _privateChannelTopics;
    private readonly Action _onDisconnect;
    private readonly string _internalEventsTopic;
    private readonly bool _isOriginalCreator;
    private readonly string _remoteContextListenersService;
    private readonly TaskCompletionSource<string> _initializationTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly List<IListener> _contextHandlers = new();
    private readonly List<PrivateChannelContextListenerEventListener> _addContextListenerHandlers = new();
    private readonly List<PrivateChannelContextListenerEventListener> _unsubscribeHandlers = new();
    private readonly List<PrivateChannelDisconnectEventListener> _disconnectHandlers = new();

    private readonly JsonSerializerOptions _jsonSerializerOptions = SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization;

    private IAsyncDisposable _subscription;
    private IAsyncDisposable _serviceRegistration;
    private bool _isDisconnected = false;

    public PrivateChannel(
        string channelId, 
        IMessaging messaging, 
        string instanceId,
        bool isOriginalCreator,
        Action onDisconnect,
        DisplayMetadata? displayMetadata = null,
        ILoggerFactory? loggerFactory = null) 
        : base(channelId, ChannelType.Private, messaging, instanceId, null, displayMetadata, loggerFactory)
    {
        _privateChannelTopics = Fdc3Topic.PrivateChannel(channelId);
        _internalEventsTopic = _privateChannelTopics.Events;
        _isOriginalCreator = isOriginalCreator;
        _remoteContextListenersService = _privateChannelTopics.GetContextHandlers(!isOriginalCreator);
        _onDisconnect = onDisconnect;
        _logger = LoggerFactory.CreateLogger<PrivateChannel>();

        Task.Run(() => InitializeAsync());
    }

    private async ValueTask InitializeAsync()
    {
        try
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Subscribing to private channel internal events for channel {ChannelId}, instance {InstanceId}, topic: {Topic}.", Id, InstanceId, _internalEventsTopic);
            }

            _subscription = await Messaging.SubscribeAsync(_internalEventsTopic, HandleInternalEvent).ConfigureAwait(false);


            var topic = _privateChannelTopics.GetContextHandlers(_isOriginalCreator);
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Registering remote private channel context handlers service for channel {ChannelId}, instance {InstanceId}, service: {Service}.", Id, InstanceId, topic);
            }

            _serviceRegistration = await Messaging.RegisterServiceAsync(topic, HandleRemoteContextListener).ConfigureAwait(false);

            _initializationTaskCompletionSource.SetResult(Id);
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
            await _lock.WaitAsync().ConfigureAwait(false);

            if (_isDisconnected)
            {
                throw ThrowHelper.PrivateChannelDisconnected(Id, InstanceId);
            }

            await base.Broadcast(context);
        }
        finally
        {
            _lock.Release();
        }
    }

    public override async Task<IListener> AddContextListener<T>(string? contextType, ContextHandler<T> handler)
    {
        await _initializationTaskCompletionSource.Task.ConfigureAwait(false);

        try
        {
            await _lock.WaitAsync().ConfigureAwait(false);

            if (_isDisconnected)
            {
                throw ThrowHelper.PrivateChannelDisconnected(Id, InstanceId);
            }

            var listener = await base.AddContextListener(contextType, handler).ConfigureAwait(false) as ContextListener<T>;

            if (listener != null)
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Setting unsubscribe callback for {ListenerType} when adding the listener on the {ChannelType}...", nameof(ContextListener<T>), nameof(PrivateChannel));
                }

                listener.SetUnsubscribeCallback(contextListener => RemoveContextHandler(contextListener));

                _contextHandlers.Add(listener);
                
                _ = FireContextHandlerAdded(contextType);

                return listener;
            }

            throw ThrowHelper.PrivateChannelSubscribeFailure(contextType, Id, InstanceId);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async void Disconnect()
    {
        await _initializationTaskCompletionSource.Task.ConfigureAwait(false);

        try
        {
            await _lock.WaitAsync().ConfigureAwait(false);

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
                    _logger.LogTrace("Unsubscribed add context listener handler on private channel {ChannelId}.", Id);
                }
            }

            foreach (var handler in _unsubscribeHandlers)
            {
                handler.UnsubscribeCore(false);

                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    _logger.LogTrace("Unsubscribed unsubscribe handler on private channel {ChannelId}.", Id);
                }
            }

            foreach (var handler in _disconnectHandlers)
            {
                handler.UnsubscribeCore(false);

                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    _logger.LogTrace("Unsubscribed disconnect handler on private channel {ChannelId}.", Id);
                }
            }

            _addContextListenerHandlers.Clear();
            _unsubscribeHandlers.Clear();
            _disconnectHandlers.Clear();

            foreach (var listener in _contextHandlers)
            {
                try
                {
                    //Fire and forget
                    listener.Unsubscribe();

                    if (_logger.IsEnabled(LogLevel.Trace))
                    {
                        _logger.LogTrace("Unsubscribed context listener on private channel {ChannelId}.", Id);
                    }
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception, "Error unsubscribing context listener on private channel {ChannelId}.", Id);
                }
            }

            var request = PrivateChannelInternalEvents.Disconnected(InstanceId);
            var serializedRequest = JsonSerializer.Serialize(request, _jsonSerializerOptions);

            await Messaging.PublishAsync(_internalEventsTopic, serializedRequest).ConfigureAwait(false);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Private channel {ChannelId} disconnected. Request: {Request} has been sent to the backend...", Id, serializedRequest);
            }

            _onDisconnect();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error during disconnecting private channel {ChannelId}, instance {InstanceId}.", Id, InstanceId);
        }
        finally
        {
            _lock.Release();
        }
    }

    public IListener OnAddContextListener(Action<string?> handler)
    {
        try
        {
            _lock.Wait();

            if (_isDisconnected)
            {
                throw ThrowHelper.PrivateChannelDisconnected(Id, InstanceId);
            }

            var listener = new PrivateChannelContextListenerEventListener(
                onContext: (contextType) => handler(contextType),
                onUnsubscribe: RemoveAddContextListenerHandler,
                logger: LoggerFactory.CreateLogger<PrivateChannelContextListenerEventListener>());

            _addContextListenerHandlers.Add(listener);

            //Fire and forget
            _ = ExecuteForRemoteContextHandlers(listener);

            return listener;
        }
        finally
        {
            _lock.Release();
        }
    }

    public IListener OnDisconnect(Action handler)
    {
        try
        {
            _lock.Wait();

            if (_isDisconnected)
            {
                throw ThrowHelper.PrivateChannelDisconnected(Id, InstanceId);
            }

            var listener = new PrivateChannelDisconnectEventListener(
                onDisconnectEventHandler: () => handler(),
                onUnsubscribeHandler: RemoveDisconnectListenerHandler,
                logger: LoggerFactory.CreateLogger<PrivateChannelDisconnectEventListener>());

            _disconnectHandlers.Add(listener);

            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("Added disconnect handler on private channel {ChannelId}.", Id);
            }

            return listener;
        }
        finally
        {
            _lock.Release();
        }
    }

    public IListener OnUnsubscribe(Action<string?> handler)
    {
        try
        {
            _lock.Wait();

            if (_isDisconnected)
            {
                throw ThrowHelper.PrivateChannelDisconnected(Id, InstanceId);
            }

            var listener = new PrivateChannelContextListenerEventListener(
                onContext: (contextType) => handler(contextType),
                onUnsubscribe: RemoveUnsubscribeListenerHandler,
                logger: LoggerFactory.CreateLogger<PrivateChannelContextListenerEventListener>());

            _unsubscribeHandlers.Add(listener);

            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("Added unsubscribe handler on private channel {ChannelId}.", Id);
            }

            return listener;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async ValueTask ExecuteForRemoteContextHandlers(PrivateChannelContextListenerEventListener listener)
    {
        await _initializationTaskCompletionSource.Task.ConfigureAwait(false);

        try
        {
            var listeners = await Messaging.InvokeServiceAsync(_remoteContextListenersService);

            var remoteListeners = string.IsNullOrEmpty(listeners)
                ? Array.Empty<string>()
                : JsonSerializer.Deserialize<IEnumerable<string>>(listeners!, _jsonSerializerOptions);

            foreach (var contextType in remoteListeners ?? Array.Empty<string>())
            {
                listener.Execute(contextType);

                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    _logger.LogTrace("Executed remote context handler for context type {ContextType} on private channel {ChannelId} for remote listener.", contextType, Id);
                }
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error executing remote context handlers on private channel: {ChannelId}, {InstanceId}.", Id, InstanceId);
            return;
        }
    }

    private void RemoveUnsubscribeListenerHandler(PrivateChannelContextListenerEventListener listener)
    {
        try
        {
            _lock.Wait();

            if (!_unsubscribeHandlers.Remove(listener))
            {
                _logger.LogWarning("The {ListenerType} to remove was not found in the list of registered {ContextListenerType}.", nameof(PrivateChannelContextListenerEventListener), nameof(PrivateChannelContextListenerEventListener));
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private void RemoveDisconnectListenerHandler(PrivateChannelDisconnectEventListener listener)
    {
        try
        {
            _lock.Wait();

            if (!_disconnectHandlers.Remove(listener))
            {
                _logger.LogWarning("The {ListenerType} to remove was not found in the list of registered {DisconnectListenerType}.", nameof(PrivateChannelDisconnectEventListener), nameof(PrivateChannelDisconnectEventListener));
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private void RemoveAddContextListenerHandler(PrivateChannelContextListenerEventListener privateChannelContextListener)
    {
        try
        {
            _lock.Wait();

            if (!_addContextListenerHandlers.Remove(privateChannelContextListener))
            {
                _logger.LogWarning("The {ListenerType} to remove was not found in the list of registered {ContextListenerType}.", nameof(PrivateChannelContextListenerEventListener), nameof(PrivateChannelContextListenerEventListener));
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private async ValueTask FireContextHandlerAdded(string? contextType)
    {
        await _initializationTaskCompletionSource.Task.ConfigureAwait(false);

        var request = new PrivateChannelInternalEvents
        {
            ContextType = contextType,
            Event = PrivateChannelInternalEventType.ContextListenerAdded,
            InstanceId = InstanceId
        };

        var serializedRequest = JsonSerializer.Serialize(request, _jsonSerializerOptions);

        await Messaging.PublishAsync(_internalEventsTopic, serializedRequest).ConfigureAwait(false);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Fired context handler added event for context type {ContextType} on private channel {ChannelId}. Request has been sent to backend: {Request}.", contextType, Id, serializedRequest);
        }
    }

    private void RemoveContextHandler<T>(ContextListener<T> contextListener) where T : IContext
    {
        try
        {
            _lock.Wait();

            if (!_isDisconnected)
            {
                if (_contextHandlers.Remove(contextListener))
                {
                    _logger.LogWarning("The context listener for context type {ContextType} was removed from private channel {ChannelId}.", contextListener.ContextType, Id);
                }
            }
            //Fire and forget
            _ = FireUnsubscribed(contextListener.ContextType);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async ValueTask FireUnsubscribed(string? contextType)
    {
        await _initializationTaskCompletionSource.Task.ConfigureAwait(false);

        var request = new PrivateChannelInternalEvents
        {
            ContextType = contextType,
            Event = PrivateChannelInternalEventType.Unsubscribed,
            InstanceId = InstanceId
        };

        var serializedRequest = JsonSerializer.Serialize(request, _jsonSerializerOptions);

        await Messaging.PublishAsync(_internalEventsTopic, serializedRequest).ConfigureAwait(false);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Fired unsubscribed event for context type {ContextType} on private channel {ChannelId}. Request has been sent to backend: {Request}.", contextType, Id, serializedRequest);
        }
    }

    private async ValueTask<string?> HandleRemoteContextListener(string? request)
    {
        await _initializationTaskCompletionSource.Task.ConfigureAwait(false);

        try
        {
            await _lock.WaitAsync().ConfigureAwait(false);

            var contextListeners = _contextHandlers.AsEnumerable();

            return JsonSerializer.Serialize(contextListeners, _jsonSerializerOptions);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async ValueTask HandleInternalEvent(string payload)
    {
        try
        {
            await _lock.WaitAsync().ConfigureAwait(false);

            if (_isDisconnected
                || string.IsNullOrEmpty(payload))
            {
                return;
            }

            var internalEvent = JsonSerializer.Deserialize<PrivateChannelInternalEvents>(payload, _jsonSerializerOptions);

            if (internalEvent == null)
            {
                _logger.LogWarning("Received invalid internal event on private channel {ChannelId}: {Payload}", Id, payload);
                return;
            }

            if (internalEvent.InstanceId == InstanceId)
            {
                // Ignore events originating from this instance
                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    _logger.LogTrace("Ignoring internal event from same instance {InstanceId} on private channel {ChannelId}. Event: {Payload}", InstanceId, Id, payload);
                }

                return;
            }

            if (internalEvent.Event == PrivateChannelInternalEventType.ContextListenerAdded)
            {
                foreach (var handler in _addContextListenerHandlers)
                {
                    handler.Execute(internalEvent.ContextType);
                }
            }
            else if (internalEvent.Event == PrivateChannelInternalEventType.Unsubscribed)
            {
                foreach (var handler in _unsubscribeHandlers)
                {
                    handler.Execute(internalEvent.ContextType);
                }
            }
            else if (internalEvent.Event == PrivateChannelInternalEventType.Disconnected)
            {
                foreach (var handler in _disconnectHandlers)
                {
                    handler.Execute();
                }
            }
        }
        finally
        {
            _lock.Release();
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
