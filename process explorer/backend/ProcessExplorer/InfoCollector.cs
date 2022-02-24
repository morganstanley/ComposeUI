/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using ProcessExplorer.Entities;
using System.Collections.Concurrent;

namespace ProcessExplorer
{
    public static class InfoCollector
    {
        public static ConcurrentDictionary<string, InfoAggregatorDto> Informations { get; set; } = new ConcurrentDictionary<string, InfoAggregatorDto>();
        public static ProcessMonitor ProcessMonitor { get; set; }
        public static void AddInformation(string assembly, InfoAggregatorDto info)
            => Informations.AddOrUpdate(assembly, info, (key, oldValue) => oldValue = info);
        public static void Remove(string assembly)
            => Informations.TryRemove(assembly, out _);
        public static List<ProcessInfoDto> GetProcesses()
        {
            ProcessMonitor = new ProcessMonitor();
            return ProcessMonitor.GetProcesses();
        }
    }
}
