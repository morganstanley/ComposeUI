/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

namespace ProcessExplorer.Processes
{
  public interface IProcessMonitor
  {
    /// <summary>
    /// List of processes. Contains the relevant infomration of processes to the Compose.
    /// </summary>
    ProcessMonitorInfo Data { get; }

    /// <summary>
    /// Event if a process has been terminated.
    /// </summary>
    event EventHandler<int> processTerminatedAction;

    /// <summary>
    /// Event if a process has been created.
    /// </summary>
    event EventHandler<ProcessInfoData> processCreatedAction;

    /// <summary>
    /// Event if a process has been modified.
    /// </summary>
    event EventHandler<ProcessInfoData> processModifiedAction;

    /// <summary>
    /// Event if a list of processes has been modified.
    /// </summary>
    event EventHandler<SynchronizedCollection<ProcessInfoData>> processesModifiedAction;

    /// <summary>
    /// Returns the current list of the processes from the ProcessMonitor.
    /// </summary>
    /// <returns></returns>
    SynchronizedCollection<ProcessInfoData>? GetProcesses();

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
    void SetComposePID(int pid);

    /// <summary>
    /// Sets the delay for the terminated processes. It represents the deletion/dissapearence of the processes from the UI and from the collection.
    /// </summary>
    /// <param name="delay"></param>
    void SetDeadProcessRemovalDelay(int delay);

    /// <summary>
    /// Fills the collection of the ProcessMonitor with related processes to the Compose.
    /// </summary>
    void FillListWithRelatedProcesses();

    /// <summary>
    /// Sets the ProcessMonitor to watch continuously the created/terminated/modified processes.
    /// </summary>
    void SetWatcher();
  }
}
