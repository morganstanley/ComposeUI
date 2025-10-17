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
using System.Text.Json.Nodes;
using Finos.Fdc3;
using Finos.Fdc3.Context;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Contracts;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Exceptions;
using MorganStanley.ComposeUI.Messaging.Abstractions;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Infrastructure.Internal;

internal class ContextListener<T> : IListener, IAsyncDisposable
    where T : IContext
{
    private readonly string _instanceId;
    private readonly ContextHandler<T> _contextHandler;
    private readonly string? _contextType;
    private readonly IMessaging _messaging;
    private readonly ILogger<ContextListener<T>> _logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions = SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization;

    private readonly SemaphoreSlim _serializedContextsLock = new(1, 1);
    private readonly List<IContext> _serializedContexts = new();

    private readonly SemaphoreSlim _subscriptionLock = new(1,1);
    private bool _isSubscribed = false;
    private IAsyncDisposable? _subscription;

    private string _contextListenerId;

    public string? ContextType => _contextType;

    public ContextListener(
        string instanceId,
        ContextHandler<T> contextHandler,
        IMessaging messaging,
        string? contextType = null,
        ILogger<ContextListener<T>>? logger = null)
    {
        _instanceId = instanceId ?? throw new ArgumentNullException(nameof(instanceId));
        _contextHandler = contextHandler;
        _contextType = contextType;
        _messaging = messaging;
        _logger = logger ?? NullLogger<ContextListener<T>>.Instance;
    }

    public ValueTask DisposeAsync()
    {
        Unsubscribe();
        return new ValueTask();
    }

    public void Unsubscribe()
    {
        try
        {
            _subscriptionLock.Wait();
            if (!_isSubscribed)
            {
                return;
            }

            UnregisterContextListenerAsync()
                .AsTask()
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();

            _subscription?.DisposeAsync()
                .AsTask()
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();

            _isSubscribed = false;
        }
        finally
        {
            _subscriptionLock.Release();
        }
    }

    public async ValueTask SubscribeAsync(string channelId, ChannelType channelType, CancellationToken cancellationToken = default)
    {
        try
        {
            await _subscriptionLock.WaitAsync().ConfigureAwait(false);
            await _serializedContextsLock.WaitAsync().ConfigureAwait(false);

            if (_isSubscribed)
            {
                return;
            }

            await RegisterContextListenerAsync(channelId, channelType).ConfigureAwait(false);
            var topic = new ChannelTopics(channelId, channelType);

            _subscription = await _messaging.SubscribeAsync(
                topic.Broadcast,
                serializedContext =>
                {
                    //Messaging implementation by default should not pass the message back to the sender if that is subscribed to the same topic.
                    if (string.IsNullOrEmpty(serializedContext))
                    {
                        _logger.LogWarning($"Null context was received: {serializedContext}...");
                        return new ValueTask();
                    }

                    var contextType = (string?)JsonNode.Parse(serializedContext, new JsonNodeOptions { PropertyNameCaseInsensitive = true })?["type"];

                    if (contextType != null && contextType != _contextType && !string.IsNullOrEmpty(_contextType))
                    {
                        return new ValueTask();
                    }

                    var context = JsonSerializer.Deserialize<T>(serializedContext!, _jsonSerializerOptions);

                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug($"Context received: {serializedContext}; Type to deserialize to: {typeof(T)}...");
                    }

                    _contextHandler(context!);
                    _serializedContexts.Add(context!);

                    return new ValueTask();
                }, cancellationToken);

            _isSubscribed = true;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, $"Subscription was not successful.");
            throw exception;
        }
        finally
        {
            _subscriptionLock.Release();
            _serializedContextsLock.Release();

            _logger.LogInformation($"Context listener subscribed to channel {channelId} for context type {_contextType}.");
        }
    }

    public async Task HandleContextAsync(IContext context)
    {
        try
        {
            await _subscriptionLock.WaitAsync().ConfigureAwait(false);

            if (!_isSubscribed)
            {
                throw new InvalidOperationException("The context listener is not subscribed to any channel.");
            }

            if (context.Type != _contextType && !string.IsNullOrEmpty(_contextType))
            {
                _logger.LogWarning($"The context type: {context.Type} does not match the registered context type: {_contextType}...");
                return;
            }

            if (context is not T typedContext)
            {
                _logger.LogWarning($"The context type: {context.GetType()?.FullName} is not compatible with the expected type: {typeof(T).FullName}...");
                return;
            }

            _contextHandler((T) context);
        }
        finally
        {
            _subscriptionLock.Release();
        }
    }

    private async ValueTask RegisterContextListenerAsync(
        string channelId,
        ChannelType channelType)
    {
        var request = new AddContextListenerRequest
        {
            ChannelId = channelId,
            ChannelType = channelType,
            ContextType = _contextType,
            Fdc3InstanceId = _instanceId
        };

        var response = await _messaging.InvokeJsonServiceAsync<AddContextListenerRequest, AddContextListenerResponse>(Fdc3Topic.AddContextListener, request, _jsonSerializerOptions);

        if (response == null)
        {
            throw ThrowHelper.MissingResponse();
        }

        if (response.Error != null)
        {
            throw ThrowHelper.ErrorResponseReceived(response.Error);
        }
        else if (!response.Success)
        {
            throw ThrowHelper.UnsuccessfulSubscription(request);
        }
        else if (string.IsNullOrEmpty(response.Id))
        {
            throw ThrowHelper.MissingSubscriptionId();
        }
        
        _contextListenerId = response.Id!;
    }

    private async ValueTask UnregisterContextListenerAsync()
    {
        var request = new RemoveContextListenerRequest
        {
            ListenerId = _contextListenerId,
            Fdc3InstanceId = _instanceId,
            ContextType = _contextType
        };

        var response = await _messaging.InvokeJsonServiceAsync<RemoveContextListenerRequest, RemoveContextListenerResponse>(Fdc3Topic.RemoveContextListener, request, _jsonSerializerOptions);

        if (response == null)
        {
            throw ThrowHelper.MissingResponse();
        }

        if (!string.IsNullOrEmpty(response.Error))
        {
            throw ThrowHelper.ErrorResponseReceived(response.Error);
        }

        if (!response.Success)
        {
            throw ThrowHelper.UnsuccessfulSubscriptionUnRegistration(request);
        }

        _contextListenerId = null;
    }
}
