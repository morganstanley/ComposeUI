using ModuleLoaderPrototype.Interfaces;
using System.Reactive.Subjects;

namespace ModuleLoaderPrototype.Modules;

public abstract class ModuleBase : IModule
{
    public ModuleBase(string name)
    {
        Name = name;
    }

    public string Name { get; }

    protected readonly Subject<LifecycleEvent> _lifecycleEvents = new Subject<LifecycleEvent>();
    public IObservable<LifecycleEvent> LifecycleEvents => _lifecycleEvents;

    public abstract Task Initialize();

    public abstract Task Launch();

    public abstract Task Teardown();
}
