namespace MorganStanley.ComposeUI.ModuleLoader;

public class ModuleNotFoundException : ModuleLoaderException
{
    public ModuleNotFoundException(string moduleId): base($"Unknown module id: {moduleId}")
    {
    }
}