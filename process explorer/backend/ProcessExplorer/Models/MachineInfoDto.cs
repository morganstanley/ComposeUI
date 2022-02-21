namespace ProcessMonitor.Models
{
    public class MachineInfoDto
    {
        public string? MachineName { get; set; }
        public bool? IsUnix { get; set; }
        public OperatingSystem? OSVersion { get; set; }
        public bool? Is64BIOS { get; set; }
        public bool? Is64BitProcess { get; set; }
    }
}