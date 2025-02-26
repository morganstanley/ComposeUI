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

using System.Collections.Generic;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;
using MorganStanley.ComposeUI.Messaging;
using MorganStanley.ComposeUI.Messaging.Abstractions;
using System.Windows;

namespace MorganStanley.ComposeUI.Shell.Fdc3.ChannelSelector
{
    internal class ChannelSelectorInstanceCommunicator : IChannelSelectorInstanceCommunicator
    {
        private readonly ILogger<ChannelSelectorInstanceCommunicator> _logger;
        private readonly IMessageRouter _messageRouter;
        private readonly object _disposeLock = new();
        private readonly List<Func<ValueTask>> _disposeTask = new();
        private readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web);

        IChannelSelectorInstanceCommunicator _channelSelectorComm;

        public ChannelSelectorInstanceCommunicator(IMessageRouter messageRouter) {
            _messageRouter = messageRouter;
        }
         public async void InvokeChannelSelectorRequest(ChannelSelectorRequest request, CancellationToken cancellationToken = default)
         {
             await _messageRouter.PublishAsync(
                 "ComposeUI/fdc3/v2.0/changeChannel",
                 MessageBuffer.Factory.CreateJson(request, _jsonSerializerOptions),
                 cancellationToken: cancellationToken
             );
         }
        
         public async Task RegisterMessageRouterForInstance(string instanceId, Fdc3ChannelSelectorColorupdateEventHandler eventHandler, CancellationToken cancellationToken = default)
         {
             await _messageRouter.RegisterServiceAsync(
             $"ComposeUI/fdc3/v2.0/channelSelector-{instanceId}",
             async (endpoint, payload, context) =>
             {
                 var request = payload?.ReadJson<ChannelSelectorRequest>(_jsonSerializerOptions);
                 if (request == null)
                 {
                     return null;
                 }
                 

                 return null;
             },
             cancellationToken: cancellationToken);

            
            var subscription = await _messageRouter.SubscribeAsync($"ComposeUI/fdc3/v2.0/channelSelectorColor-{instanceId}", 
                
                async (payloadBuffer) => {
                    var request = payloadBuffer?.ReadJson<JoinUserChannelRequest>(_jsonSerializerOptions);
                    if (request == null)
                    {
                        return;
                    }

                    await Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        eventHandler(request.Color);
                    });
                },
                cancellationToken: default);

             lock (_disposeLock)
             {
                 _disposeTask.Add(
                     async () =>
                     {
                         if (_messageRouter != null)
                         {
                             await _messageRouter.UnregisterServiceAsync($"ComposeUI/fdc3/v2.0/channelSelector-{instanceId}");
                             await _messageRouter.DisposeAsync();
                         }
                     });
             }
         }
    }
}