
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace ProcessExplorer.Entities.EnvironmentVariables
{
    public class MachineInfo
    {
        public MachineInfo() 
        {
            try
            {
                //BytesAllocatedByGC = GC.GetTotalMemory(true);
                MachineName = Environment.MachineName;
                IsUnix = (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux));
                OSVersion = Environment.OSVersion;
                Is64BIOS = Environment.Is64BitOperatingSystem;
                Is64BitProcess = Environment.Is64BitProcess;

                //if (IsUnix)
                //{
                //    GetMemoryValuesLinux();
                //}
                //else
                //{
                //    TotalMemory = GetTotalRAMWindows();
                //    FreeMemory = (long)new PerformanceCounter("Memory", "Available KiloBytes").NextValue();
                //}
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public MachineInfo(string machineName, bool isLinux, OperatingSystem system, bool is64BIOS, bool is64BitProcess)
        {
            MachineName = machineName;
            IsUnix = isLinux;
            OSVersion = system;
            Is64BIOS = is64BIOS;
            Is64BitProcess = is64BitProcess;
        }

        //public long? BytesAllocatedByGC { get; private set; }
        public string MachineName { get; private set; }
        public bool IsUnix { get; private set; }
        public OperatingSystem OSVersion { get; private set; }
        public bool Is64BIOS { get; private set; }
        public bool Is64BitProcess { get; private set; }
        //public CpuUsage? CPUUsageByHttpRequest { get; private set; }
        //public long TotalMemory { get; private set; }
        //public long FreeMemory { get; private set; }

        //private void GetMemoryValuesLinux()
        //{
        //    string[] memInfoLines = File.ReadAllLines(@"/proc/meminfo");
        //    MemoryInformation[] memInfoMatches =
        //    {
        //        new MemoryInformation(@"^MemFree:\s+(\d+)", value => FreeMemory = Convert.ToInt64(value)),
        //        new MemoryInformation(@"^MemTotal:\s+(\d+)", value => TotalMemory = Convert.ToInt64(value))
        //    };

        //    foreach (string memInfoLine in memInfoLines)
        //    {
        //        foreach (MemoryInformation memInfoMatch in memInfoMatches)
        //        {
        //            Match match = memInfoMatch.regex.Match(memInfoLine);
        //            if (match.Groups[1].Success)
        //            {
        //                string value = match.Groups[1].Value;
        //                memInfoMatch.updateValue(value);
        //            }
        //        }
        //    }
        //}

        private long GetTotalRAMWindows()
        {
            long memoryB;
            GetPhysicallyInstalledSystemMemory(out memoryB);
            return memoryB;
        }

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetPhysicallyInstalledSystemMemory(out long TotalMemoryInKilobytes);

    }
    public class MemoryInformation
    {
        public Regex regex;
        public Action<string> updateValue;

        public MemoryInformation(string pattern, Action<string> updateValue)
        {
            this.regex = new Regex(pattern, RegexOptions.Compiled);
            this.updateValue = updateValue;
        }
    }
}
