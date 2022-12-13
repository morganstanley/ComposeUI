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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MorganStanley.ComposeUI.Messaging;

namespace ComposeUI.Example.DataService
{
    internal class SubscriptionsMonitor : IObserver<RouterMessage>
    {
        public SubscriptionsMonitor(IMessageRouter messageRouter)
        {
            _messageRouter = messageRouter;
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter("Microsoft", LogLevel.Information)
                    .AddFilter("System", LogLevel.Information)
                    .AddFilter("LoggingConsoleApp.SubscriptionsMonitor", LogLevel.Information)
                    .AddConsole();
            });

            _logger = loggerFactory.CreateLogger<SubscriptionsMonitor>();
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
            _logger?.LogError(error.ToString());
        }

        public async void OnNext(RouterMessage message)
        {
            _subscriptions.Add(message);

            var payload = message.Payload;

            if (_logger.IsEnabled(LogLevel.Information)) 
            { 
                _logger.LogInformation("Message {payload}", payload);
            }

            if (payload != null)
            {
                Selected = JsonSerializer.Deserialize<MonthlySalesDataModel>(payload.GetSpan(), MonthlySalesDataModel.JsonSerializerOptions);

                if (Selected != null)
                {
                    var toPublish = MonthlySalesData.MyList[Selected.Symbol];

                    try
                    {
                        await _messageRouter.PublishAsync("proto_select_monthlySales", JsonSerializer.Serialize(toPublish, MonthlySalesDataModel.JsonSerializerOptions));
                    }
                    catch (Exception exception)
                    {
                        _logger.LogError(exception.ToString());
                    }
                }
            }
        }
    }
}
