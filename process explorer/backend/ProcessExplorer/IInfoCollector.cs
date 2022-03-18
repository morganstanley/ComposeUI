/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using LocalCollector;
using ProcessExplorer.Entities;
using ProcessExplorer.Processes;
using System.Collections.Concurrent;

namespace ProcessExplorer
{
    public interface IInfoCollector
    {
        #region Properties
        /// <summary>
        /// Contains information.
        /// (connection/registrations/modules/environment variables)
        /// </summary>
        public ConcurrentDictionary<string, InfoAggregatorDto>? Information { get; set; }

        /// <summary>
        /// Contains the information about the processes.
        /// </summary>
        public IProcessMonitor? ProcessMonitor { get; set; }
        #endregion

        #region Methods
        /// <summary>
        /// Adds a module information to the collection.
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="info"></param>
        public void AddInformation(string assembly, InfoAggregatorDto info);

        /// <summary>
        /// Removes a module information to the collection.
        /// </summary>
        /// <param name="assembly"></param>
        public void Remove(string assembly);

        /// <summary>
        /// Sets Compose PID.
        /// </summary>
        /// <param name="pid"></param>
        public void SetComposePID(int pid);

        /// <summary>
        /// Sets the delay time for keeping a process after it was terminated.(s)
        /// Default: 1 minute.
        /// </summary>
        /// <param name="delay"></param>
        public void SetDeadProcessRemovalDelay(int delay);

        /// <summary>
        /// Reinitializes the list conatining the current, relevant processes
        /// </summary>
        /// <returns>A collection</returns>
        public SynchronizedCollection<ProcessInfoDto>? RefreshProcessList();

        /// <summary>
        /// Returns the list containing the processes.
        /// </summary>
        /// <returns></returns>
        public SynchronizedCollection<ProcessInfoDto>? GetProcesses();

        /// <summary>
        /// Fills the list.
        /// </summary>
        public void InitProcessExplorer();

        /// <summary>
        /// Sets the url, where the new information can be pushed continouosly.
        /// </summary>
        /// <param name="url"></param>
        public void SetSubribeUrl(string url);

        /// <summary>
        /// Initalizes the process creator/modifier/terminator events.
        /// </summary>
        public void SetWatcher();
        #endregion
    }
}
