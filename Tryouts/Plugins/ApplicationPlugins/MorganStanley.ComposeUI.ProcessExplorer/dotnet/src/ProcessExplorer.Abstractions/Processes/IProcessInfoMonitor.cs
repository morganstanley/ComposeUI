﻿// Morgan Stanley makes this available to you under the Apache License,
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

using ProcessExplorer.Abstractions.Handlers;

namespace ProcessExplorer.Abstractions.Processes;

//TODO(Lilla): should be async?
public interface IProcessInfoMonitor : IDisposable
{
    /// <summary>
    /// Clears the currently initilialzed processes.
    /// </summary>
    void ClearProcessIds();

    /// <summary>
    /// Returns the CPU usage of the given process.
    /// </summary>
    /// <param name="processId"></param>
    /// <param name="processName"></param>
    /// <returns></returns>
    float GetCpuUsage(int processId, string processName);

    /// <summary>
    /// Returns the memory usage of the given process.
    /// </summary>
    /// <param name="processId"></param>
    /// <param name="processName"></param>
    /// <returns></returns>
    float GetMemoryUsage(int processId, string processName);

    /// <summary>
    /// Returns the parent id of the given process.
    /// </summary>
    /// <param name="processId"></param>
    /// <param name="processName"></param>
    /// <returns></returns>
    int? GetParentId(int processId, string processName);

    /// <summary>
    /// Returns the initialized/watched process ids.
    /// </summary>
    /// <returns></returns>
    ReadOnlySpan<int> GetProcessIds();

    /// <summary>
    /// Sets the behaviors of the process changed events.
    /// </summary>
    /// <param name="processModifiedHandler"></param>
    /// <param name="processTerminatedHandler"></param>
    /// <param name="processCreatedHandler"></param>
    void SetHandlers(
        ProcessModifiedHandler processModifiedHandler, 
        ProcessTerminatedHandler processTerminatedHandler,
        ProcessCreatedHandler processCreatedHandler, 
        ProcessesModifiedHandler processesModifiedHandler, 
        ProcessStatusChangedHandler processStatusChangedHandler);

    /// <summary>
    /// Sets the watchable process list.
    /// </summary>
    /// <param name="mainProcessId"></param>
    /// <param name="processIds"></param>
    void SetProcessIds(int mainProcessId, ReadOnlySpan<int> processIds);

    /// <summary>
    /// Enables watching processes.
    /// </summary>
    /// <param name="mainProcessId"></param>
    void WatchProcesses(int mainProcessId);
}
