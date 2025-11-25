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
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Infrastructure.Internal.Protocol;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Contracts;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Exceptions;
using MorganStanley.ComposeUI.Messaging.Abstractions;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Infrastructure.Internal;

//TODO: Rename this class and its interface to better reflect their responsibilities
internal class ChannelFactory : IChannelFactory
{
    private readonly IMessaging _messaging;
    private readonly string _instanceId;
    private readonly IContext? _openedAppContext;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<ChannelFactory> _logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions = SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization;
    private readonly ConcurrentDictionary<string, IPrivateChannel> _privateChannels = new();

    public ChannelFactory(
        IMessaging messaging,
        string fdc3InstanceId,
        IContext? openedAppContext = null,
        ILoggerFactory? loggerFactory = null)
    {
        _messaging = messaging;
        _instanceId = fdc3InstanceId;
        _openedAppContext = openedAppContext;
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _logger = _loggerFactory.CreateLogger<ChannelFactory>();
    }

    public async ValueTask<ContextListener<T>> CreateContextListenerAsync<T>(
        ContextHandler<T> contextHandler,
        IChannel? currentChannel = null,
        string? contextType = null)
        where T : IContext
    {
        if (currentChannel == null)
        {
            return new ContextListener<T>(
                instanceId: _instanceId,
                contextHandler: contextHandler,
                messaging: _messaging,
                contextType: contextType,
                openedAppContext: _openedAppContext,
                logger: _loggerFactory.CreateLogger<ContextListener<T>>());
        }

        if (await currentChannel.AddContextListener<T>(contextType, contextHandler) is ContextListener<T> contextListener)
        {
            _logger.LogDebug("Added context listener to channel {CurrentChannelId} for context type {ContextType}.)", currentChannel.Id, contextType ?? "null");

            return contextListener;
        }
        else
        {
            throw ThrowHelper.ContextListenerNotCreated(currentChannel.Id, contextType);
        }
    }

    public async ValueTask<IChannel> CreateAppChannelAsync(string channelId)
    {
        var request = new CreateAppChannelRequest
        {
            InstanceId = _instanceId,
            ChannelId = channelId,
        };

        var response = await _messaging.InvokeJsonServiceAsync<CreateAppChannelRequest, CreateAppChannelResponse>(
            Fdc3Topic.CreateAppChannel,
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

        if (!response.Success)
        {
            throw ThrowHelper.ErrorResponseReceived(Fdc3DesktopAgentErrors.UnspecifiedReason);
        }

        return new Channel(
            channelId: channelId,
            channelType: ChannelType.App,
            messaging: _messaging,
            instanceId: _instanceId,
            displayMetadata: null,
            loggerFactory: _loggerFactory);
    }

    public async ValueTask<IEnumerable<IChannel>> GetUserChannelsAsync()
    {
        var request = new GetUserChannelsRequest
        {
            InstanceId = _instanceId
        };

        var response = await _messaging.InvokeJsonServiceAsync<GetUserChannelsRequest, GetUserChannelsResponse>(
            Fdc3Topic.GetUserChannels,
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

        if (response.Channels == null || !response.Channels.Any())
        {
            throw ThrowHelper.DesktopAgentBackendDidNotResolveRequest(nameof(GetUserChannelsRequest), nameof(response.Channels), Fdc3DesktopAgentErrors.NoUserChannelSetFound);
        }

        var channels = new List<IChannel>();

        foreach (var channel in response.Channels)
        {
            if (channel.DisplayMetadata == null)
            {
                _logger.LogDebug("Skipping channel with missing ChannelId: {ChannelId} or the {NameOfDisplayMetadata}.", channel.Id, nameof(DisplayMetadata));
            }

            if (string.IsNullOrEmpty(channel.Id))
            {
                _logger.LogDebug("Skipping channel with missing {NameOfChannelId}.", nameof(channel.Id));
                continue;
            }

            var userChannel = new Channel(
                channelId: channel.Id,
                channelType: ChannelType.User,
                instanceId: _instanceId,
                messaging: _messaging,
                displayMetadata: channel.DisplayMetadata,
                loggerFactory: _loggerFactory);

            channels.Add(userChannel);
        }

        return channels;
    }

    public async ValueTask<IChannel> JoinUserChannelAsync(string channelId)
    {
        var request = new JoinUserChannelRequest
        {
            InstanceId = _instanceId,
            ChannelId = channelId,
        };

        var response = await _messaging.InvokeJsonServiceAsync<JoinUserChannelRequest, JoinUserChannelResponse>(
            serviceName: Fdc3Topic.JoinUserChannel,
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

        if (!response.Success)
        {
            throw ThrowHelper.UnsuccessfulUserChannelJoining(request);
        }

        var channel = new Channel(
            channelId: channelId,
            channelType: ChannelType.User,
            instanceId: _instanceId,
            messaging: _messaging,
            openedAppContext: _openedAppContext,
            displayMetadata: response.DisplayMetadata,
            loggerFactory: _loggerFactory);

        return channel;
    }

    public async ValueTask<IChannel> FindChannelAsync(string channelId, ChannelType channelType)
    {
        var request = new FindChannelRequest
        {
            ChannelId = channelId,
            ChannelType = channelType
        };

        var response = await _messaging.InvokeJsonServiceAsync<FindChannelRequest, FindChannelResponse>(
            Fdc3Topic.FindChannel,
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

        if (!response.Found)
        {
            throw ThrowHelper.ChannelNotFound(channelId, channelType);
        }

        if (channelType == ChannelType.Private)
        {
            return await JoinPrivateChannelAsync(channelId);
        }

        //This is only called when raising an intent, the RaiseIntent logic.
        return new Channel(
            channelId: channelId,
            channelType: channelType,
            instanceId: _instanceId,
            messaging: _messaging,
            openedAppContext: _openedAppContext,
            loggerFactory: _loggerFactory);
    }

    public async ValueTask<IPrivateChannel> CreatePrivateChannelAsync()
    {
        var request = new CreatePrivateChannelRequest
        {
            InstanceId = _instanceId
        };

        var response = await _messaging.InvokeJsonServiceAsync<CreatePrivateChannelRequest, CreatePrivateChannelResponse>(
            Fdc3Topic.CreatePrivateChannel,
            request,
            _jsonSerializerOptions);

        if (response == null)
        {
            throw ThrowHelper.PrivateChannelCreationFailed();
        }

        if (!string.IsNullOrEmpty(response.Error))
        {
            throw ThrowHelper.ErrorResponseReceived(response.Error);
        }

        var channel = new PrivateChannel(
            channelId: response.ChannelId!,
            instanceId: _instanceId,
            messaging: _messaging,
            isOriginalCreator: true,
            onDisconnect: () => _privateChannels.TryRemove(response.ChannelId!, out _),
            displayMetadata: null,
            loggerFactory: _loggerFactory);

        _privateChannels.TryAdd(response.ChannelId!, channel);

        return channel;
    }

    private async ValueTask<IPrivateChannel> JoinPrivateChannelAsync(string channelId)
    {
        var request = new JoinPrivateChannelRequest
        {
            InstanceId = _instanceId,
            ChannelId = channelId
        };

        var response = await _messaging.InvokeJsonServiceAsync<JoinPrivateChannelRequest, JoinPrivateChannelResponse>(
            Fdc3Topic.JoinPrivateChannel,
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

        if (!response.Success)
        {
            throw ThrowHelper.PrivateChannelJoiningFailed(channelId);
        }

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Joined private channel {ChannelId}.", channelId);
        }

        var channel = _privateChannels.GetOrAdd(
            channelId, 
            new PrivateChannel(
                channelId: channelId,
                instanceId: _instanceId,
                messaging: _messaging,
                isOriginalCreator: false,
                onDisconnect: () => _privateChannels.TryRemove(channelId, out _),
                displayMetadata: null,
                loggerFactory: _loggerFactory));

        return channel;
    }
}
