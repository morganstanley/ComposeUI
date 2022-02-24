/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using System.Collections;

namespace ProcessExplorer.Entities.EnvironmentVariables
{
    public class EnvironmentMonitor
    {
        public EnvironmentMonitorDto Data { get; set; }

        EnvironmentMonitor()
            :this(false)
        {

        }

        public EnvironmentMonitor(bool constless = false)
        {
            Data = new EnvironmentMonitorDto();
            if (constless)
            {
                GetEnvironmentVariables();
            }
        }

        public EnvironmentMonitor(List<EnvironmentInfoDto> environmentVariables)
            => this.Data.EnvironmentVariables = environmentVariables;
        public EnvironmentMonitor(ICollection<EnvironmentInfo> environmentVariables)
        {
            foreach (var ev in environmentVariables)
            {
                this.Data.EnvironmentVariables.Add(ev.Data);
            }
        }
            
        private void LoadEnvironmentVariables()
        {
            foreach (DictionaryEntry item in Environment.GetEnvironmentVariables())
            {
                Data.EnvironmentVariables.Add(new EnvironmentInfo(item.Key.ToString(), item.Value?.ToString()).Data);
            }
        }

        private List<EnvironmentInfoDto> GetEnvironmentVariables()
        {
            LoadEnvironmentVariables();
            return Data.EnvironmentVariables;
        }
    }

    public class EnvironmentMonitorDto
    {
        public List<EnvironmentInfoDto>? EnvironmentVariables { get; set; } = new List<EnvironmentInfoDto>();
    }
}
