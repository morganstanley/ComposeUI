using MorganStanley.ComposeUI.Tryouts.Core.Abstractions;
using MorganStanley.ComposeUI.Tryouts.Core.Utilities;
using Subscriptions;
using System.Threading;

namespace SimpleWpfApp
{
    public class ViewModel : ViewModelBase
    {
        CancellationTokenSource _cts = new CancellationTokenSource();

        private readonly ISubscriptionClient _subscriptionClient;


        public ViewModel()
        {
            StatusText = "Initializing the client";

            _subscriptionClient = ((App)App.Current).Container.Resolve<ISubscriptionClient>();

            _ = _subscriptionClient.Connect(CommunicationsConstants.MachineName, CommunicationsConstants.Port);

            StatusText = $"Initialized client connection to '{CommunicationsConstants.ConnectionStr}'";
        }


        #region Text Property
        private string? _text;
        public string? Text
        {
            get
            {
                return this._text;
            }
            set
            {
                if (this._text == value)
                {
                    return;
                }

                this._text = value;
                this.OnPropertyChanged(nameof(Text));
            }
        }
        #endregion Text Property


        #region StatusText Property
        private string _statusText;
        public string StatusText
        {
            get
            {
                return this._statusText;
            }
            set
            {
                if (this._statusText == value)
                {
                    return;
                }

                this._statusText = value;
                this.OnPropertyChanged(nameof(StatusText));
            }
        }
        #endregion StatusText Property


        public async void SendText()
        {
            StatusText = $"Calling SendText() method";

            await _subscriptionClient.Publish(Topic.Test, new TestTopicMessage { Str = Text });

            StatusText = $"Sent text: {Text}";
        }
    }

}
