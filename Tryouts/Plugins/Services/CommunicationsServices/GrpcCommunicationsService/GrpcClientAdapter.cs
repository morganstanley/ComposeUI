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

using MorganStanley.ComposeUI.Tryouts.Core.Abstractions;
using NP.Utilities.Attributes;
using Subscriptions;
using System.Reactive.Linq;

namespace MorganStanley.ComposeUI.Services.CommunicationsServices.GrpcCommunicationsService
{
    [Implements(typeof(ISubscriptionClient), IsSingleton = true)]
    public class GrpcSubscriptionClientAdapter : ISubscriptionClient
    {
        private ITypedSubscriptionClient _subscriptionClient;

        [CompositeConstructor]
        public GrpcSubscriptionClientAdapter(ITypedSubscriptionClient typedSubscriptionClient)
        {
            _subscriptionClient = typedSubscriptionClient;
        }

        public Task Connect(string host, int port)
        {
            return _subscriptionClient.Connect(host, port);
        }

        public Task Publish(string topic, string msg)
        {
            return _subscriptionClient.Publish(topic, new StringContainingMessage { Str = msg });
        }

        public IObservable<CommunicationsMessage> ConsumeTopic(string topic)
        {
            IObservable<StringContainingMessage> grpcMessageStream =
                _subscriptionClient.ConsumeTopic<StringContainingMessage>(topic);

            return grpcMessageStream.Select(msg => new CommunicationsMessage(topic, msg.Str));
        }
    }
}
