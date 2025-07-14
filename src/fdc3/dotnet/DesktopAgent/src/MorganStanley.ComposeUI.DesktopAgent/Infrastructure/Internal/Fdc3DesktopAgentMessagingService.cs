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
using System.Text.Json.Serialization;
using Finos.Fdc3;
using Finos.Fdc3.Context;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Channels;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Converters;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.DependencyInjection;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Exceptions;
using MorganStanley.ComposeUI.MessagingAdapter.Abstractions;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure.Internal;

internal class Fdc3DesktopAgentMessagingService : IHostedService
{
    private readonly IMessaging _messaging;
    private readonly IFdc3DesktopAgentBridge _desktopAgent;
    private readonly Fdc3DesktopAgentOptions _options;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<Fdc3DesktopAgentMessagingService> _logger;
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

    public Fdc3DesktopAgentMessagingService(
        IMessaging messaging,
        IFdc3DesktopAgentBridge desktopAgent,
        IOptions<Fdc3DesktopAgentOptions> options,
        ILoggerFactory? loggerFactory = null)
    {
        _messaging = messaging;
        _desktopAgent = desktopAgent;
        _options = options.Value;
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _logger = _loggerFactory.CreateLogger<Fdc3DesktopAgentMessagingService>() ?? NullLogger<Fdc3DesktopAgentMessagingService>.Instance;
    }

    public async ValueTask<UserChannel?> HandleAddUserChannel(string id)
    {
        var userChannel = await _desktopAgent.AddUserChannel((channelId) => new UserChannel(channelId, _messaging, _loggerFactory.CreateLogger<UserChannel>()), id);
        return userChannel;
    }

    internal ValueTask<FindChannelResponse?> HandleFindChannel(FindChannelRequest? request, MessageAdapterContext? context)
    {
        return new ValueTask<FindChannelResponse?>(
            _desktopAgent.FindChannel(request!.ChannelId, request!.ChannelType)
                ? FindChannelResponse.Success
                : FindChannelResponse.Failure(ChannelError.NoChannelFound));
    }

    internal async ValueTask<FindIntentResponse?> HandleFindIntent(FindIntentRequest? request, MessageAdapterContext? context)
    {
        var contextType = request?.Context != null ? JsonSerializer.Deserialize<Context>(request.Context, _jsonSerializerOptions)?.Type : null;
        return await _desktopAgent.FindIntent(request, contextType);
    }

    internal async ValueTask<FindIntentsByContextResponse?> HandleFindIntentsByContext(FindIntentsByContextRequest? request, MessageAdapterContext? context)
    {
        var contextType = request?.Context != null ? JsonSerializer.Deserialize<Context>(request.Context, _jsonSerializerOptions)?.Type : null;
        return await _desktopAgent.FindIntentsByContext(request, contextType);
    }

    internal async ValueTask<RaiseIntentResponse?> HandleRaiseIntent(RaiseIntentRequest? request, MessageAdapterContext context)
    {
        var contextType = request?.Context != null ? JsonSerializer.Deserialize<Context>(request.Context, _jsonSerializerOptions)?.Type : null;

        var result = await _desktopAgent.RaiseIntent(request, contextType!);
        if (result.RaiseIntentResolutionMessages.Any())
        {
            foreach (var message in result.RaiseIntentResolutionMessages)
            {
                await _messaging.PublishAsync(
                    Fdc3Topic.RaiseIntentResolution(message.Intent, message.TargetModuleInstanceId),
                    JsonFactory.CreateJson(message.Request, _jsonSerializerOptions));
            }
        }

        return result.Response;
    }

    internal async ValueTask<IntentListenerResponse?> HandleAddIntentListener(IntentListenerRequest? request, MessageAdapterContext? context)
    {
        return await _desktopAgent.AddIntentListener(request);
    }

    internal async ValueTask<StoreIntentResultResponse?> HandleStoreIntentResult(StoreIntentResultRequest? request, MessageAdapterContext? context)
    {
        return await _desktopAgent.StoreIntentResult(request);
    }

    internal async ValueTask<GetIntentResultResponse?> HandleGetIntentResult(GetIntentResultRequest? request, MessageAdapterContext? context)
    {
        return await _desktopAgent.GetIntentResult(request);
    }

    internal async ValueTask<CreatePrivateChannelResponse?> HandleCreatePrivateChannel(CreatePrivateChannelRequest? request, MessageAdapterContext? context)
    {
        try
        {
            var privateChannelId = Guid.NewGuid().ToString();
            await _desktopAgent.AddPrivateChannel((channelId) => new PrivateChannel(channelId, _messaging, _loggerFactory.CreateLogger<PrivateChannel>(), request.InstanceId), privateChannelId);

            return CreatePrivateChannelResponse.Created(privateChannelId);
        }
        catch (Exception ex)
        {
            // TODO: better exception
            return CreatePrivateChannelResponse.Failed(ex.Message);
        }
    }

    internal async ValueTask<CreateAppChannelResponse?> HandleCreateAppChannel(
        CreateAppChannelRequest? request,
        MessageAdapterContext? context)
    {
        if (request == null)
        {
            return CreateAppChannelResponse.Failed(Fdc3DesktopAgentErrors.PayloadNull);
        }

        return await _desktopAgent.AddAppChannel((channelId) => new AppChannel(channelId, _messaging, _loggerFactory.CreateLogger<AppChannel>()), request);
    }

    internal async ValueTask<GetUserChannelsResponse?> HandleGetUserChannels(
        GetUserChannelsRequest? request,
        MessageAdapterContext? context)
    {
        return await _desktopAgent.GetUserChannels(request);
    }

    internal async ValueTask<GetInfoResponse?> HandleGetInfo(GetInfoRequest? request, MessageAdapterContext? context)
    {
        return await _desktopAgent.GetInfo(request);
    }

    internal async ValueTask<JoinUserChannelResponse?> HandleJoinUserChannel(JoinUserChannelRequest? request, MessageAdapterContext? context)
    {
        if (request == null)
        {
            return JoinUserChannelResponse.Failed(Fdc3DesktopAgentErrors.PayloadNull);
        }

        return await _desktopAgent.JoinUserChannel((channelId) => new UserChannel(channelId, _messaging, _loggerFactory.CreateLogger<UserChannel>()), request);
    }

    internal async ValueTask<FindInstancesResponse?> HandleFindInstances(FindInstancesRequest? request, MessageAdapterContext? context)
    {
        return await _desktopAgent.FindInstances(request);
    }

    internal async ValueTask<GetAppMetadataResponse?> HandleGetAppMetadata(GetAppMetadataRequest? request, MessageAdapterContext? context)
    {
        return await _desktopAgent.GetAppMetadata(request);
    }

    internal async ValueTask<AddContextListenerResponse?> HandleAddContextListener(AddContextListenerRequest? request, MessageAdapterContext? context)
    {
        return await _desktopAgent.AddContextListener(request);
    }

    internal async ValueTask<RemoveContextListenerResponse?> HandleRemoveContextListener(RemoveContextListenerRequest? request, MessageAdapterContext? context)
    {
        return await _desktopAgent.RemoveContextListener(request);
    }

    internal async ValueTask<OpenResponse?> HandleOpen(OpenRequest? request, MessageAdapterContext? context)
    {
        string? contextType = null;
        if (!string.IsNullOrEmpty(request?.Context))
        {
            contextType = JsonObject.Parse(request.Context)?["type"]?.GetValue<string>();
        }
        return await _desktopAgent.Open(request, contextType);
    }

    public async ValueTask<RaiseIntentResponse> HandleRaiseIntentForContext(RaiseIntentForContextRequest request, MessageAdapterContext context)
    {
        var contextType = request?.Context != null ? JsonSerializer.Deserialize<Context>(request.Context, _jsonSerializerOptions)?.Type : null;

        var result = await _desktopAgent.RaiseIntentForContext(request, contextType!);
        if (result.RaiseIntentResolutionMessages.Any())
        {
            foreach (var message in result.RaiseIntentResolutionMessages)
            {
                await _messaging.PublishAsync(
                    Fdc3Topic.RaiseIntentResolution(message.Intent, message.TargetModuleInstanceId),
                    JsonFactory.CreateJson(message.Request, _jsonSerializerOptions));
            }
        }
        return result.Response;
    }

    internal async ValueTask<GetOpenedAppContextResponse?> HandleGetOpenedAppContext(
        GetOpenedAppContextRequest? request,
        MessageAdapterContext? context)
    {
        return await _desktopAgent.GetOpenedAppContext(request);
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
                _logger.LogError(exception, "An exception was thrown while waiting for a task to finish.");
            }
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        async Task RegisterHandler<TRequest, TResponse>(string topic, Func<TRequest?, MessageAdapterContext?, ValueTask<TResponse?>> handler) where TRequest : class
        {
            await _messaging.RegisterServiceAsync(topic,
                async (endpoint, payload, context) =>
                {
                    var request = payload?.ReadJson<TRequest>(_jsonSerializerOptions);
                    var response = await handler(request, context);
                    return response is null ? null : JsonFactory.CreateJson(response, _jsonSerializerOptions);
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
        await RegisterHandler<AddContextListenerRequest, AddContextListenerResponse>(Fdc3Topic.AddContextListener, HandleAddContextListener);
        await RegisterHandler<RemoveContextListenerRequest, RemoveContextListenerResponse>(Fdc3Topic.RemoveContextListener, HandleRemoveContextListener);
        await RegisterHandler<OpenRequest, OpenResponse>(Fdc3Topic.Open, HandleOpen);
        await RegisterHandler<GetOpenedAppContextRequest, GetOpenedAppContextResponse>(Fdc3Topic.GetOpenedAppContext, HandleGetOpenedAppContext);
        await RegisterHandler<RaiseIntentForContextRequest, RaiseIntentResponse>(Fdc3Topic.RaiseIntentForContext, HandleRaiseIntentForContext);

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
            _messaging.UnregisterServiceAsync(Fdc3Topic.FindChannel, cancellationToken),
            _messaging.UnregisterServiceAsync(Fdc3Topic.FindIntent, cancellationToken),
            _messaging.UnregisterServiceAsync(Fdc3Topic.RaiseIntent, cancellationToken),
            _messaging.UnregisterServiceAsync(Fdc3Topic.FindIntentsByContext, cancellationToken),
            _messaging.UnregisterServiceAsync(Fdc3Topic.GetIntentResult, cancellationToken),
            _messaging.UnregisterServiceAsync(Fdc3Topic.SendIntentResult, cancellationToken),
            _messaging.UnregisterServiceAsync(Fdc3Topic.AddIntentListener, cancellationToken),
            _messaging.UnregisterServiceAsync(Fdc3Topic.CreatePrivateChannel, cancellationToken),
            _messaging.UnregisterServiceAsync(Fdc3Topic.CreateAppChannel, cancellationToken),
            _messaging.UnregisterServiceAsync(Fdc3Topic.GetUserChannels, cancellationToken),
            _messaging.UnregisterServiceAsync(Fdc3Topic.JoinUserChannel, cancellationToken),
            _messaging.UnregisterServiceAsync(Fdc3Topic.GetInfo, cancellationToken),
            _messaging.UnregisterServiceAsync(Fdc3Topic.FindInstances, cancellationToken),
            _messaging.UnregisterServiceAsync(Fdc3Topic.GetAppMetadata, cancellationToken),
            _messaging.UnregisterServiceAsync(Fdc3Topic.AddContextListener, cancellationToken),
            _messaging.UnregisterServiceAsync(Fdc3Topic.RemoveContextListener, cancellationToken),
            _messaging.UnregisterServiceAsync(Fdc3Topic.Open, cancellationToken),
            _messaging.UnregisterServiceAsync(Fdc3Topic.GetOpenedAppContext, cancellationToken),
            _messaging.UnregisterServiceAsync(Fdc3Topic.RaiseIntentForContext, cancellationToken),
        };

        await SafeWaitAsync(unregisteringTasks);
        await _desktopAgent.StopAsync(cancellationToken);
    }
}
