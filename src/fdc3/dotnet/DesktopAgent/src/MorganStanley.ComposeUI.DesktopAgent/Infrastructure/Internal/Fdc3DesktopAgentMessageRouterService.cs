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
using System.Text.Json.Serialization;
using Finos.Fdc3;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Channels;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Converters;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.DependencyInjection;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Exceptions;
using MorganStanley.ComposeUI.Messaging;
using MorganStanley.ComposeUI.Messaging.Abstractions;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure.Internal;

internal class Fdc3DesktopAgentMessageRouterService : IHostedService
{
    private readonly IMessagingService _messageRouter;
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
            new DisplayMetadataJsonConverter(),
            new IconJsonConverter(),
            new ImageJsonConverter(),
            new IntentMetadataJsonConverter(),
            new ImplementationMetadataJsonConverter(),
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    public JsonSerializerOptions JsonMessageSerializerOptions => new(_jsonSerializerOptions);

    public Fdc3DesktopAgentMessageRouterService(
        IMessagingService messageRouter,
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

    internal ValueTask<FindChannelResponse?> HandleFindChannel(FindChannelRequest? request, MessageContext? context)
    {
        return ValueTask.FromResult<FindChannelResponse?>(
            _desktopAgent.FindChannel(request!.ChannelId, request!.ChannelType)
                ? FindChannelResponse.Success
                : FindChannelResponse.Failure(ChannelError.NoChannelFound));
    }

    internal async ValueTask<FindIntentResponse?> HandleFindIntent(FindIntentRequest? request, MessageContext? context)
    {
        return await _desktopAgent.FindIntent(request);
    }

    internal async ValueTask<FindIntentsByContextResponse?> HandleFindIntentsByContext(FindIntentsByContextRequest? request, MessageContext? context)
    {
        return await _desktopAgent.FindIntentsByContext(request);
    }

    internal async ValueTask<RaiseIntentResponse?> HandleRaiseIntent(RaiseIntentRequest? request, MessageContext? context)
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

    internal async ValueTask<IntentListenerResponse?> HandleAddIntentListener(IntentListenerRequest? request, MessageContext? context)
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

    internal async ValueTask<StoreIntentResultResponse?> HandleStoreIntentResult(StoreIntentResultRequest? request, MessageContext? context)
    {
        return await _desktopAgent.StoreIntentResult(request);
    }

    internal async ValueTask<GetIntentResultResponse?> HandleGetIntentResult(GetIntentResultRequest? request, MessageContext? context)
    {
        return await _desktopAgent.GetIntentResult(request);
    }

    internal async ValueTask<CreatePrivateChannelResponse> HandleCreatePrivateChannel(CreatePrivateChannelRequest request, MessageContext? context)
    {
        try
        {
            var channel = new PrivateChannel(Guid.NewGuid().ToString(), _messageRouter, _loggerFactory.CreateLogger<PrivateChannel>());

            await _desktopAgent.AddPrivateChannel(channel);

            return CreatePrivateChannelResponse.Created(channel.Id);
        }
        catch (Exception ex)
        {
            // TODO: better exception
            return CreatePrivateChannelResponse.Failed(ex.Message);
        }
    }

    internal async ValueTask<CreateAppChannelResponse?> HandleCreateAppChannel(
        CreateAppChannelRequest? request,
        MessageContext? context)
    {
        if (request == null)
        {
            return CreateAppChannelResponse.Failed(ChannelError.CreationFailed);
        }

        var channel = new AppChannel(request.ChannelId, _messageRouter, _loggerFactory.CreateLogger<AppChannel>());
        return await _desktopAgent.AddAppChannel(channel, request.InstanceId);
    }

    internal async ValueTask<GetUserChannelsResponse?> HandleGetUserChannels(
        GetUserChannelsRequest? request,
        MessageContext? context)
    {
        return await _desktopAgent.GetUserChannels(request);
    }

    internal async ValueTask<GetInfoResponse?> HandleGetInfo(GetInfoRequest? request, MessageContext? context)
    {
        return await _desktopAgent.GetInfo(request);
    }

    internal async ValueTask<JoinUserChannelResponse?> HandleJoinUserChannel(JoinUserChannelRequest? request, MessageContext? context)
    {
        if (request == null)
        {
            return JoinUserChannelResponse.Failed(Fdc3DesktopAgentErrors.PayloadNull);
        }

        var channel = new UserChannel(request.ChannelId, _messageRouter, _loggerFactory.CreateLogger<UserChannel>());
        return await _desktopAgent.JoinUserChannel(channel, request.InstanceId);
    }

    internal async ValueTask<FindInstancesResponse?> HandleFindInstances(FindInstancesRequest? request, MessageContext? context)
    {
        return await _desktopAgent.FindInstances(request);
    }

    internal async ValueTask<GetAppMetadataResponse> HandleGetAppMetadata(GetAppMetadataRequest? request, MessageContext? context)
    {
        return await _desktopAgent.GetAppMetadata(request);
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
                _logger.LogError($"An exception was thrown while waiting for a task to finish. Exception: {exception}");
            }
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        async Task RegisterHandler<TRequest, TResponse>(string topic, Func<TRequest?, MessageContext?, ValueTask<TResponse?>> handler) where TRequest : class
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
        await RegisterHandler<CreatePrivateChannelRequest, CreatePrivateChannelResponse>(Fdc3Topic.CreatePrivateChannel, HandleCreatePrivateChannel);
        await RegisterHandler<CreateAppChannelRequest, CreateAppChannelResponse>(Fdc3Topic.CreateAppChannel, HandleCreateAppChannel);
        await RegisterHandler<GetUserChannelsRequest, GetUserChannelsResponse>(Fdc3Topic.GetUserChannels, HandleGetUserChannels);
        await RegisterHandler<JoinUserChannelRequest, JoinUserChannelResponse>(Fdc3Topic.JoinUserChannel, HandleJoinUserChannel);
        await RegisterHandler<GetInfoRequest, GetInfoResponse>(Fdc3Topic.GetInfo, HandleGetInfo);
        await RegisterHandler<FindInstancesRequest, FindInstancesResponse>(Fdc3Topic.FindInstances, HandleFindInstances);
        await RegisterHandler<GetAppMetadataRequest, GetAppMetadataResponse>(Fdc3Topic.GetAppMetadata, HandleGetAppMetadata);

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
            _messageRouter.UnregisterServiceAsync(Fdc3Topic.AddIntentListener, cancellationToken),
            _messageRouter.UnregisterServiceAsync(Fdc3Topic.CreatePrivateChannel, cancellationToken),
            _messageRouter.UnregisterServiceAsync(Fdc3Topic.CreateAppChannel, cancellationToken),
            _messageRouter.UnregisterServiceAsync(Fdc3Topic.GetUserChannels, cancellationToken),
            _messageRouter.UnregisterServiceAsync(Fdc3Topic.JoinUserChannel, cancellationToken),
            _messageRouter.UnregisterServiceAsync(Fdc3Topic.GetInfo, cancellationToken),
            _messageRouter.UnregisterServiceAsync(Fdc3Topic.FindInstances, cancellationToken),
            _messageRouter.UnregisterServiceAsync(Fdc3Topic.GetAppMetadata, cancellationToken),
        };

        await SafeWaitAsync(unregisteringTasks);
        await _desktopAgent.StopAsync(cancellationToken);
    }
}
