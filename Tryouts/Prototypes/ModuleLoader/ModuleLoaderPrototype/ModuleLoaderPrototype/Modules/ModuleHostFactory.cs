namespace ModuleLoaderPrototype.Modules;

public class ModuleHostFactory : IModuleHostFactory
{
    public IModule CreateModuleHost(ModuleManifest manifest)
    {
        switch (manifest.ModuleType)
        {
            case (ModuleType.Executable):
                return new ExecutableModule(manifest.Name, manifest.Path);
            default:
                throw new NotSupportedException("Unsupported module type");
        }
    }
}
