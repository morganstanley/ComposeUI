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

using System.Diagnostics;
using ProcessExplorer.Abstraction.Processes;
using ThreadState = System.Diagnostics.ThreadState;

namespace ModuleProcessMonitor.Processes;

public static class EnumExtensions
{
    public static string ToStringCached(this Status status)
    {
        return status switch
        {
            Status.Terminated => nameof(Status.Terminated),
            Status.Running => nameof(Status.Running),
            Status.Stopped => nameof(Status.Stopped),
            _ => status.ToString(),
        };
    }

    public static string ToStringCached(this ProcessPriorityClass priorityClass)
    {
        return priorityClass switch
        {
            ProcessPriorityClass.Normal => nameof(ProcessPriorityClass.Normal),
            ProcessPriorityClass.BelowNormal => nameof(ProcessPriorityClass.BelowNormal),
            ProcessPriorityClass.RealTime => nameof(ProcessPriorityClass.RealTime),
            ProcessPriorityClass.AboveNormal => nameof(ProcessPriorityClass.AboveNormal),
            ProcessPriorityClass.Idle => nameof(ProcessPriorityClass.Idle),
            ProcessPriorityClass.High => nameof(ProcessPriorityClass.High),
            _ => priorityClass.ToString(),
        };
    }

    public static string ToStringCached(this ThreadState threadState)
    {
        return threadState switch
        {
            ThreadState.Ready => nameof(ThreadState.Ready),
            ThreadState.Terminated => nameof(ThreadState.Terminated),
            ThreadState.Initialized => nameof(ThreadState.Initialized),
            ThreadState.Unknown => nameof(ThreadState.Unknown),
            ThreadState.Transition => nameof(ThreadState.Transition),
            ThreadState.Wait => nameof(ThreadState.Wait),
            ThreadState.Running => nameof(ThreadState.Running),
            ThreadState.Standby => nameof(ThreadState.Standby),
            _ => threadState.ToString(),
        };
    }

    public static string ToStringCached(this ThreadWaitReason waitReason)
    {
        return waitReason switch
        {
            ThreadWaitReason.LpcReceive => nameof(ThreadWaitReason.LpcReceive),
            ThreadWaitReason.LpcReply => nameof(ThreadWaitReason.LpcReply),
            ThreadWaitReason.EventPairHigh => nameof(ThreadWaitReason.EventPairHigh),
            ThreadWaitReason.EventPairLow => nameof(ThreadWaitReason.EventPairLow),
            ThreadWaitReason.ExecutionDelay => nameof(ThreadWaitReason.ExecutionDelay),
            ThreadWaitReason.Executive => nameof(ThreadWaitReason.Executive),
            ThreadWaitReason.FreePage => nameof(ThreadWaitReason.FreePage),
            ThreadWaitReason.PageOut => nameof(ThreadWaitReason.PageOut),
            ThreadWaitReason.PageIn => nameof(ThreadWaitReason.PageIn),
            ThreadWaitReason.Suspended => nameof(ThreadWaitReason.Suspended),
            ThreadWaitReason.SystemAllocation => nameof(ThreadWaitReason.SystemAllocation),
            ThreadWaitReason.UserRequest => nameof(ThreadWaitReason.UserRequest),
            ThreadWaitReason.Unknown => nameof(ThreadWaitReason.Unknown),
            ThreadWaitReason.VirtualMemory => nameof(ThreadWaitReason.VirtualMemory),
            _ => waitReason.ToString(),
        };
    }
}
