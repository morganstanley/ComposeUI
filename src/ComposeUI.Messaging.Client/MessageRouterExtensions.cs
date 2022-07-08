// /*
//  * Morgan Stanley makes this available to you under the Apache License,
//  * Version 2.0 (the "License"). You may obtain a copy of the License at
//  *
//  *      http://www.apache.org/licenses/LICENSE-2.0.
//  *
//  * See the NOTICE file distributed with this work for additional information
//  * regarding copyright ownership. Unless required by applicable law or agreed
//  * to in writing, software distributed under the License is distributed on an
//  * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
//  * or implied. See the License for the specific language governing permissions
//  * and limitations under the License.
//  */

using System.Reactive;

namespace ComposeUI.Messaging.Client;

public static class MessageRouterExtensions
{
    public static ValueTask<IDisposable> SubscribeAsync(
        this IMessageRouter messageRouter,
        string topicName,
        IObserver<string?> observer,
        CancellationToken cancellationToken = default)
    {
        var innerObserver = Observer.Create<RouterMessage>(
            message => observer.OnNext(message.Payload),
            observer.OnError,
            observer.OnCompleted);

        return messageRouter.SubscribeAsync(topicName, innerObserver, cancellationToken);
    }
}