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

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.DependencyInjection;
using MorganStanley.ComposeUI.Messaging;
using MorganStanley.Fdc3;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent;

public class Fdc3DesktopAgent : IHostedService
{
    private readonly List<UserChannel> _userChannels = new();

    private readonly ILoggerFactory _loggerFactory;
    private readonly IMessageRouter _messageRouter;
    private Fdc3DesktopAgentConfig? _config;

    public Fdc3DesktopAgent(IMessageRouter messageRouter, ILoggerFactory? loggerFactory, Fdc3DesktopAgentConfig? config)
    {
        _messageRouter = messageRouter;
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _config = config;
    }

    public async ValueTask AddUserChannel(string id)
    {
        var uc = new UserChannel(id, _messageRouter, _loggerFactory.CreateLogger<UserChannel>());
        await uc.Connect();
        _userChannels.Add(uc);
    }

    internal async ValueTask<MessageBuffer?> FindChannel(string _, MessageBuffer? requestBuffer, MessageContext _2)
    {
        var request = requestBuffer?.ReadJson<FindChannelRequest>();
        if (request?.ChannelType == ChannelType.User)
        {
            if (_userChannels.Any(x => x.Id == request.ChannelId))
            {
                return MessageBuffer.Factory.CreateJson(FindChannelResponse.Success);
            }
        }
        return MessageBuffer.Factory.CreateJson(FindChannelResponse.Failure(ChannelError.NoChannelFound));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _messageRouter.RegisterServiceAsync(Fdc3Topic.FindChannel, FindChannel);

        if (_config == null)
        {
            return;
        }

        // There are no dependencies among possible actions at the moment.
        // Future features may introduce dependencies, in that case this solution needs to change
        foreach (var task in _config.BuilderActions.Select(x => x(this)).ToArray())
        {
            await (task);
        }

        _config = null;        
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var tasks = _userChannels.Select(x => x.DisposeAsync()).ToArray();
        var unregister = _messageRouter.UnregisterServiceAsync(Fdc3Topic.FindChannel);

        foreach (var task in tasks)
        {
            try
            {
                await task;
            }
            catch
            {
                // If disposing one of the channels fails for some reason, we should still try to wait for the rest
            }
        }
        await unregister;
    }
}
