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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Channels;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.DependencyInjection;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Contracts;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Exceptions;
using MorganStanley.ComposeUI.Messaging.Abstractions;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure.Internal;

internal class Fdc3DesktopAgentMessagingService : IHostedService
{
    private readonly IMessaging _messaging;
    private readonly IFdc3DesktopAgentService _desktopAgent;
    private readonly Fdc3DesktopAgentOptions _options;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<Fdc3DesktopAgentMessagingService> _logger;
    private readonly List<IAsyncDisposable> _registeredServices = new List<IAsyncDisposable>();

    private readonly JsonSerializerOptions _jsonSerializerOptions = SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization;

    internal JsonSerializerOptions JsonMessageSerializerOptions => _jsonSerializerOptions;

    public Fdc3DesktopAgentMessagingService(
        IMessaging messaging,
        IFdc3DesktopAgentService desktopAgent,
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
        var userChannel = await _desktopAgent.AddUserChannel((channelId) => new UserChannel(channelId, _messaging, _jsonSerializerOptions, _loggerFactory.CreateLogger<UserChannel>()), id).ConfigureAwait(false);
        return userChannel;
    }

    internal ValueTask<FindChannelResponse?> HandleFindChannel(FindChannelRequest? request)
    {
        return new ValueTask<FindChannelResponse?>(
            _desktopAgent.FindChannel(request!.ChannelId, request!.ChannelType)
                ? FindChannelResponse.Success
                : FindChannelResponse.Failure(ChannelError.NoChannelFound));
    }

    internal async ValueTask<FindIntentResponse?> HandleFindIntent(FindIntentRequest? request)
    {
        var contextType = request?.Context != null ? JsonSerializer.Deserialize<Context>(request.Context, _jsonSerializerOptions)?.Type : null;
        return await _desktopAgent.FindIntent(request, contextType).ConfigureAwait(false);
    }

    internal async ValueTask<FindIntentsByContextResponse?> HandleFindIntentsByContext(FindIntentsByContextRequest? request)
    {
        var contextType = request?.Context != null ? JsonSerializer.Deserialize<Context>(request.Context, _jsonSerializerOptions)?.Type : null;
        return await _desktopAgent.FindIntentsByContext(request, contextType).ConfigureAwait(false);
    }

    internal async ValueTask<RaiseIntentResponse?> HandleRaiseIntent(RaiseIntentRequest? request)
    {
        var contextType = request?.Context != null ? JsonSerializer.Deserialize<Context>(request.Context, _jsonSerializerOptions)?.Type : null;

        var result = await _desktopAgent.RaiseIntent(request, contextType!).ConfigureAwait(false);
        if (result.RaiseIntentResolutionMessages.Any())
        {
            foreach (var message in result.RaiseIntentResolutionMessages)
            {
                await _messaging.PublishJsonAsync(
                    Fdc3Topic.RaiseIntentResolution(message.Intent, message.TargetModuleInstanceId),
                    message.Request, _jsonSerializerOptions).ConfigureAwait(false);
            }
        }

        return result.Response;
    }

    internal async ValueTask<IntentListenerResponse?> HandleAddIntentListener(IntentListenerRequest? request)
    {
        return await _desktopAgent.AddIntentListener(request).ConfigureAwait(false);
    }

    internal async ValueTask<StoreIntentResultResponse?> HandleStoreIntentResult(StoreIntentResultRequest? request)
    {
        return await _desktopAgent.StoreIntentResult(request).ConfigureAwait(false);
    }

    internal async ValueTask<GetIntentResultResponse?> HandleGetIntentResult(GetIntentResultRequest? request)
    {
        return await _desktopAgent.GetIntentResult(request).ConfigureAwait(false);
    }

    internal async ValueTask<CreatePrivateChannelResponse?> HandleCreatePrivateChannel(CreatePrivateChannelRequest? request)
    {
        if (request == null)
        {
            return CreatePrivateChannelResponse.Failed(Fdc3DesktopAgentErrors.PayloadNull);
        }

        try
        {
            var privateChannelId = Guid.NewGuid().ToString();
            await _desktopAgent.CreateOrJoinPrivateChannel((channelId) => new PrivateChannel(channelId, _messaging, _jsonSerializerOptions, _loggerFactory.CreateLogger<PrivateChannel>()), privateChannelId, request.InstanceId).ConfigureAwait(false);

            return CreatePrivateChannelResponse.Created(privateChannelId);
        }
        catch (Exception ex)
        {
            // TODO: better exception
            return CreatePrivateChannelResponse.Failed(ex.Message);
        }
    }

    internal async ValueTask<JoinPrivateChannelResponse?> HandleJoinPrivateChannel(JoinPrivateChannelRequest? request)
    {
        if (request == null)
        {
            return JoinPrivateChannelResponse.Failed(Fdc3DesktopAgentErrors.PayloadNull);
        }
        try
        {
            await _desktopAgent.CreateOrJoinPrivateChannel((channelId) => throw new Fdc3DesktopAgentException(Fdc3DesktopAgentErrors.PrivateChannelNotFound, "The private channel could not be found"), request.ChannelId, request.InstanceId).ConfigureAwait(false);
            return JoinPrivateChannelResponse.Joined;
        }
        catch (Exception ex)
        {
            return JoinPrivateChannelResponse.Failed(ex.Message);
        }
    }

    internal async ValueTask<CreateAppChannelResponse?> HandleCreateAppChannel(
        CreateAppChannelRequest? request)
    {
        if (request == null)
        {
            return CreateAppChannelResponse.Failed(Fdc3DesktopAgentErrors.PayloadNull);
        }

        return await _desktopAgent.AddAppChannel((channelId) => new AppChannel(channelId, _messaging, _jsonSerializerOptions, _loggerFactory.CreateLogger<AppChannel>()), request).ConfigureAwait(false);
    }

    internal async ValueTask<GetUserChannelsResponse?> HandleGetUserChannels(
        GetUserChannelsRequest? request)
    {
        return await _desktopAgent.GetUserChannels(request).ConfigureAwait(false);
    }

    internal async ValueTask<GetInfoResponse?> HandleGetInfo(GetInfoRequest? request)
    {
        return await _desktopAgent.GetInfo(request).ConfigureAwait(false);
    }

    internal async ValueTask<JoinUserChannelResponse?> HandleJoinUserChannel(JoinUserChannelRequest? request)
    {
        if (request == null)
        {
            return JoinUserChannelResponse.Failed(Fdc3DesktopAgentErrors.PayloadNull);
        }

        return await _desktopAgent.JoinUserChannel((channelId) => new UserChannel(channelId, _messaging, _jsonSerializerOptions, _loggerFactory.CreateLogger<UserChannel>()), request).ConfigureAwait(false);
    }

    internal async ValueTask<FindInstancesResponse?> HandleFindInstances(FindInstancesRequest? request)
    {
        return await _desktopAgent.FindInstances(request).ConfigureAwait(false);
    }

    internal async ValueTask<GetAppMetadataResponse?> HandleGetAppMetadata(GetAppMetadataRequest? request)
    {
        return await _desktopAgent.GetAppMetadata(request).ConfigureAwait(false);
    }

    internal async ValueTask<AddContextListenerResponse?> HandleAddContextListener(AddContextListenerRequest? request)
    {
        return await _desktopAgent.AddContextListener(request).ConfigureAwait(false);
    }

    internal async ValueTask<RemoveContextListenerResponse?> HandleRemoveContextListener(RemoveContextListenerRequest? request)
    {
        return await _desktopAgent.RemoveContextListener(request).ConfigureAwait(false);
    }

    internal async ValueTask<OpenResponse?> HandleOpen(OpenRequest? request)
    {
        string? contextType = null;
        if (!string.IsNullOrEmpty(request?.Context))
        {
            contextType = JsonObject.Parse(request.Context)?["type"]?.GetValue<string>();
        }

        return await _desktopAgent.Open(request, contextType).ConfigureAwait(false);
    }

    public async ValueTask<RaiseIntentResponse?> HandleRaiseIntentForContext(RaiseIntentForContextRequest? request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var contextType = request?.Context != null ? JsonSerializer.Deserialize<Context>(request.Context, _jsonSerializerOptions)?.Type : null;

        var result = await _desktopAgent.RaiseIntentForContext(request, contextType!).ConfigureAwait(false);
        if (result.RaiseIntentResolutionMessages.Any())
        {
            foreach (var message in result.RaiseIntentResolutionMessages)
            {
                await _messaging.PublishJsonAsync(
                    Fdc3Topic.RaiseIntentResolution(message.Intent, message.TargetModuleInstanceId),
                    message.Request, _jsonSerializerOptions).ConfigureAwait(false);
            }
        }
        
        return result.Response;
    }

    internal async ValueTask<GetOpenedAppContextResponse?> HandleGetOpenedAppContext(GetOpenedAppContextRequest? request)
    {
        return await _desktopAgent.GetOpenedAppContext(request).ConfigureAwait(false);
    }

    private async ValueTask SafeWaitAsync(IEnumerable<ValueTask> tasks)
    {
        foreach (var task in tasks)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "An exception was thrown while waiting for a task to finish.");
            }
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _registeredServices.Add(await _messaging.RegisterJsonServiceAsync<FindChannelRequest, FindChannelResponse>(Fdc3Topic.FindChannel, HandleFindChannel, _jsonSerializerOptions).ConfigureAwait(false));
        _registeredServices.Add(await _messaging.RegisterJsonServiceAsync<FindIntentRequest, FindIntentResponse>(Fdc3Topic.FindIntent, HandleFindIntent, _jsonSerializerOptions).ConfigureAwait(false));
        _registeredServices.Add(await _messaging.RegisterJsonServiceAsync<FindIntentsByContextRequest, FindIntentsByContextResponse>(Fdc3Topic.FindIntentsByContext, HandleFindIntentsByContext, _jsonSerializerOptions).ConfigureAwait(false));
        _registeredServices.Add(await _messaging.RegisterJsonServiceAsync<RaiseIntentRequest, RaiseIntentResponse>(Fdc3Topic.RaiseIntent, HandleRaiseIntent, _jsonSerializerOptions).ConfigureAwait(false));
        _registeredServices.Add(await _messaging.RegisterJsonServiceAsync<GetIntentResultRequest, GetIntentResultResponse>(Fdc3Topic.GetIntentResult, HandleGetIntentResult, _jsonSerializerOptions).ConfigureAwait(false));
        _registeredServices.Add(await _messaging.RegisterJsonServiceAsync<StoreIntentResultRequest, StoreIntentResultResponse>(Fdc3Topic.SendIntentResult, HandleStoreIntentResult, _jsonSerializerOptions).ConfigureAwait(false));
        _registeredServices.Add(await _messaging.RegisterJsonServiceAsync<IntentListenerRequest, IntentListenerResponse>(Fdc3Topic.AddIntentListener, HandleAddIntentListener, _jsonSerializerOptions).ConfigureAwait(false));
        _registeredServices.Add(await _messaging.RegisterJsonServiceAsync<CreatePrivateChannelRequest, CreatePrivateChannelResponse>(Fdc3Topic.CreatePrivateChannel, HandleCreatePrivateChannel, _jsonSerializerOptions).ConfigureAwait(false));
        _registeredServices.Add(await _messaging.RegisterJsonServiceAsync<JoinPrivateChannelRequest, JoinPrivateChannelResponse>(Fdc3Topic.JoinPrivateChannel, HandleJoinPrivateChannel, _jsonSerializerOptions).ConfigureAwait(false));
        _registeredServices.Add(await _messaging.RegisterJsonServiceAsync<CreateAppChannelRequest, CreateAppChannelResponse>(Fdc3Topic.CreateAppChannel, HandleCreateAppChannel, _jsonSerializerOptions).ConfigureAwait(false));
        _registeredServices.Add(await _messaging.RegisterJsonServiceAsync<GetUserChannelsRequest, GetUserChannelsResponse>(Fdc3Topic.GetUserChannels, HandleGetUserChannels, _jsonSerializerOptions).ConfigureAwait(false));
        _registeredServices.Add(await _messaging.RegisterJsonServiceAsync<JoinUserChannelRequest, JoinUserChannelResponse>(Fdc3Topic.JoinUserChannel, HandleJoinUserChannel, _jsonSerializerOptions).ConfigureAwait(false));
        _registeredServices.Add(await _messaging.RegisterJsonServiceAsync<GetInfoRequest, GetInfoResponse>(Fdc3Topic.GetInfo, HandleGetInfo, _jsonSerializerOptions).ConfigureAwait(false));
        _registeredServices.Add(await _messaging.RegisterJsonServiceAsync<FindInstancesRequest, FindInstancesResponse>(Fdc3Topic.FindInstances, HandleFindInstances, _jsonSerializerOptions).ConfigureAwait(false));
        _registeredServices.Add(await _messaging.RegisterJsonServiceAsync<GetAppMetadataRequest, GetAppMetadataResponse>(Fdc3Topic.GetAppMetadata, HandleGetAppMetadata, _jsonSerializerOptions).ConfigureAwait(false));
        _registeredServices.Add(await _messaging.RegisterJsonServiceAsync<AddContextListenerRequest, AddContextListenerResponse>(Fdc3Topic.AddContextListener, HandleAddContextListener, _jsonSerializerOptions).ConfigureAwait(false));
        _registeredServices.Add(await _messaging.RegisterJsonServiceAsync<RemoveContextListenerRequest, RemoveContextListenerResponse>(Fdc3Topic.RemoveContextListener, HandleRemoveContextListener, _jsonSerializerOptions).ConfigureAwait(false));
        _registeredServices.Add(await _messaging.RegisterJsonServiceAsync<OpenRequest, OpenResponse>(Fdc3Topic.Open, HandleOpen, _jsonSerializerOptions).ConfigureAwait(false));
        _registeredServices.Add(await _messaging.RegisterJsonServiceAsync<GetOpenedAppContextRequest, GetOpenedAppContextResponse>(Fdc3Topic.GetOpenedAppContext, HandleGetOpenedAppContext, _jsonSerializerOptions).ConfigureAwait(false));
        _registeredServices.Add(await _messaging.RegisterJsonServiceAsync<RaiseIntentForContextRequest, RaiseIntentResponse>(Fdc3Topic.RaiseIntentForContext, HandleRaiseIntentForContext, _jsonSerializerOptions).ConfigureAwait(false));

        await _desktopAgent.StartAsync(cancellationToken).ConfigureAwait(false);

        if (_options.ChannelId != null)
        {
            await HandleAddUserChannel(_options.ChannelId).ConfigureAwait(false);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var unregisteringTasks = _registeredServices.Select(x => x.DisposeAsync()).ToArray();

        await SafeWaitAsync(unregisteringTasks).ConfigureAwait(false);
        await _desktopAgent.StopAsync(cancellationToken).ConfigureAwait(false);
    }
}
