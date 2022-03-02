/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using LocalCollector;
using LocalCollector.Modules;
using ProcessExplorer.Entities.Connections;
using ProcessExplorer.Entities.EnvironmentVariables;
using ProcessExplorer.Entities.Registrations;
using System.Diagnostics;

namespace ProcessExplorer
{
    public class InfoAggregator : IInfoAggregator
    {
        //later maybe it will be removed.
        private readonly HttpClient? httpClient;

        InfoAggregator(HttpClient? httpClient = null)
        {
            Data = new InfoAggregatorDto();
            this.httpClient = httpClient;
        }
        public InfoAggregator(Guid id, EnvironmentMonitorDto envs,
            ConnectionMonitor cons, HttpClient? httpClient = null)
            : this(httpClient)
        {
            Data.Id = id;
            Data.EnvironmentVariables = envs;
            Data.Connections = cons.Data;
        }
        public InfoAggregator(Guid id, EnvironmentMonitorDto envs, ConnectionMonitor cons,
            RegistrationMonitorDto registrations, ModuleMonitorDto modules, HttpClient? httpClient = null)
            : this(id, envs, cons, httpClient)
        {
            Data.Registrations = registrations;
            Data.Modules = modules;
        }

        public InfoAggregatorDto Data { get; set; }

        //SAMPLE MESSAGE SENDING
        public async Task SendMessage(string url)
        {
            if (httpClient != default)
            {
                HttpResponseMessage responseMessage = await httpClient.PostAsJsonAsync(url, this.Data);
                var result = await responseMessage.Content.ReadAsStringAsync();
                Debug.WriteLine(result);
            }
        }
    }
}
