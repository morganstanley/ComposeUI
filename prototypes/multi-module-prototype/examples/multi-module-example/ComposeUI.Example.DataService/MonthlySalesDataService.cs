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

using System.Reactive;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using MorganStanley.ComposeUI.Messaging;

namespace ComposeUI.Example.DataService
{
    internal class MonthlySalesDataService : IDisposable
    {
        private readonly IMessageRouter _messageRouter;
        private readonly ILogger<MonthlySalesDataService>? _logger;
        private readonly List<IDisposable> _subscriptions = new();

        public MonthlySalesDataService(IMessageRouter messageRouter, ILogger<MonthlySalesDataService>? logger = null)
        {
            _messageRouter = messageRouter;
            _logger = logger;
        }

        public async Task Start()
        {
            _logger?.LogInformation("Start");

            try
            {
                _subscriptions.Add(
                    await _messageRouter.SubscribeAsync(
                        "proto_select_marketData",
                        AsyncObserver.Create<TopicMessage>(OnSymbolSelected, OnSymbolSelectionError, () => default)));
            }
            catch (Exception exception)
            {
                _logger?.LogError(exception, exception.Message);
            }
        }

        private async ValueTask OnSymbolSelected(TopicMessage message)
        {
            var payload = message.Payload;

            _logger?.LogInformation("Message {payload}", payload?.GetString());

            if (payload != null)
            {
                var selected = payload.ReadJson<MonthlySalesDataModel>(MonthlySalesDataModel.JsonSerializerOptions);

                if (selected != null)
                {
                    var toPublish = MonthlySalesData.MyList[selected.Symbol];

                    try
                    {
                        await _messageRouter.PublishJsonAsync(
                            "proto_select_monthlySales",
                            toPublish,
                            MonthlySalesDataModel.JsonSerializerOptions);
                    }
                    catch (Exception exception)
                    {
                        _logger?.LogError(exception.ToString());
                    }
                }
            }
        }

        private ValueTask OnSymbolSelectionError(Exception exception)
        {
            _logger?.LogError(exception, exception.Message);

            return default;
        }

        public void Dispose()
        {
            foreach (var subscription in _subscriptions)
            {
                subscription.Dispose();
            }
        }

        private class SymbolSelectedModel
        {
            public string Symbol { get; set; } = null!;
        }
    }
}
