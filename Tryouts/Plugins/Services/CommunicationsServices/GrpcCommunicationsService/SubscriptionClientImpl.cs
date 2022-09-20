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
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MorganStanley.ComposeUI.Tryouts.Core.Abstractions;
using NP.Utilities.Attributes;
using Subscriptions;
using System.Reactive.Linq;
using static Subscriptions.SubscriptionsService;

namespace MorganStanley.ComposeUI.Services.CommunicationsServices.GrpcCommunicationsService
{
    [Implements(typeof(ITypedSubscriptionClient), isSingleton:true)]
    public class SubscriptionClientImpl : ITypedSubscriptionClient
    {
        private SubscriptionsServiceClient _client;
        CancellationTokenSource _cts = new CancellationTokenSource();

        public async Task Connect(string host, int port)
        {
            await Task.Delay(5000);
            Channel channel = new Channel(host, port, ChannelCredentials.Insecure);

            _client = new SubscriptionsServiceClient(channel);
        }

        public async Task Publish(string topic, IMessage msg)
        {
            PublishRequest publishRequest = new PublishRequest { Topic = topic };

            publishRequest.Message = Any.Pack(msg);

            await _client.PublishAsync(publishRequest);
        }

        private async IAsyncEnumerable<IMessage> ConsumeTopicImpl
        (
            AsyncServerStreamingCall<ReturnedSubscriptionItem> replies, 
            System.Type messageType)
        {
            while (await replies.ResponseStream.MoveNext())
            {
                var msg = replies.ResponseStream.Current;

                IMessage message = (IMessage)Activator.CreateInstance(messageType)!;

                message.MergeFrom(msg.Message.Value);

                yield return message;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="messageType">Should have a default constructor and should derive from IMessage!!!</param>
        /// <returns></returns>
        public IObservable<IMessage> 
        ConsumeTopic
        (
            string topic, 
            System.Type messageType)
        {
            var replies =
                 _client.Subscribe(new SubscriptionRequest { Topic = topic }, cancellationToken: _cts.Token);

            IObservable<IMessage> messageStream = ConsumeTopicImpl(replies, messageType).ToObservable();

            return new ObservableWithRefCount<IMessage>(messageStream, replies);
        }


        public IObservable<TMessage>  ConsumeTopic<TMessage>(string topic)
            where TMessage : IMessage, new()
        {
            IObservable<IMessage> messageStream = ConsumeTopic(topic, typeof(TMessage));

            return messageStream.Cast<TMessage>();
        }
    }
}
