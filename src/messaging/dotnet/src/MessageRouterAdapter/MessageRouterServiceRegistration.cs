// Morgan Stanley makes this available to you under the Apache License,
// Version 2.0 (the "License"). You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0.
// 
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership. Unless required by applicable law or agreed
// to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
// or implied. See the License for the specific language governing permissions
// and limitations under the License.


using MorganStanley.ComposeUI.Messaging.Abstractions;

namespace MorganStanley.ComposeUI.MessagingAdapter.MessageRouter;

internal class MessageRouterServiceRegistration(string serviceName, IMessageRouter messageRouter) : IAsyncDisposable
{
    private int _disposed = 0;
    private readonly string _serviceName = serviceName;
    private readonly IMessageRouter _messageRouter = messageRouter;


    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) > 0)
        {
            return;
        }
        await _messageRouter.UnregisterServiceAsync(_serviceName);
    }
}
