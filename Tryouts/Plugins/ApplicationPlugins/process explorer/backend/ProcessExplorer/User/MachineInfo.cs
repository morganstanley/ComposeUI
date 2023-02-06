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

using System.Runtime.InteropServices;

namespace ProcessExplorer.Core.User;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public class MachineInfo
{
    public string? MachineName { get; internal set; }
    public bool? IsUnix { get; internal set; }
    public OperatingSystem? OSVersion { get; internal set; }
    public bool? Is64BIOS { get; internal set; }
    public bool? Is64BitProcess { get; internal set; }
    public long? TotalRAM { get; internal set; }
    public double? AvailableRAM { get; internal set; }

    public static MachineInfo FromMachine()
    {
        var Data = new MachineInfo();

        Data.MachineName = Environment.MachineName;
        Data.IsUnix = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        Data.OSVersion = Environment.OSVersion;
        Data.Is64BIOS = Environment.Is64BitOperatingSystem;
        Data.Is64BitProcess = Environment.Is64BitProcess;
        Data.TotalRAM = GetTotalRAM();
        Data.AvailableRAM = GetAvailableRAM();
        return Data;
    }

    private static long GetTotalRAM()
    {
        long memKb;
        GetPhysicallyInstalledSystemMemory(out memKb);
        return memKb / 1024 / 1024;
    }

    private static double? GetAvailableRAM()
    {
        var performance = new System.Diagnostics.PerformanceCounter("Memory", "Available MBytes");
        var memory = performance.NextValue();

        return Convert.ToDouble(memory) / 1024;
    }

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool GetPhysicallyInstalledSystemMemory(out long TotalMemoryInKilobytes);
}
