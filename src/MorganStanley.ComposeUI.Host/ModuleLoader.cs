/*
* Morgan Stanley makes this available to you under the Apache License,
* Version 2.0 (the "License"). You may obtain a copy of the License at
*
*      http://www.apache.org/licenses/LICENSE-2.0.
*
* See the NOTICE file distributed with this work for additional information
* regarding copyright ownership. Unless required by applicable law or agreed
* to in writing, software distributed under the License is distributed on an
* "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
* or implied. See the License for the specific language governing permissions
* and limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ComposeUI.Messaging.Client;
using ComposeUI.Messaging.Client.Transport.WebSocket;
using MorganStanley.ComposeUI.Host.Modules;
using MorganStanley.ComposeUI.Interfaces;

namespace MorganStanley.ComposeUI.Host
{
    internal class ModuleLoader
    {
        private readonly CommunicationModule _communicationModule;
        private readonly List<IModule> _apps = new List<IModule>();

        public ModuleLoader(IEnumerable<IModule> modules, CommunicationModule communicationModule)
        {
            _apps.AddRange(modules);
            _communicationModule = communicationModule;
        }

        public async Task LoadModules()
        {
            await _communicationModule.Initialize(null);
            await InitializeApps();
        }

        private Task InitializeApps()
        {
            var tasks = new List<Task>();
            foreach (var app in _apps)
            {
                var client = MessageRouter.Create(
                     mr => mr.UseWebSocket(
                         new MessageRouterWebSocketOptions
                         {
                             Uri = new Uri("ws://localhost:5000/ws")
                         }));
                tasks.Add(app.Initialize(client));
            }

            return Task.WhenAll(tasks);
        }
    }
}
