/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using System.Diagnostics;

namespace ProcessExplorer.Processes
{
    internal static class EnumExtensions
    {
        internal static string ToStringCached(this Status status)
        {
            return status switch
            {
                Status.Running => nameof(Status.Running),
                Status.Stopped => nameof(Status.Stopped),
                _ => status.ToString(),
            };
        }

        internal static string ToStringCached(this ProcessPriorityClass priorityClass)
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
    }
}
