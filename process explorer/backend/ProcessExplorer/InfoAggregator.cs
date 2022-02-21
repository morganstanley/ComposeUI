/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using ProcessExplorer.Entities;
using ProcessExplorer.Entities.Connections;
using ProcessExplorer.Entities.EnvironmentVariables;
using ProcessExplorer.Entities.Modules;
using ProcessExplorer.Entities.Registrations;
using System.Net.Http.Headers;

namespace ProcessExplorer
{
    public class InfoAggregator
    {
        public InfoAggregator(Guid id, AppUserInfo user, EnvironmentMonitor envs, ConnectionMonitor cons,
            ProcessMonitor processes)
        {
            Id = id;
            User = user;
            EnvironmentVariables = envs;
            Connections = cons;
            Processses = processes;
        }
        public InfoAggregator(Guid id, AppUserInfo user, EnvironmentMonitor envs, ConnectionMonitor cons, 
            ProcessMonitor processes, RegistrationMonitor registrations, ModuleMonitor modules) { 
            Id = id;
            User = user;
            EnvironmentVariables = envs;
            Connections = cons;
            Processses = processes;
            Registrations = registrations;
            Modules = modules;
        }
        public Guid? Id { get; set; } = default;
        public AppUserInfo User { get; set; }
        public RegistrationMonitor? Registrations { get; set; } = default;
        public EnvironmentMonitor? EnvironmentVariables { get; set; } = default;
        public ConnectionMonitor? Connections { get; set; } = default;
        public ModuleMonitor? Modules { get; set; } = default;
        public ProcessMonitor Processses { get; set; }

        //SAMPLE MESSAGE SENDING?
        public async void SendMessage(string url)
        {
            using(var client = new HttpClient())
            { 
                client.BaseAddress = new Uri(url);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                HttpResponseMessage responseMessage = await client.PostAsJsonAsync("/api/info", this);
                var result = await responseMessage.Content.ReadAsStringAsync();
                if (responseMessage.IsSuccessStatusCode)
                {
                    Uri? infoUrl = responseMessage.Headers.Location;
                    Console.WriteLine(infoUrl);
                }
            }
        }
    }
}
