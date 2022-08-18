using ModuleLoaderPrototype.Interfaces;

namespace ModuleLoaderPrototype.Modules;

internal interface IModule
{
    IObservable<LifecycleEvent> LifecycleEvents { get; }
    void Initialize();
    void Launch();
    void Teardown();
}
