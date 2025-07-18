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

namespace MorganStanley.ComposeUI.Messaging.Abstractions;

public interface IMessaging
{
    /// <summary>
    /// Subscribes to a messaging topic. Messages published by OTHER messaging instances will be processed by calling the subscriber delegate. Messages sent by the same messaging instance will be discarded.
    /// </summary>
    /// <param name="topic">The name of the topic to subscribe to</param>
    /// <param name="subscriber">The message handler delegate that is called for every message</param>
    /// <param name="cancellationToken"></param>
    /// <returns>An object that can be disposed to unsubscribe</returns>
    public ValueTask<IAsyncDisposable> SubscribeAsync(string topic, TopicMessageHandler subscriber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a message to a topic.
    /// </summary>
    /// <param name="topic">The name of the topic to publish to</param>
    /// <param name="message">The message to send. Must not be null.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public ValueTask PublishAsync(string topic, string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a service by providing a name and handler. A service will receive a message, process it and optionally returns a message.
    /// </summary>
    /// <param name="serviceName">The name the service can be invoked by. Must be unique across all messaging instances</param>
    /// <param name="serviceHandler">The delegate that handles incoming service invocations</param>
    /// <param name="cancellationToken"></param>
    /// <returns>An object that can be disposed to close the service</returns>
    public ValueTask<IAsyncDisposable> RegisterServiceAsync(string serviceName, ServiceHandler serviceHandler, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invokes a named service.
    /// </summary>
    /// <param name="serviceName">The name of the service registered to Messaging via RegisterServiceAsync</param>
    /// <param name="payload">Data to pass on to the service</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The response from the service</returns>
    public ValueTask<string?> InvokeServiceAsync(string serviceName, string? payload = null, CancellationToken cancellationToken = default);
}