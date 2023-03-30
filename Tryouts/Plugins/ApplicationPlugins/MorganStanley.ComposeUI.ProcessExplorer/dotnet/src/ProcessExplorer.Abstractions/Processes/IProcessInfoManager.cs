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

using ProcessExplorer.Abstractions.Handlers;

namespace ProcessExplorer.Abstractions.Processes;

//TODO(Lilla): Add description
public interface IProcessInfoManager : IDisposable
{
    ReadOnlySpan<int> AddChildProcesses(int processId, string? processName);
    void AddProcess(int processId);
    bool CheckIfIsComposeProcess(int processId);
    void ClearProcessIds();
    bool ContainsId(int processId);
    float GetCpuUsage(int processId, string processName);
    float GetMemoryUsage(int processId, string processName);
    int? GetParentId(int processId, string processName);
    ReadOnlySpan<int> GetProcessIds();
    void RemoveProcessId(int processId);
    void SendNewProcessUpdate(int processId);
    void SendProcessModifiedUpdate(int processId);
    void SendTerminatedProcessUpdate(int processId);
    void SetHandlers(
        ProcessModifiedHandler processModifiedHandler, 
        ProcessTerminatedHandler processTerminatedHandler,
        ProcessCreatedHandler processCreatedHandler, 
        ProcessesModifiedHandler processesModifiedHandler, 
        ProcessStatusChangedHandler processStatusChangedHandler);
    void SetProcessIds(int mainProcessId, ReadOnlySpan<int> processIds);
    void WatchProcesses(int mainProcessId);
}
