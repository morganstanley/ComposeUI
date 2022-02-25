/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using System.Collections;
using System.Collections.Concurrent;

namespace ProcessExplorer.Entities.EnvironmentVariables
{
    public class EnvironmentMonitor
    {
        public EnvironmentMonitorDto? Data { get; set; }

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

        public EnvironmentMonitor(ConcurrentDictionary<string, string>? environmentVariables)
            => Data.EnvironmentVariables = environmentVariables;
            
        private void LoadEnvironmentVariables()
        {
            foreach (DictionaryEntry item in Environment.GetEnvironmentVariables())
            {
                Data?.EnvironmentVariables?.AddOrUpdate(item.Key.ToString(), item.Value?.ToString(), (key, oldValue) => oldValue = item.Value.ToString());
            }
        }

        private ConcurrentDictionary<string, string>? GetEnvironmentVariables()
        {
            LoadEnvironmentVariables();
            return Data?.EnvironmentVariables;
        }
    }

    public class EnvironmentMonitorDto
    {
        public ConcurrentDictionary<string, string>? EnvironmentVariables { get; set; } = new ConcurrentDictionary<string, string>();
    }
}
