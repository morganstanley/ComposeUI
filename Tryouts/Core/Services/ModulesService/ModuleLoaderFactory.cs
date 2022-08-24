using MorganStanley.ComposeUI.Tryouts.Core.Abstractions.Modules;

namespace MorganStanley.ComposeUI.Tryouts.Core.Services.ModulesService
{
    public class ModuleLoaderFactory : IModuleLoaderFactory
    {
        public IModuleLoader Create(ModuleCatalogue catalogue)
        {
            return new ModuleLoader(catalogue, new ModuleHostFactory());
        }
    }
}
