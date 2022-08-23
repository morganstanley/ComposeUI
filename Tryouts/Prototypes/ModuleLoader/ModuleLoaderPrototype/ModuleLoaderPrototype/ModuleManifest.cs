namespace ModuleLoaderPrototype;

public class ModuleManifest
{
    public string Name { get; set; }
    public StartupType StartupType { get; set; }
    public string UIType { get; set; }
    public string? Path { get; set; }
    public string? Url { get; set; }
}
