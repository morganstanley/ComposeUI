/// ********************************************************************************************************
///
/// Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License").
/// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0.
/// See the NOTICE file distributed with this work for additional information regarding copyright ownership.
/// Unless required by applicable law or agreed to in writing, software distributed under the License
/// is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
/// See the License for the specific language governing permissions and limitations under the License.
/// 
/// ********************************************************************************************************

using MorganStanley.ComposeUI.Tryouts.Core.Abstractions;
using MorganStanley.ComposeUI.Tryouts.Core.Utilities;
using NP.Utilities.Attributes;
using Subscriptions;
using System;
using System.Linq;
using System.Text.Json;
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

            IObservable<CommunicationsMessage> observable =_subscriptionClient.ConsumeTopic("Test");

            _subscription = observable.Subscribe(OnTestTopicMessageArrived);
        }

        private void OnTestTopicMessageArrived(CommunicationsMessage message)
        {
            string json = message.Info; 

            TestTopicMessage msg = JsonSerializer.Deserialize<TestTopicMessage>(json)!;

            ReceivedText = msg.Str;
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
