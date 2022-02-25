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
        InfoAggregator()
        {
            Data = new InfoAggregatorDto();
        }
        public InfoAggregator(Guid id, AppUserInfoDto user, EnvironmentMonitor envs, ConnectionMonitor cons)
            :this()
        {
            Data.Id = id;
            Data.User = user;
            Data.EnvironmentVariables = envs.Data;
            Data.Connections = cons.Data;
        }
        public InfoAggregator(Guid id, AppUserInfoDto user, EnvironmentMonitor envs, ConnectionMonitor cons,
            RegistrationMonitorDto registrations, ModuleMonitorDto modules)
            : this(id, user, envs, cons)
        {
            Data.Registrations = registrations;
            Data.Modules = modules;
        }
        
        public InfoAggregatorDto Data { get; set; }

        //SAMPLE MESSAGE SENDING?
        private async Task SendMessage(string url)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(url);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                HttpResponseMessage responseMessage = await client.PostAsJsonAsync("", this.Data);
                var result = await responseMessage.Content.ReadAsStringAsync();
                if (responseMessage.IsSuccessStatusCode)
                {
                    Uri? infoUrl = responseMessage.Headers.Location;
                    Console.WriteLine(infoUrl);
                }
            }
        }
        public void Send(string url)
        {
            var task = SendMessage(url);
            task.Wait();
        }
    }

    public class InfoAggregatorDto
    {
        public Guid? Id { get; set; }
        public AppUserInfoDto? User { get; set; } 
        public RegistrationMonitorDto? Registrations { get; set; }
        public EnvironmentMonitorDto? EnvironmentVariables { get; set; }
        public ConnectionMonitorDto? Connections { get; set; } 
        public ModuleMonitorDto? Modules { get; set; } 
    }
}
