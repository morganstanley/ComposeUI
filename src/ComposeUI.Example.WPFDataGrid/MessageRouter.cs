/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using System;
using System.Threading;
using System.Threading.Tasks;
using ComposeUI.Messaging.Client;

namespace ComposeUI.Example.WPFDataGrid
{
    //TO DO
    internal class MessageRouter : IMessageRouter
    {
        public ValueTask DisposeAsync()
        {
            throw new NotImplementedException();
        }

        public ValueTask PublishAsync(string topicName, byte[]? payload = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public ValueTask RegisterServiceAsync(string serviceName, ServiceInvokeHandler handler, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public ValueTask<IDisposable> SubscribeAsync(string topicName, IObserver<RouterMessage> observer, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public ValueTask UnregisterServiceAsync(string serviceName, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
