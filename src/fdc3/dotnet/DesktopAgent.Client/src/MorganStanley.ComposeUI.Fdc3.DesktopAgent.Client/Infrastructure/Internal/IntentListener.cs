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

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Infrastructure.Internal;

internal class IntentListener<T> : IListener
    where T : IContext
{
    private readonly IMessaging _messaging;
    private readonly string _intent;
    private readonly string _instanceId;
    private readonly IntentHandler<T> _intentHandler;
    private readonly ILogger<IntentListener<T>> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _isRegistered = false;
    private IAsyncDisposable _subscription;
    private readonly JsonSerializerOptions _jsonSerializerOptions = SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization;

    public IntentListener(
        IMessaging messaging,
        string intent,
        string instanceId,
        IntentHandler<T> handler,
        ILogger<IntentListener<T>>? logger = null)
    {
        _messaging = messaging;
        _intent = intent;
        _instanceId = instanceId;
        _intentHandler = handler;
        _logger = logger ?? NullLogger<IntentListener<T>>.Instance;
    }

    public async ValueTask RegisterIntentHandlerAsync()
    {
        try
        {
            await _semaphore.WaitAsync();
            if (_isRegistered)
            {
                return;
            }

            var topic = Fdc3Topic.RaiseIntentResolution(_intent, _instanceId);

            _subscription = await _messaging.SubscribeJsonAsync<RaiseIntentResolutionRequest>(
                topic,
                HandleIntentMessageAsync,
                _jsonSerializerOptions);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("{NameOfIntentListener} is subscribed for {Intent} with instance id: {InstanceId} to topic: {Topic}.", nameof(IntentListener<T>), _intent, _instanceId, topic);
            }

            _isRegistered = true;
        }
        finally
        {
            _semaphore.Release();
        }        
    }

    public void Unsubscribe()
    {
        try
        {
            _semaphore.Wait();

            var request = CreateUnsubscribeRequest();
            LogDebug("Unsubscribing intent listener for intent {Intent} and instanceId {InstanceId}...", _intent, _instanceId);

            var response = _messaging.InvokeJsonServiceAsync<IntentListenerRequest, IntentListenerResponse>(
                Fdc3Topic.AddIntentListener,
                request,
                _jsonSerializerOptions).GetAwaiter().GetResult();

            ValidateUnsubscribeResponse(response);

            _subscription.DisposeAsync().GetAwaiter().GetResult();

            LogDebug("Successfully unsubscribed intent listener for intent {Intent} and instanceId {InstanceId}.", _intent, _instanceId);
            _isRegistered = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while unsubscribing intent listener for intent {Intent} and instanceId {InstanceId}.", _intent, _instanceId);
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private IntentListenerRequest CreateUnsubscribeRequest() =>
        new IntentListenerRequest
        {
            Intent = _intent,
            Fdc3InstanceId = _instanceId,
            State = SubscribeState.Unsubscribe
        };

    private void ValidateUnsubscribeResponse(IntentListenerResponse? response)
    {
        if (response == null)
        {
            throw ThrowHelper.MissingResponse();
        }

        if (!string.IsNullOrEmpty(response.Error))
        {
            throw ThrowHelper.ErrorResponseReceived(response.Error);
        }

        if (response.Stored)
        {
            throw ThrowHelper.ListenerNotUnRegistered(_intent, _instanceId);
        }
    }

    private async Task SetRequestResultAsync(object? intentResult, StoreIntentResultRequest request, string messageId)
    {
        try
        {
            if (intentResult == null) //It is a simply void
            {
                LogDebug("The intent result is void for intent:{Intent} for message: {MessageId}.", _intent, messageId);
                request.VoidResult = true;
            }
            else if (intentResult is Task<IIntentResult> resolvableTask) //It is a task with some return type
            {
                var resolvedIntentResult = await resolvableTask;

                if (resolvedIntentResult is IChannel channel)
                {
                    LogDebug("The intent result is a channel for intent:{Intent} for message: {MessageId}. Channel: {Channel}", _intent, messageId, Serialize(channel));
                    request.ChannelId = channel.Id;
                    request.ChannelType = channel.Type;
                }
                else if (resolvedIntentResult is IContext ctx)
                {
                    var context = Serialize(ctx);
                    LogDebug("The intent result is a context for intent:{Intent} for message: {MessageId}. Context: {Context}", _intent, messageId, context);
                    request.Context = context;
                }
                else // it is a resolvable task with no return type
                {
                    LogDebug("The intent result is void for intent:{Intent} for message: {MessageId}.", _intent, messageId);
                    request.VoidResult = true;
                }
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error while resolving the intent...");
        }
    }

    private async ValueTask HandleIntentMessageAsync(RaiseIntentResolutionRequest? receivedRequest)
    {
        if (receivedRequest == null)
        {
            LogError("Received null or invalid intent invocation request for intent {Intent}.", _intent);
            throw ThrowHelper.MissingResponse();
        }

        var request = new StoreIntentResultRequest
        {
            MessageId = receivedRequest.MessageId,
            Intent = _intent,
            OriginFdc3InstanceId = _instanceId,
            TargetFdc3InstanceId = receivedRequest.ContextMetadata.Source!.InstanceId!, //This should be defined
        };

        try
        {
            var context = Deserialize<T>(receivedRequest.Context);
            if (context == null)
            {
                LogError("Received null or invalid context for intent {Intent}. Context: {Context}...", _intent, receivedRequest.Context);
                throw ThrowHelper.MissingContext();
            }

            if (ContextTypes.GetType(context.Type) != typeof(T))
            {
                request.Error = ResolveError.IntentDeliveryFailed;

                LogDebug("The context type {ContextType} does not match the expected type {ExpectedType} for intent {Intent}. Context: {Context}...", context.Type, typeof(T).Name, _intent, Serialize(context));
            }
            else
            {
                var intentResult = _intentHandler(context, receivedRequest.ContextMetadata);

                LogDebug("Resolved intent {Intent} with result: {Result}...", _intent, intentResult);

                await SetRequestResultAsync(intentResult, request, receivedRequest.MessageId);
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error while resolving the received intent: {Intent}.", _intent);
            request.Error = ResultError.IntentHandlerRejected;
        }

        var response = await _messaging.InvokeJsonServiceAsync<StoreIntentResultRequest, StoreIntentResultResponse>(
            Fdc3Topic.SendIntentResult,
            request,
            _jsonSerializerOptions);

        if (response == null)
        {
            LogError("Received null or invalid response when storing the intent result for intent {Intent}. Request: {Request}...", _intent, Serialize(request));
            throw ThrowHelper.MissingResponse();
        }

        if (!string.IsNullOrEmpty(response.Error))
        {
            LogError("Received error response when storing the intent result for intent {Intent}. Request: {Request}, Response: {Response}...", _intent, Serialize(request), Serialize(response));
            throw ThrowHelper.ErrorResponseReceived(response.Error);
        }

        if (!response.Stored)
        {
            throw ThrowHelper.IntentResultStoreFailed(_intent, _instanceId);
        }
    }

    private TType? Deserialize<TType>(string json) => JsonSerializer.Deserialize<TType>(json, _jsonSerializerOptions);

    private string Serialize<TType>(TType obj) => JsonSerializer.Serialize(obj, _jsonSerializerOptions);

    private void LogDebug(string message, params object[] args)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(message, args);
        }
    }

    private void LogError(string message, params object[] args)
    {
        _logger.LogError(message, args);
    }
}
