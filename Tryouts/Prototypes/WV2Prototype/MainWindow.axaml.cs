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

using Avalonia.Controls;
using MorganStanley.ComposeUI.Tryouts.Visuals.Windows.VisualUtils;
using Subscriptions;
using System;
using System.Collections.Generic;
using System.IO;

namespace MorganStanley.ComposeUI.Prototypes.WV2Prototype
{
    public partial class MainWindow : Window
    {
        private JSCommunicationsClient _publishClient;
        private JSCommunicationsClient _subscriptionClient;

        public MainWindow()
        {
            InitializeComponent();

            this.Closed += MainWindow_Closed;

            _publishClient =
                new JSCommunicationsClient
                (
                    ((App)App.Current!).SubscriptionClient, 
                    PublishingWebView.WebView,
                    new Dictionary<string, System.Type>{
                        {"Test", typeof(StringContainingMessage) }
                    }    
                );

            _subscriptionClient =
                new JSCommunicationsClient(((App)App.Current!).SubscriptionClient, SubscribingWebView.WebView);

            PublishingWebView.CommunicationsObjects = new Dictionary<string, object>
            {
                { "JavaScriptCommunicationsClient", _publishClient }
            };

            SubscribingWebView.CommunicationsObjects = new Dictionary<string, object>
            {
                { "JavaScriptCommunicationsClient", _subscriptionClient },
            };
        }

        private void MainWindow_Closed(object? sender, System.EventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
