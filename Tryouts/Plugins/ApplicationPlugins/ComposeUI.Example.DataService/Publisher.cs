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
    internal class Publisher
    {
        private readonly IMessageRouter _messageRouter;
        private readonly ILogger<Publisher>? _logger;
        private readonly SubscriptionsMonitor _subscriptionsMonitor;

        public Publisher( IMessageRouter messageRouter, ILogger<Publisher>? logger = null) {
            _messageRouter = messageRouter;
            _logger = logger;

            _subscriptionsMonitor = new SubscriptionsMonitor(_messageRouter);
        }
        
        public async void Subscribe()
        {
            _logger.LogInformation("Subscribe");

            try
            {
                await _messageRouter.SubscribeAsync("proto_select_marketData", _subscriptionsMonitor);
            }
            catch (Exception exception)
            {
                _logger?.LogError(exception.ToString());
            }
        }
    }
}
