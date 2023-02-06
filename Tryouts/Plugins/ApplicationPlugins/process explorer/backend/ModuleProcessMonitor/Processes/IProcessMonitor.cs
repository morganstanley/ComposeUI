// Morgan Stanley makes this available to you under the Apache License,
// Version 2.0 (the "License"). You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0.
// 
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership. Unless required by applicable law or agreed
// to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
// or implied. See the License for the specific language governing permissions
// and limitations under the License.

using ProcessExplorer.Abstraction.Handlers;

namespace ProcessExplorer.Abstraction.Processes;

public interface IProcessMonitor : IDisposable
{
    int ComposePid { get; }

    void SetHandlers(
        ProcessModifiedHandler processModifiedHandler,
        ProcessCreatedHandler processCreatedHandler,
        ProcessTerminatedHandler processTerminatedHandler,
        ProcessesModifiedHandler processesModifiedHandler,
        ProcessStatusChangedHandler processStatusChangedHandler);

    /// <summary>
    /// Returns the current list of the processes from the ProcessMonitor.
    /// </summary>
    /// <returns></returns>
    ReadOnlySpan<int> GetProcessIds();

    /// <summary>
    /// Kills a process by ID.
    /// </summary>
    /// <param name="processId"></param>
    void KillProcessById(int processId);

    /// <summary>
    /// Kills a process by name.
    /// </summary>
    /// <param name="processName"></param>
    void KillProcessByName(string processName);

    /// <summary>
    /// Sets the ProcessID for the Compose.
    /// </summary>
    /// <param name="pid"></param>
    void SetComposePid(int pid);

    /// <summary>
    /// Sets the delay for the terminated processes. It represents the deletion/dissapearence of the processes from the UI and from the collection.
    /// </summary>
    /// <param name="delay"></param>
    void SetDeadProcessRemovalDelay(int delay);

    /// <summary>
    /// Sets the ProcessMonitor to watch continuously the created/terminated/modified processes.
    /// </summary>
    void SetWatcher();

    /// <summary>
    /// Initializes the processes.
    /// </summary>
    /// <param name="pids"></param>
    void InitProcesses(ReadOnlySpan<int> pids);
}
