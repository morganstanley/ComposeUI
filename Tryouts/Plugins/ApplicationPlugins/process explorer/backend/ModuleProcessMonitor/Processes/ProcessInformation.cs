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

using System.ComponentModel;
using System.Diagnostics;

namespace ModuleProcessMonitor.Processes;

[Serializable]
public class ProcessInformation
{
    private ProcessInfoData _processInfo = new();
    public ProcessInfoData ProcessInfo
    {
        get
        {
            return _processInfo;
        }
        internal set
        {
            _processInfo = value;
        }
    }

    public ProcessInformation(string name, Guid instanceId, string uiType, string uiHint, int pid)
    {
        ProcessInfo.InstanceId = instanceId;
        ProcessInfo.UiType = uiType;
        ProcessInfo.UiHint = uiHint;
        ProcessInfo.PID = pid;
        ProcessInfo.ProcessName = name;
    }

    public ProcessInformation(Process process)
    {
        ProcessInfo.PID = process.Id;
        ProcessInfo.ProcessName = process.ProcessName;
        ProcessInfo.InstanceId = Guid.NewGuid();
    }

    internal static ProcessInformation GetProcessInfoWithCalculatedData(Process process, ProcessInfoManager processInfoManager)
    {
        var processInformation = new ProcessInformation(process);
        SetProcessInfoData(processInformation, processInfoManager);
        return processInformation;
    }

    internal static void SetProcessInfoData(ProcessInformation processInfo, ProcessInfoManager manager)
    {
        try
        {
            var process = Process.GetProcessById((int)processInfo.ProcessInfo.PID!);
            process.Refresh();

            processInfo._processInfo.PriorityLevel = process.BasePriority;
            processInfo._processInfo.PrivateMemoryUsage = process.PrivateMemorySize64;
            processInfo._processInfo.ParentId = manager.GetParentId(process);
            processInfo._processInfo.MemoryUsage = manager.GetMemoryUsage(process);
            processInfo._processInfo.ProcessorUsage = manager.GetCpuUsage(process);
            processInfo._processInfo.StartTime = process.StartTime.ToString("yyyy.MM.dd. hh:mm:s");
            processInfo._processInfo.ProcessorUsageTime = process.TotalProcessorTime;
            processInfo._processInfo.PhysicalMemoryUsageBit = process.WorkingSet64;
            processInfo._processInfo.ProcessPriorityClass = process.PriorityClass.ToStringCached();
            processInfo._processInfo.VirtualMemorySize = process.VirtualMemorySize64;

            var list = new SynchronizedCollection<ProcessThreadInfo>();

            for (int i = 0; i < process.Threads.Count; i++)
            {
                var thread = ProcessThreadInfo.FromProcessThread(process.Threads[i]);
                if (thread == null) continue;
                list.Add(thread);
            }

            processInfo._processInfo.Threads = list;

            processInfo._processInfo.ProcessStatus =
                process.HasExited == false ?
                    Status.Running.ToStringCached()
                    : Status.Stopped.ToStringCached();
        }
        catch (Exception exception)
        {
            if (exception is Win32Exception || exception is NotSupportedException) return;
            processInfo._processInfo.ProcessStatus = Status.Terminated.ToStringCached();
        }
    }
}
