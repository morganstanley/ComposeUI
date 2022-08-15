using ModuleLoaderPrototype.Interfaces;

namespace ModuleLoaderPrototype.Modules;

public interface IModule
{
    string Name { get; }
    IObservable<LifecycleEvent> LifecycleEvents { get; }
    Task Initialize();
    Task Launch();
    Task Teardown();
}
