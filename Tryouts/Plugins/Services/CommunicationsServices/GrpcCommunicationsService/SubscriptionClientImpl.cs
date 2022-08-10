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
using static Subscriptions.SubscriptionsService;

namespace MorganStanley.ComposeUI.Services.CommunicationsServices.GrpcCommunicationsService
{
    [Implements(typeof(ISubscriptionClient), isSingleton:true)]
    public class SubscriptionClientImpl : ISubscriptionClient
    {
        private SubscriptionsServiceClient _client;
        CancellationTokenSource _cts = new CancellationTokenSource();

        public async Task Connect(string host, int port)
        {
            await Task.Delay(5000);
            Channel channel = new Channel(host, port, ChannelCredentials.Insecure);

            _client = new SubscriptionsServiceClient(channel);
        }

        public async Task Publish<TMessage>(Topic topic, TMessage msg)
            where TMessage : IMessage
        {
            PublishRequest publishRequest = new PublishRequest { Topic = topic };

            publishRequest.Message = Any.Pack(msg);

            await _client.PublishAsync(publishRequest);
        }

        public async IAsyncEnumerable<TMessage> Subscribe<TMessage>(Topic topic)
            where TMessage : IMessage, new()
        {
            using var replies =
                _client.Subscribe(new SubscriptionRequest { Topic = topic }, cancellationToken: _cts.Token);

            while(await replies.ResponseStream.MoveNext())
            {
                var msg = replies.ResponseStream.Current;

                TMessage message = msg.Message.Unpack<TMessage>();

                yield return message;
            }
        }
    }
}
