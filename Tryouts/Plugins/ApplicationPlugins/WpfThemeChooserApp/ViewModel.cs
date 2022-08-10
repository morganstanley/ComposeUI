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
using Subscriptions;
using System.Threading;

namespace WpfThemeChooserApp
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


        #region SelectedTheme Property
        private ThemeId _selectedTheme = ThemeId.Light;
        public ThemeId SelectedTheme
        {
            get
            {
                return this._selectedTheme;
            }
            set
            {
                if (this._selectedTheme == value)
                {
                    return;
                }

                this._selectedTheme = value;
                this.OnPropertyChanged(nameof(SelectedTheme));

                SendTheme();
            }
        }
        #endregion SelectedTheme Property



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


        public async void SendTheme()
        {
            StatusText = $"Calling SendTheme() method";

            await _subscriptionClient.Publish(Topic.Theme, new ThemeMessage { Theme = SelectedTheme});

            StatusText = $"Sent theme: {SelectedTheme}";
        }
    }

}
