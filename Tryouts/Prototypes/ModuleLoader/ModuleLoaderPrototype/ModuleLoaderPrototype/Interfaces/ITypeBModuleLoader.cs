namespace ModuleLoaderPrototype.Interfaces
{
    internal interface ITypeBModuleLoader
    {
        void RequestStartProcess(LaunchRequest request);
        void RequestStopProcess(string name);
        IObservable<LifecycleEvent> LifecycleEvents { get; }
    }
}
