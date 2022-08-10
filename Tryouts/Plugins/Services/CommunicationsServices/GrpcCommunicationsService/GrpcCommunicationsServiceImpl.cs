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

using Grpc.Core;
using MorganStanley.ComposeUI.Tryouts.Core.Abstractions;
using NP.Utilities.Attributes;
using Subscriptions;

namespace MorganStanley.ComposeUI.Services.CommunicationsServices.GrpcCommunicationsService
{
    [Implements(typeof(ICommunicationService), isSingleton:true)]
    public class GrpcCommunicationsServiceImpl : ICommunicationService
    {
        private SubscriptionsServiceImpl _subscriptionsService = new SubscriptionsServiceImpl();

        private Server _grpcServer;

        public GrpcCommunicationsServiceImpl()
        {
            _grpcServer = new Server()
            {
                Services =
                {
                    SubscriptionsService.BindService(_subscriptionsService)
                }
            };
        }

        public void AddTopics(params (Topic topic, Type messageType)[] topics)
        {
            _subscriptionsService.AddTopics(topics);
        }

        public void SetHostAndPort(string host, int port)
        {
            _grpcServer.Ports.Add
            (
                host,
                port,
                ServerCredentials.Insecure);
        }

        public void Start()
        {
            _grpcServer.Start();
        }

        public async Task ShutdownAsync()
        {
            await _grpcServer.ShutdownAsync();
        }
    }
}
