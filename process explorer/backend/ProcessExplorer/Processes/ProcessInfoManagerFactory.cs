/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using LocalCollector.Processes;
using ProcessExplorer.Entities;
using System.Runtime.InteropServices;

namespace ProcessExplorer.Processes
{
    public static class ProcessInfoManagerFactory
    {
        private static ProcessGeneratorBase? processInfoManager;
        public static ProcessGeneratorBase? SetProcessInfoGeneratorBasedOnOS(Action<ProcessInfo> SendNewProcess, Action<int> SendTerminatedProcess, Action<int> SendModifiedProcess)
        {
            if ((RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux)))
                processInfoManager = new ProcessInfoLinux(SendNewProcess, SendTerminatedProcess, SendModifiedProcess);
            else
                processInfoManager = new ProcessInfoWindows(SendNewProcess, SendTerminatedProcess, SendModifiedProcess);
            return processInfoManager;
        }
    }
}
