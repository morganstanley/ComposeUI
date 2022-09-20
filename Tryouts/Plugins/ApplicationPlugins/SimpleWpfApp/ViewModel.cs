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
using System.Text.Json;
using System.Threading;

namespace SimpleWpfApp
{
    public class ViewModel : ViewModelBase
    {
        CancellationTokenSource _cts = new CancellationTokenSource();

        private readonly ISubscriptionClient _subscriptionClient;


        [CompositeConstructor]
        public ViewModel(ISubscriptionClient subscriptionClient)
        {
            StatusText = "Initializing the client";

            _subscriptionClient = subscriptionClient;

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

            var testTopicMessage = new TestTopicMessage { Str = Text };

            await _subscriptionClient.Publish("Test", JsonSerializer.Serialize(testTopicMessage));

            StatusText = $"Sent text: {Text}";
        }
    }

}
