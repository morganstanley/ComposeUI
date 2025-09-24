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

//TODO: Rename this class and its interface to better reflect their responsibilities
internal class ChannelFactory : IChannelFactory
{
    private readonly IMessaging _messaging;
    private readonly string _instanceId;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<ChannelFactory> _logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions = SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization;

    public ChannelFactory(
        IMessaging messaging,
        string fdc3InstanceId,
        ILoggerFactory? loggerFactory = null)
    {
        _messaging = messaging;
        _instanceId = fdc3InstanceId;
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _logger = _loggerFactory.CreateLogger<ChannelFactory>();
    }

    public async ValueTask<ContextListener<T>> CreateContextListener<T>(
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

    public async ValueTask<IChannel[]> GetUserChannels()
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

        if (response.Channels == null)
        {
            throw ThrowHelper.NoChannelsReturned();
        }

        var channels = new List<IChannel>();

        foreach (var channel in response.Channels)
        {
            if (channel.DisplayMetadata == null)
            {
                _logger.LogWarning($"Skipping channel with missing ChannelId: {channel.Id} or the {nameof(DisplayMetadata)}.");
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
        return channels.ToArray();
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
            displayMetadata: response.DisplayMetadata,
            loggerFactory: _loggerFactory);

        return channel;
    }
}
