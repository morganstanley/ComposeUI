/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using ProcessExplorer.Entities;
using ProcessExplorer.Processes;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace LocalCollector.Processes
{
    public interface IProcessGenerator
    {
        Action<int>? SendModifiedProcess { get; set; }
        Action<ProcessInfo>? SendNewProcess { get; set; }
        Action<int>? SendTerminatedProcess { get; set; }

        /// <summary>
        /// Returns the a list conatining the child processes of the given process.
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        SynchronizedCollection<ProcessInfoDto> GetChildProcesses(Process process);

        /// <summary>
        /// Adds the childs of the main process to the list.
        /// </summary>
        void AddChildProcessesToList();

        /// <summary>
        /// Returns the CPU usage of the goven process.
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        float GetCPUUsage(Process process);

        /// <summary>
        /// Returns the memory usage of the given process.
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        float GetMemoryUsage(Process process);

        /// <summary>
        /// Returns the PPID of the given process.
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        int? GetParentId(Process process);

        /// <summary>
        /// Returns a list containing the PID's.
        /// </summary>
        /// <param name="processes"></param>
        /// <returns></returns>
        //SynchronizedCollection<int>? GetProcessIds(SynchronizedCollection<ProcessInfoDto> processes);
        ConcurrentDictionary<int, byte[]>? GetProcessIds(SynchronizedCollection<ProcessInfoDto> processes);

        /// <summary>
        /// Returns if the process is a Compose process.
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        bool IsComposeProcess(object process);

        /// <summary>
        /// Terminates a process by ID.
        /// </summary>
        /// <param name="processId"></param>
        /// <returns></returns>
        ProcessStartInfo KillProcessById(int processId);
        
        /// <summary>
        /// Terminates a process by name
        /// </summary>
        /// <param name="processName"></param>
        /// <returns></returns>
        ProcessStartInfo KillProcessByName(string processName);
        ProcessInfo ProcessCreated(Process process);

        /// <summary>
        /// Sets the events for creating/terminating/modifying a process.
        /// </summary>
        /// <param name="processes"></param>
        void WatchProcesses(SynchronizedCollection<ProcessInfoDto> processes);
    }
}