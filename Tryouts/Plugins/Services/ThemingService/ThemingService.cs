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
using System.Windows;

namespace MorganStanley.ComposeUI.Plugins.Services.ThemingService
{
    [Implements(typeof(IThemingService), isSingleton:true)]
    public class ThemingService : ViewModelBase, IThemingService
    {
        private readonly ISubscriptionClient _subscriptionClient;

        public event Action? ThemeChanged;

        [CompositeConstructor]
        public ThemingService(ISubscriptionClient subscriptionClient)
        {
            _subscriptionClient = subscriptionClient;

            _ = ConnectAndSubscribe();
        }

        IDisposable? _subscription;
        private async Task ConnectAndSubscribe()
        {
            await _subscriptionClient.Connect(CommunicationsConstants.MachineName, CommunicationsConstants.Port);

            IObservable<ThemeMessage> observable =
                _subscriptionClient.Subscribe<ThemeMessage>(Topic.Theme)
                .ToObservable();

            _subscription = observable.Subscribe(OnThemeMessageArrived);
        }

        private void OnThemeMessageArrived(ThemeMessage themeMessage)
        {
            Theme = themeMessage.Theme;
        }

        #region Theme Property
        private ThemeId _theme = ThemeId.Light;

        public ThemeId Theme
        {
            get
            {
                return this._theme;
            }
            set
            {
                if (this._theme == value)
                {
                    return;
                }

                this._theme = value;
                this.OnPropertyChanged(nameof(Theme));

                ThemeChanged?.Invoke();

                ChangeTheme();
            }
        }

        private void ChangeTheme()
        {
            ResourceDictionary? currentDictionary =
                Application.Current.Resources.MergedDictionaries.FirstOrDefault(d => d.Source.ToString().EndsWith("Theme.xaml"));

            if (currentDictionary != null)
            {
                Application.Current.Resources.MergedDictionaries.Remove(currentDictionary);
            }

            Application.Current.Resources.MergedDictionaries.Add
            (
                new ResourceDictionary
                {
                    Source = new System.Uri($"/WpfThemeDictionaries;Component/Themes/{Theme}Theme.xaml", System.UriKind.RelativeOrAbsolute)
                }
            );
        }

        public void SetTheme()
        {
            ChangeTheme();
        }
        #endregion Theme Property
    }
}
