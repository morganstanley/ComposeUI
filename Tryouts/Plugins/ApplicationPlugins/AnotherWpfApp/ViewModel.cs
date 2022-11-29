using MorganStanley.ComposeUI.Tryouts.Core.Abstractions;
using MorganStanley.ComposeUI.Tryouts.Core.Utilities;
using NP.Utilities.Attributes;
using Subscriptions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AnotherWpfApp
{
    public class ViewModel : ViewModelBase
    {
        CancellationTokenSource? _cts = new CancellationTokenSource();

        private readonly ISubscriptionClient _subscriptionClient;

        IDisposable? _subscription;
        [CompositeConstructor]
        public ViewModel(ISubscriptionClient subscriptionClient)
        {
            _subscriptionClient = subscriptionClient;

            ReceivedText = "Initial Text";

            _ = ConnectAndSubscribe();
        }

        private async Task ConnectAndSubscribe()
        {
            await _subscriptionClient.Connect(CommunicationsConstants.MachineName, CommunicationsConstants.Port);

            IObservable<TestTopicMessage> observable =
                _subscriptionClient.Subscribe<TestTopicMessage>(Topic.Test)
                .ToObservable();

            _subscription = observable.Subscribe(OnTestTopicMessageArrived);
        }

        private void OnTestTopicMessageArrived(TestTopicMessage testTopicMessage)
        {
            ReceivedText = testTopicMessage.Str;
        }

        #region ReceivedText Property
        private string? _receivedText;
        public string? ReceivedText
        {
            get
            {
                return this._receivedText;
            }
            set
            {
                if (this._receivedText == value)
                {
                    return;
                }

                this._receivedText = value;
                this.OnPropertyChanged(nameof(ReceivedText));
            }
        }
        #endregion ReceivedText Property
    }
}
