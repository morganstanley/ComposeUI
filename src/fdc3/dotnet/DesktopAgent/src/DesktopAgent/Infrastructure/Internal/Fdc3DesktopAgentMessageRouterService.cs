// Morgan Stanley makes this available to you under the Apache License,
// Version 2.0 (the "License"). You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0.
// 
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership. Unless required by applicable law or agreed
// to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
// or implied. See the License for the specific language governing permissions
// and limitations under the License.

using System.Reactive.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Converters;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.DependencyInjection;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Exceptions;
using MorganStanley.ComposeUI.Messaging;
using MorganStanley.Fdc3;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure.Internal;

internal class Fdc3DesktopAgentMessageRouterService : IHostedService
{
    private readonly IMessageRouter _messageRouter;
    private readonly IFdc3DesktopAgentBridge _desktopAgent;
    private readonly Fdc3DesktopAgentOptions _options;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<Fdc3DesktopAgentMessageRouterService> _logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new AppMetadataJsonConverter(),
            new IntentMetadataJsonConverter(),
            new AppIntentJsonConverter(),
            new IconJsonConverter(),
            new ImageJsonConverter(),
            new IntentMetadataJsonConverter(),
        }
    };

    public JsonSerializerOptions JsonMessageSerializerOptions => new(_jsonSerializerOptions);

    public Fdc3DesktopAgentMessageRouterService(
        IMessageRouter messageRouter,
        IFdc3DesktopAgentBridge desktopAgent,
        IOptions<Fdc3DesktopAgentOptions> options,
        ILoggerFactory? loggerFactory = null)
    {
        _messageRouter = messageRouter;
        _desktopAgent = desktopAgent;
        _options = options.Value;
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _logger = _loggerFactory.CreateLogger<Fdc3DesktopAgentMessageRouterService>() ?? NullLogger<Fdc3DesktopAgentMessageRouterService>.Instance;
    }

    public async ValueTask<UserChannel> HandleAddUserChannel(string id)
    {
        var userChannel = new UserChannel(id, _messageRouter, _loggerFactory.CreateLogger<UserChannel>());
        await _desktopAgent.AddUserChannel(userChannel);
        return userChannel;
    }

    internal ValueTask<FindChannelResponse?> HandleFindChannel(FindChannelRequest? request, MessageContext context)
    {
        return ValueTask.FromResult<FindChannelResponse?>(
                _desktopAgent.FindChannel(request!.ChannelId, request!.ChannelType)
                    ? FindChannelResponse.Success
                    : FindChannelResponse.Failure(ChannelError.NoChannelFound));
    }

    internal async ValueTask<FindIntentResponse?> HandleFindIntent(FindIntentRequest? request, MessageContext context)
    {
        return await _desktopAgent.FindIntent(request);
    }

    internal async ValueTask<FindIntentsByContextResponse?> HandleFindIntentsByContext(FindIntentsByContextRequest? request, MessageContext context)
    {
        return await _desktopAgent.FindIntentsByContext(request);
    }

    internal async ValueTask<RaiseIntentResponse?> HandleRaiseIntent(RaiseIntentRequest? request, MessageContext context)
    {
        try
        {
            var result = await _desktopAgent.RaiseIntent(request);
            if (result.RaiseIntentResolutionMessages.Any())
            {
                foreach (var message in result.RaiseIntentResolutionMessages)
                {
                    await _messageRouter.PublishAsync(
                        Fdc3Topic.RaiseIntentResolution(message.Intent, message.TargetModuleInstanceId),
                        MessageBuffer.Factory.CreateJson(message.Request, _jsonSerializerOptions));
                }
            }

            return result.Response;
        }
        catch (Fdc3DesktopAgentException)
        {
            throw;
        }
    }

    internal async ValueTask<IntentListenerResponse?> HandleAddIntentListener(IntentListenerRequest? request, MessageContext context)
    {
        var result = await _desktopAgent.AddIntentListener(request);

        if (result.RaiseIntentResolutionMessages.Any())
        {
            foreach (var message in result.RaiseIntentResolutionMessages)
            {
                await _messageRouter.PublishAsync(
                    Fdc3Topic.RaiseIntentResolution(message.Intent, message.TargetModuleInstanceId),
                    MessageBuffer.Factory.CreateJson(message.Request, _jsonSerializerOptions));
            }
        }

        return result.Response;
    }

    internal async ValueTask<StoreIntentResultResponse?> HandleStoreIntentResult(StoreIntentResultRequest? request, MessageContext context)
    {
        return await _desktopAgent.StoreIntentResult(request);
    }

    internal async ValueTask<GetIntentResultResponse?> HandleGetIntentResult(GetIntentResultRequest? request, MessageContext context)
    {
        return await _desktopAgent.GetIntentResult(request);
    }

    private async ValueTask SafeWaitAsync(IEnumerable<ValueTask> tasks)
    {
        foreach (var task in tasks)
        {
            try
            {
                await task;
            }
            catch (Exception exception)
            {
                _logger.LogError($"An exception was thrown while waiting for a teask to finish. Exception: {exception}");
            }
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        async Task RegisterHandler<TRequest, TResponse>(string topic, Func<TRequest?, MessageContext, ValueTask<TResponse?>> handler) where TRequest : class
        {
            await _messageRouter.RegisterServiceAsync(topic,
                async (endpoint, payload, context) =>
                {
                    var request = payload?.ReadJson<TRequest>(_jsonSerializerOptions);
                    var response = await handler(request, context);
                    return response is null ? null : MessageBuffer.Factory.CreateJson(response, _jsonSerializerOptions);
                }, cancellationToken: cancellationToken);
        }

        await RegisterHandler<FindChannelRequest, FindChannelResponse>(Fdc3Topic.FindChannel, HandleFindChannel);
        await RegisterHandler<FindIntentRequest, FindIntentResponse>(Fdc3Topic.FindIntent, HandleFindIntent);
        await RegisterHandler<FindIntentsByContextRequest, FindIntentsByContextResponse>(Fdc3Topic.FindIntentsByContext, HandleFindIntentsByContext);
        await RegisterHandler<RaiseIntentRequest, RaiseIntentResponse>(Fdc3Topic.RaiseIntent, HandleRaiseIntent);
        await RegisterHandler<GetIntentResultRequest, GetIntentResultResponse>(Fdc3Topic.GetIntentResult, HandleGetIntentResult);
        await RegisterHandler<StoreIntentResultRequest, StoreIntentResultResponse>(Fdc3Topic.SendIntentResult, HandleStoreIntentResult);
        await RegisterHandler<IntentListenerRequest, IntentListenerResponse>(Fdc3Topic.AddIntentListener, HandleAddIntentListener);

        await _desktopAgent.StartAsync(cancellationToken);

        if (_options.ChannelId != null)
        {
            await HandleAddUserChannel(_options.ChannelId);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var unregisteringTasks = new ValueTask[]
        {
                _messageRouter.UnregisterServiceAsync(Fdc3Topic.FindChannel, cancellationToken),
                _messageRouter.UnregisterServiceAsync(Fdc3Topic.FindIntent, cancellationToken),
                _messageRouter.UnregisterServiceAsync(Fdc3Topic.RaiseIntent, cancellationToken),
                _messageRouter.UnregisterServiceAsync(Fdc3Topic.FindIntentsByContext, cancellationToken),
                _messageRouter.UnregisterServiceAsync(Fdc3Topic.GetIntentResult, cancellationToken),
                _messageRouter.UnregisterServiceAsync(Fdc3Topic.SendIntentResult, cancellationToken),
                _messageRouter.UnregisterServiceAsync(Fdc3Topic.AddIntentListener, cancellationToken)
        };

        await SafeWaitAsync(unregisteringTasks);
        await _desktopAgent.StopAsync(cancellationToken);
    }
}
