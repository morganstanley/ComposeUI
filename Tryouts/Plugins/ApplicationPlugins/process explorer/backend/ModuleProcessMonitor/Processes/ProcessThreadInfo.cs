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
using ThreadState = System.Diagnostics.ThreadState;

namespace ModuleProcessMonitor.Processes;

public class ProcessThreadInfo
{
    public string? StartTime { get; internal set; }
    public int? PriorityLevel { get; internal set; }
    public int? Id { get; internal set; }
    public string? Status { get; internal set; }
    public TimeSpan? ProcessorUsageTime { get; internal set; }
    public string? WaitReason { get; internal set; }

    internal static ProcessThreadInfo? FromProcessThread(ProcessThread processThread)
    {
        var result = new ProcessThreadInfo();
        try
        {
            if (processThread.ThreadState == ThreadState.Wait)
            {
                result.WaitReason = processThread.WaitReason.ToStringCached();
            }
            result.StartTime = processThread.StartTime.ToString("yyyy.MM.dd. hh:mm:s");
            result.ProcessorUsageTime = processThread.TotalProcessorTime;
        }
        //ProcessThread could raise 2 exceptions: Win32(thread time could not be retrieved) and NotSupported(computer is on remote computer)
        catch (Win32Exception)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
        finally
        {
            result.PriorityLevel = processThread.CurrentPriority;
            result.Id = processThread.Id;
            result.Status = processThread.ThreadState.ToStringCached();
        }

        return result;
    }
}
