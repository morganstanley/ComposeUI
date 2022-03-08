/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using LocalCollector;
using Microsoft.Extensions.Logging;
using ProcessExplorer.Entities;
using ProcessExplorer.Processes;
using System.Collections.Concurrent;

namespace ProcessExplorer
{
    public class InfoCollector : IInfoCollector
    {
        public ILogger<InfoCollector>? logger;
        public ConcurrentDictionary<string, InfoAggregatorDto>? Information { get; set; } = new ConcurrentDictionary<string, InfoAggregatorDto>();
        public IProcessMonitor? ProcessMonitor { get; set; }

        public InfoCollector(ILogger<InfoCollector> logger, IProcessMonitor processMonitor)
        {
            this.logger = logger;
            ProcessMonitor = processMonitor;
        }
        public void AddInformation(string assembly, InfoAggregatorDto info)
            => Information?.AddOrUpdate(assembly, info, (_, _) => info);
        public void Remove(string assembly)
            => Information?.TryRemove(assembly, out _);
        public void SetComposePID(int pid)
            => ProcessMonitor?.SetComposePID(pid);
        public SynchronizedCollection<ProcessInfoDto>? RefreshProcessList()
        {
            var processes = ProcessMonitor?.GetProcesses();
            if (processes != default)
                return processes;
            return default;
        }
        public void SetSubribeUrl(string url)
            =>ProcessMonitor?.SetSubribeUrl(url);
        public SynchronizedCollection<ProcessInfoDto>? GetProcesses()
            => ProcessMonitor?.GetProcesses();
        public void InitProcessExplorer()
            =>  ProcessMonitor?.FillListWithRelatedProcesses();
        public void SetWatcher()
            => ProcessMonitor?.SetWatcher();
        public void SetDelay(int delay)
            => ProcessMonitor?.SetDelay(delay);
    }
}
