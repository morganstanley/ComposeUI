using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ComposeUI.Messaging.Client;
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

            if (payload != null)
            {
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
}
