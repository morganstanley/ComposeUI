using ModuleLoaderPrototype;

namespace ConsoleShell
{
    internal class TypeADemo : IDemo
    {
        public async Task RunDemo()
        {
            var loader = new DirectlyStartingModuleLoader();
            int pid;
            loader.ProcessRestarted.Subscribe(pr =>
            {
                Console.WriteLine($"Process restart detected: {pr.oldPid} -> {pr.newPid}");
                pid = pr.newPid;
            });
            pid = loader.StartProcess(new LaunchRequest() { path = @"..\..\..\..\CrashingApp\bin\Debug\net6.0\CrashingApp.exe" });

            Console.ReadLine();

            Console.WriteLine("Exiting subprocesses");
            await loader.StopProcess(pid);
            Console.WriteLine("Bye, ComposeUI!");
        }
    }
}
