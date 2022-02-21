/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using ProcessExplorer.Entities.ConfigurationFiles;
using System.Diagnostics;
using System.Reflection;

namespace ProcessExplorer.Entities.Subsystems
{
    public class SubsystemInfo
    {
        public SubsystemInfo(string url, string urlPath, CurrentConfigurations configs = default, bool launchDebugger = false)
        {
            URL = url;
            URLPath = urlPath;
            CurrentConfigurations = configs;
            LaunchDebugger = launchDebugger;
        }

        public ProcessInfo ProcessInfo { get; set; } = new ProcessInfo(Thread.GetCurrentProcessorId());
        public string? VersionOfCompose { get; set; } = Assembly.GetExecutingAssembly()?.GetName().Version?.ToString();
        public string? VersionOfClientApp { get; set; } = Assembly.GetEntryAssembly()?.GetName().Version?.ToString();
        public int? NumberOfThreads { get; set; } = Process.GetProcessById(Thread.GetCurrentProcessorId()).Threads.Count;
        public string? URL { get; set; } 
        public string? URLPath { get; set; }
        public string? ManifestPath { get; set; } = Assembly.GetEntryAssembly()?.ManifestModule.Assembly.Location;
        public CurrentConfigurations CurrentConfigurations { get; set; }
        public bool LaunchDebugger { get; set; } = false;
    }

    public class CurrentSubsystems
    {
        public CurrentSubsystems()
        {
            SubsystemInfos = new List<SubsystemInfo>();
        }
        public List<SubsystemInfo> SubsystemInfos { get; }
    }

    public class SubsystemMonitor: ISubsystemHandler
    {
        private CurrentSubsystems currentSubsystems;

        public SubsystemMonitor()
        {
            currentSubsystems = new CurrentSubsystems();
        }

        public void AddSubsystem(SubsystemInfo subsystem) => currentSubsystems.SubsystemInfos.Add(subsystem);
        public List<SubsystemInfo> GetSubsystems() => currentSubsystems.SubsystemInfos;

        public void ManageSubsystem(SubsystemInfo subsystemInfo, CommandType commandType)
        {
            if(subsystemInfo != default)
            {
                if (commandType == CommandType.Start)
                {
                    //Console.WriteLine("Start subsystem.");
                }
                else if (commandType == CommandType.Stop)
                {
                    //Console.WriteLine("Stop subsystem.");
                }
                else
                {
                    //Console.WriteLine("Restart subsystem");
                }
            }
        }
    }
}
