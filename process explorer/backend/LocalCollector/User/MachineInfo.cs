
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
                MachineName = Environment.MachineName;
                IsUnix = (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux));
                OSVersion = Environment.OSVersion;
                Is64BIOS = Environment.Is64BitOperatingSystem;
                Is64BitProcess = Environment.Is64BitProcess;
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
        public string MachineName { get; private set; }
        public bool IsUnix { get; private set; }
        public OperatingSystem OSVersion { get; private set; }
        public bool Is64BIOS { get; private set; }
        public bool Is64BitProcess { get; private set; }
      
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
