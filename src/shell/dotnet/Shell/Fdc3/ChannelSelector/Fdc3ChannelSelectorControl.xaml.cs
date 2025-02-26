/*
 * Morgan Stanley makes this available to you under the Apache License,
 * Version 2.0 (the "License"). You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0.
 *
 * See the NOTICE file distributed with this work for additional information
 * regarding copyright ownership. Unless required by applicable law or agreed
 * to in writing, software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
 * or implied. See the License for the specific language governing permissions
 * and limitations under the License.
 */

using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent;

namespace MorganStanley.ComposeUI.Shell.Fdc3.ChannelSelector
{
    /// <summary>
    /// Interaction logic for Fdc3ChannelSelectorControl.xaml
    /// </summary>
    public partial class Fdc3ChannelSelectorControl : UserControl
    {
        private readonly Fdc3ChannelSelectorViewModel? _viewModel;
        private string _instanceId;
        private ObservableCollection<ComposeUI.Fdc3.DesktopAgent.Protocol.ChannelItem> _userChannelCollection;

        public Fdc3ChannelSelectorControl(IChannelSelectorInstanceCommunicator channelSelectorCommunicator, string color, string instanceId, ObservableCollection<ComposeUI.Fdc3.DesktopAgent.Protocol.ChannelItem> userChannelCollection)
        {
            _viewModel = new Fdc3ChannelSelectorViewModel(channelSelectorCommunicator, userChannelCollection, instanceId, color);
            _instanceId = instanceId;
            _userChannelCollection = userChannelCollection;
            DataContext = _viewModel;

            InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button) sender;
            var channelNumber = (string)btn.Content;
            var color = btn.Background;
            
            await Task.Run(() =>
            {
                var channelId = _userChannelCollection.FirstOrDefault(x => x.DisplayMetadata.Glyph == channelNumber).Id;

                _viewModel.SendChannelSelectorRequest(channelId, _instanceId);
            });
            
            ChannelSelector.BorderBrush = color;
        }

        public async Task ColorUpdate(string color) 
        {
            await _viewModel.UpdateChannelSelectorColor(color);   
        }
    }
}
