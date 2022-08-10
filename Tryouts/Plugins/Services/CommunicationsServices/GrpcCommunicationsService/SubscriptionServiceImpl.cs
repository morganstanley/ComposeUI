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
using Subscriptions;
using System.Collections.Concurrent;

namespace MorganStanley.ComposeUI.Services.CommunicationsServices.GrpcCommunicationsService
{
    public class SubscriptionsServiceImpl : SubscriptionsService.SubscriptionsServiceBase
    {
        private ConcurrentDictionary<Topic, SubscriptionData> _topicsDictionary =
            new ConcurrentDictionary<Topic, SubscriptionData>();


        public SubscriptionsServiceImpl(params (Topic Topic, System.Type Type)[] topicToTypeMaps)
        {
            AddTopics(topicToTypeMaps);
        }

        public void AddTopics(params (Topic Topic, System.Type Type)[] topicToTypeMaps)
        {
            foreach (var topicAndType in topicToTypeMaps)
            {
                _topicsDictionary[topicAndType.Topic] =
                    new SubscriptionData(topicAndType.Topic, topicAndType.Type);
            }
        }

        private List<SingleSubscription> /*, SingleSubscription*/ FindSubscription(Topic topic, string pluginId)
        {

            SubscriptionData topicSubscriptions;
            if (!_topicsDictionary.TryGetValue(topic, out topicSubscriptions!))
            {
                // exception or errror - unexisting topic has been requested by the client
            }

            //var existingSubscription =
            //    topicSubscriptions.TopicsSubscriptions.FirstOrDefault(topicSubscription => topicSubscription.PluginId == pluginId);

            return topicSubscriptions.TopicsSubscriptions;/*, existingSubscription!)*/;
        }

        public override async Task Subscribe
        (
            SubscriptionRequest request,
            IServerStreamWriter<ReturnedSubscriptionItem> responseStream,
            ServerCallContext context)
        {
            Topic topic = request.Topic;
            string pluginId = request.PluginId;

            List<SingleSubscription> topicSubscriptions/*, SingleSubscription existingSubscription)*/ =
                FindSubscription(topic, pluginId);

            //if (existingSubscription != null)
            //{
            //    // exception or error - there should be only one subscription per plugin
            //}

            SingleSubscription singleSubscription =
                new SingleSubscription(topic, pluginId, context.CancellationToken);

            topicSubscriptions.Add(singleSubscription);

            while (!context.CancellationToken.IsCancellationRequested)
            {
                Any message =
                    singleSubscription.GetMessage();

                ReturnedSubscriptionItem returningSubscriptionItem = new ReturnedSubscriptionItem
                {
                    DateTimeStamp = Timestamp.FromDateTime(DateTime.UtcNow),
                    Topic = topic,
                    Message = message
                };

                await responseStream.WriteAsync(returningSubscriptionItem);
            }

            // cleanup after cancellation
            topicSubscriptions.Remove(singleSubscription);
        }

        public override async Task<PublishReply> Publish(PublishRequest request, ServerCallContext context)
        {
            Topic topic = request.Topic;

            SubscriptionData? topicSubscriptionData;

            if (!_topicsDictionary.TryGetValue(topic, out topicSubscriptionData))
            {
                // exception or error
            }

            foreach (SingleSubscription singleSubscription in topicSubscriptionData.TopicsSubscriptions)
            {
                singleSubscription.Publish(request.Message);
            }

            return new PublishReply { DateTimeStamp = Timestamp.FromDateTime(DateTime.UtcNow), Topic = topic };
        }

        private class SubscriptionData
        {
            public Topic Topic { get; }
            public System.Type ReturnedItemType { get; }

            internal List<SingleSubscription> TopicsSubscriptions { get; } = new List<SingleSubscription>();

            public SubscriptionData(Topic topic, System.Type returnedItemType)
            {
                Topic = topic;
                ReturnedItemType = returnedItemType;

                if (!typeof(IMessage).IsAssignableFrom(returnedItemType))
                    throw new Exception("Only messages are allowed as topic item types");
            }
        }

        private class SingleSubscription
        {
            BlockingCollection<Any> _subscriptionMessageQueue = new BlockingCollection<Any>();

            public Topic Topic { get; }

            public string PluginId { get; }

            public CancellationToken CancellationToken { get; }

            public SingleSubscription(Topic topic, string pluginId, CancellationToken cancellationToken)
            {
                Topic = topic;
                PluginId = pluginId;
                CancellationToken = cancellationToken;
            }

            public void Publish(Any message)
            {
                _subscriptionMessageQueue.Add(message);
            }

            public Any GetMessage()
            {
                Any message = _subscriptionMessageQueue.Take(CancellationToken);

                return message;
            }
        }

    }


}