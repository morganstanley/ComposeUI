using ModuleLoaderPrototype.Interfaces;

namespace ModuleLoaderPrototype.Modules;

public class ModuleHostFactory : IModuleHostFactory
{
    public IModule CreateModuleHost(ModuleManifest manifest)
    {
        switch (manifest.StartupType, manifest.UIType)
        {
            case (StartupType.Executable, UIType.Window):
                return new ExecutableModule(manifest.Name, manifest.Path);
            case (StartupType.None, UIType.Web):
                return new WebpageModule(manifest.Name, manifest.Url);
            default:
                throw new NotSupportedException("Unsupported module type");
        }
    }
}
