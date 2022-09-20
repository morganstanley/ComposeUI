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

using Google.Protobuf;
using Microsoft.Web.WebView2.WinForms;
using MorganStanley.ComposeUI.Tryouts.Core.Abstractions;
using Subscriptions;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace MorganStanley.ComposeUI.Tryouts.Visuals.Windows.VisualUtils
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class JSCommunicationsClient 
    {
        private ISubscriptionClient _subscriptionClient;
        private WebView2 _webView;
        public JSCommunicationsClient
        (
            ISubscriptionClient subscriptionClient, 
            WebView2 webView2, 
            IDictionary<string, Type>? topicToMessageTypeConverter = null)
        {
            _subscriptionClient = subscriptionClient;
            _webView = webView2;
        }

        public async void Publish(string topic, object testTopicMessage)
        {
            await _subscriptionClient.Publish(topic, (string)testTopicMessage);
        }

        private Dictionary<string, IDisposable> _subscriptions = new Dictionary<string, IDisposable>();

        public void Subscribe(string topic)
        {
            if (_subscriptions.ContainsKey(topic))
            {
                return; // subscriptions already exists
            }

            var observable =
                _subscriptionClient
                    .ConsumeTopic(topic);

            IDisposable disposable = observable.Subscribe(OnMessageArrived);

            _subscriptions.Add(topic, disposable);
        }


        public void Unsubscribe(string topic)
        {
            if (_subscriptions.ContainsKey(topic))
            {
                IDisposable disposable = _subscriptions[topic];

                disposable.Dispose();

                _subscriptions.Remove(topic);
            }
        }

        private void OnMessageArrived(CommunicationsMessage msg)
        {
            _webView.CoreWebView2.PostWebMessageAsJson(msg.Info);
        }
    }
}
