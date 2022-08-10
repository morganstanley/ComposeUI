using ModuleLoaderPrototype;

namespace ConsoleShell
{
    internal class TypeADemo : IDemo
    {
        public async Task RunDemo()
        {
            Console.WriteLine("Restart with module loader? (1 = yes)");
            bool loaderRestart = Console.ReadLine().StartsWith('1');

            var loader = new DirectlyStartingModuleLoader(loaderRestart);
            int pid;
            loader.ProcessRestarted.Subscribe(pr =>
            {
                Console.WriteLine($"Process restart detected: {pr.oldPid} -> {pr.newPid}");
                pid = pr.newPid;
            });
            pid = loader.StartProcess(new LaunchRequest() { path = @"..\..\..\..\TestApp\bin\Debug\net6.0-windows\TestApp.exe" });

            Console.ReadLine();

            Console.WriteLine("Exiting subprocesses");
            if (loaderRestart)
            {
                await loader.StopProcess(pid);
            }
            Console.WriteLine("Bye, ComposeUI!");
        }
    }
}
