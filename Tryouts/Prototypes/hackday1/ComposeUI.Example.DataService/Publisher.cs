// /*
//  * Morgan Stanley makes this available to you under the Apache License,
//  * Version 2.0 (the "License"). You may obtain a copy of the License at
//  *
//  *      http://www.apache.org/licenses/LICENSE-2.0.
//  *
//  * See the NOTICE file distributed with this work for additional information
//  * regarding copyright ownership. Unless required by applicable law or agreed
//  * to in writing, software distributed under the License is distributed on an
//  * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
//  * or implied. See the License for the specific language governing permissions
//  * and limitations under the License.
//  */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ComposeUI.Messaging.Client;
using ComposeUI.Messaging.Client.Transport.WebSocket;
using Microsoft.Extensions.Logging;

namespace ComposeUI.Example.DataService
{
    internal class SubscriptionsMonitor : IObserver<RouterMessage>
    {
        public SubscriptionsMonitor(IMessageRouter messageRouter)
        {
            _messageRouter = messageRouter;
        }
        private readonly List<RouterMessage> _subscriptions = new();
        public MonthlySalesDataModel? Selected;
        private readonly IMessageRouter _messageRouter;
        private readonly ILogger<SubscriptionsMonitor>? _logger;

        public void OnCompleted()
        {
            _subscriptions.Clear();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public async void OnNext(RouterMessage message)
        {
            _subscriptions.Add(message);

            var payload = message.Payload;
   
            if (payload != null) {
                Selected = JsonSerializer.Deserialize<MonthlySalesDataModel>(payload, MonthlySalesDataModel.JsonSerializerOptions);
            
                if (Selected != null)
                {
                    var toPublish = MonthlySalesData.MyList[Selected.Symbol];

                    try
                    {
                        await _messageRouter.PublishAsync("proto_select_monthlySales", JsonSerializer.Serialize(toPublish, MonthlySalesDataModel.JsonSerializerOptions));
                    }
                    catch (Exception exception)
                    {
                        _logger?.LogError(exception.ToString());
                    }
                } 
            }
        }
    }
    internal class Publisher
    {
        public static Uri WebsocketURI { get; set; } = new("ws://localhost:5000/ws");

        private readonly IMessageRouter _messageRouter = MessageRouter.Create(
          mr => mr.UseWebSocket(
              new MessageRouterWebSocketOptions
              {
                  Uri = WebsocketURI
              }));

        private readonly ILogger<Publisher>? _logger;

        public SubscriptionsMonitor Observer;

        public Publisher() {
            Observer = new SubscriptionsMonitor(_messageRouter);
        }
        
        public async void Subscribe()
        {
            try
            {
                await _messageRouter.SubscribeAsync("proto_select_marketData", Observer);
            }
            catch (Exception exception)
            {
                _logger?.LogError(exception.ToString());
            }
        }
    }
}
