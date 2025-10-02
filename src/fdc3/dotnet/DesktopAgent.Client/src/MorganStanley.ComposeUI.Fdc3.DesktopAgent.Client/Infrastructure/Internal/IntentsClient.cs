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
using IntentResolution = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Infrastructure.Internal.Protocol.IntentResolution;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Infrastructure.Internal;

internal class IntentsClient : IIntentsClient
{
    private readonly IMessaging _messaging;
    private readonly IChannelFactory _channelFactory;
    private readonly string _instanceId;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<IntentsClient> _logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions = SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization;

    public IntentsClient(
        IMessaging messaging,
        IChannelFactory channelFactory,
        string instanceId,
        ILoggerFactory? loggerFactory = null)
    {
        _messaging = messaging;
        _channelFactory = channelFactory;
        _instanceId = instanceId;
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _logger = _loggerFactory.CreateLogger<IntentsClient>();
    }

    public async ValueTask<IListener> AddIntentListenerAsync<T>(string intent, IntentHandler<T> handler) where T : IContext
    {
        var listener = new IntentListener<T>(
            _messaging,
            intent,
            _instanceId,
            handler,
            _loggerFactory.CreateLogger<IntentListener<T>>());

        await listener.RegisterIntentHandlerAsync();

        var request = new IntentListenerRequest
        {
            Fdc3InstanceId = _instanceId,
            Intent = intent,
            State = SubscribeState.Subscribe
        };

        var response = await _messaging.InvokeJsonServiceAsync<IntentListenerRequest, IntentListenerResponse>(
            Fdc3Topic.AddIntentListener,
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

        if (!response.Stored)
        {
            throw ThrowHelper.ListenerNotRegistered(intent, _instanceId);
        }

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug($"Listener is registered...");
        }

        return listener;
    }

    public async ValueTask<IAppIntent> FindIntentAsync(string intent, IContext? context = null, string? resultType = null)
    {
        var request = new FindIntentRequest
        {
            Fdc3InstanceId = _instanceId,
            Intent = intent,
            Context = context?.Type,
            ResultType = resultType
        };

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Finding intent. Intent: {Intent}, Context Type: {ContextType}, Result Type: {ResultType}", intent, context?.Type, resultType);
        }

        var response = await _messaging.InvokeJsonServiceAsync<FindIntentRequest, FindIntentResponse>(
            Fdc3Topic.FindIntent,
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

        if (response.AppIntent == null)
        {
            throw ThrowHelper.AppIntentIsNotDefined(intent, context?.Type, resultType);
        }

        return response.AppIntent;
    }

    public async ValueTask<IEnumerable<IAppIntent>> FindIntentsByContextAsync(IContext context, string? resultType = null)
    {
        var request = new FindIntentsByContextRequest
        {
            Fdc3InstanceId = _instanceId,
            Context = JsonSerializer.Serialize(context, _jsonSerializerOptions),
            ResultType = resultType
        };

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Finding intents by context. Context Type: {ContextType}, Result Type: {ResultType}", context.Type, resultType);
        }

        var response = await _messaging.InvokeJsonServiceAsync<FindIntentsByContextRequest, FindIntentsByContextResponse>(
            Fdc3Topic.FindIntentsByContext,
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

        if (response.AppIntents == null)
        {
            throw ThrowHelper.AppIntentIsNotDefined("null", context.Type, resultType);
        }

        return response.AppIntents;
    }

    public async ValueTask<IIntentResolution> RaiseIntentForContextAsync(IContext context, IAppIdentifier? app)
    {
        var messageId = new Random().Next(100000);

        var request = new RaiseIntentForContextRequest
        {
            Fdc3InstanceId = _instanceId,
            Context = JsonSerializer.Serialize(context, _jsonSerializerOptions),
            MessageId = messageId
        };

        if (app != null)
        {
            request.TargetAppIdentifier = new AppIdentifier
            {
                AppId = app.AppId,
                InstanceId = app.InstanceId
            };
        }

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug($"Request is created: {JsonSerializer.Serialize(request, _jsonSerializerOptions)}");
        }

        var response = await _messaging.InvokeJsonServiceAsync<RaiseIntentForContextRequest, RaiseIntentResponse>(
            Fdc3Topic.RaiseIntentForContext,
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

        if (string.IsNullOrEmpty(response.MessageId)
            || string.IsNullOrEmpty(response.Intent)
            || response.AppMetadata == null)
        {
            throw ThrowHelper.IntentResolutionIsNotDefined(context.Type, app?.AppId, app?.InstanceId);
        }

        var intentResolution = new IntentResolution(
            response.MessageId!,
            _messaging,
            _channelFactory,
            response.Intent!,
            response.AppMetadata!,
            _loggerFactory.CreateLogger<IntentResolution>());

        return intentResolution;
    }
}
