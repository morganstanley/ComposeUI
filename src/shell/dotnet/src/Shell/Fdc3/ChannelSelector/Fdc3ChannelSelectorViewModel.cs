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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Finos.Fdc3;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent;
using System.Windows.Media;
using System.ComponentModel;
using System.Threading;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;


namespace MorganStanley.ComposeUI.Shell.Fdc3.ChannelSelector
{
    public class Fdc3ChannelSelectorViewModel : INotifyPropertyChanged, IChannelSelector
    {
        public IChannelSelectorInstanceCommunicator ChannelSelectorInstanceCommunicator;
        public event PropertyChangedEventHandler? PropertyChanged;
        public ObservableCollection<ComposeUI.Fdc3.DesktopAgent.Protocol.ChannelItem> UserChannelCollection { get; } = new();
        private readonly ILogger<ChannelSelectorInstanceCommunicator> _logger;
        private readonly object _disposeLock = new();
        private readonly List<Func<ValueTask>> _disposeTask = new();

        public Fdc3ChannelSelectorViewModel(IChannelSelectorInstanceCommunicator channelSelectorInstanceCommunicator, ObservableCollection<ComposeUI.Fdc3.DesktopAgent.Protocol.ChannelItem> userChannelCollection, string instanceId = "", string color = "Gray" )
        {
            ChannelSelectorInstanceCommunicator = channelSelectorInstanceCommunicator;
            UserChannelCollection = userChannelCollection;

            SetCurrentColor(_currentChannelColor);

            if (color != null)
            {
                var brushColor = GetBrushForColor(color);
                SetCurrentColor(brushColor);
            }
        }

        private Brush GetBrushForColor(string color)
        {
            var myColor = (Color) ColorConverter.ConvertFromString(color);
            SolidColorBrush brush = new SolidColorBrush(myColor);

            return brush;
        }

        private void SetCurrentColor(Brush color)
        {
            CurrentChannelColor = color;
            OnPropertyChanged(nameof(CurrentChannelColor));
        }

        private void SetCurrentChannelColor()
        {
            CurrentChannelColor = _currentChannelColor;
            SetCurrentColor(_currentChannelColor);
        }

        private Brush _currentChannelColor = new SolidColorBrush(Colors.Gray);

         public Brush CurrentChannelColor
         {
             get => _currentChannelColor;
             set
             {
                 _currentChannelColor = value;
                 OnPropertyChanged(nameof(CurrentChannelColor));
             }
         }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Task UpdateChannelSelectorColor(string color)
        {
           var brushColor = GetBrushForColor(color);

            SetCurrentColor(brushColor); 

            return Task.CompletedTask;
        }

        public async Task SendChannelSelectorColorUpdateRequest(ChannelSelectorRequest req, string? color, CancellationToken cancellationToken = default)
        {
            await UpdateChannelSelectorColor(color);
        }

        public async Task<ChannelSelectorResponse?> SendChannelSelectorRequest(string channelId, string instanceId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await SendChannelSelectorRequestCore(channelId, instanceId, cancellationToken);
            }
            catch (TimeoutException ex)
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug(ex, "MessageRouter didn't receive response from the Channel Selector.");
                }

                return new ChannelSelectorResponse()
                {
                    Error = ResolveError.ResolverTimeout
                };
            }
        }

        private async Task<ChannelSelectorResponse?> SendChannelSelectorRequestCore(string channelId, string instanceId, CancellationToken cancellationToken = default)
        {
            var request = new ChannelSelectorRequest
            {
                ChannelId = channelId,
                InstanceId = instanceId
            };

            ChannelSelectorInstanceCommunicator.InvokeChannelSelectorRequest(request, cancellationToken);

            return null;
        }
    }
}
