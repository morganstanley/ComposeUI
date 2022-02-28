/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using System.Runtime.InteropServices;

namespace ProcessExplorer.Entities.User
{
    public class MachineDto
    {
        #region Properties
        public string? MachineName { get; set; }
        public bool? IsUnix { get; set; }
        public OperatingSystem? OSVersion { get; set; }
        public bool? Is64BIOS { get; set; }
        public bool? Is64BitProcess { get; set; }
        #endregion

        public static MachineDto FromProperties(string machineName, bool isLinux, OperatingSystem system, bool is64BIOS, bool is64BitProcess)
        {
            return new MachineDto()
            {
                MachineName = machineName,
                IsUnix = isLinux,
                OSVersion = system,
                Is64BitProcess = is64BitProcess,
                Is64BIOS = is64BIOS
            };
        }

        public static MachineDto FromMachine()
        {
            var Data = new MachineDto();
            try
            {
                Data.MachineName = Environment.MachineName;
                Data.IsUnix = (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux));
                Data.OSVersion = Environment.OSVersion;
                Data.Is64BIOS = Environment.Is64BitOperatingSystem;
                Data.Is64BitProcess = Environment.Is64BitProcess;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            return Data;
        }
    }
}
