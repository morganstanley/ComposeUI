/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using ProcessExplorer.Entities;
using ProcessExplorer.Processes;
using System.Diagnostics;

namespace LocalCollector.Processes
{
    public interface IProcessGenerator
    {
        Action<int>? SendModifiedProcess { get; set; }
        Action<ProcessInfo>? SendNewProcess { get; set; }
        Action<int>? SendTerminatedProcess { get; set; }

        SynchronizedCollection<ProcessInfoDto> GetChildProcesses(Process process);
        float GetCPUUsage(Process process);
        float GetMemoryUsage(Process process);
        int? GetParentId(Process process);
        SynchronizedCollection<int>? GetProcessIds(SynchronizedCollection<ProcessInfoDto> processes);
        bool IsComposeProcess(object process, SynchronizedCollection<int> processes);
        ProcessStartInfo KillProcessById(int processId);
        ProcessStartInfo KillProcessByName(string processName);
        ProcessInfo ProcessCreated(Process process);
        void WatchProcesses(SynchronizedCollection<ProcessInfoDto> processes);
    }
}