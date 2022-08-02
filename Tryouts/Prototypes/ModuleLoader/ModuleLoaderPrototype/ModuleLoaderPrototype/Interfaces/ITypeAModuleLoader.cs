namespace ModuleLoaderPrototype.Interfaces
{
    internal interface ITypeAModuleLoader
    {
        int StartProcess(LaunchRequest request);
        Task<bool> StopProcess(int processId, CancellationToken cancellationToken = default(CancellationToken));
        IObservable<ProcessRestarted> ProcessRestarted { get; }
    }
}
