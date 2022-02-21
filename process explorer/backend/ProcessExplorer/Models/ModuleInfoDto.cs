using System.Reflection;

namespace ProcessMonitor.Models
{
    public class ModuleInfoDto
    {
        public string? Name { get; set; }
        public Guid? Version { get; set; }
        public string? VersionRedirectedFrom { get; set; }
        public byte[]? PublicKeyToken { get; set; }
        public string? Path { get; set; }
        public IEnumerable<CustomAttributeData>? Dependencies { get; set; }
    }
}