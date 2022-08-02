using ModuleLoaderPrototype;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleShell
{
    internal class TypeBDemo : IDemo
    {
        public async Task RunDemo()
        {
            const string crashingApp = "crashingapp";
            var loader = new MessageBasedModuleLoader();
            bool canExit = false;
            int pid;
            loader.LifecycleEvents.Subscribe(e =>
            {
                var unexpected = e.expected ? string.Empty : " unexpectedly";
                Console.WriteLine($"LifecycleEvent detected: {e.pid} {e.eventType}{unexpected}");

                canExit = e.expected && e.eventType == LifecycleEventType.Stopped;
            });
            loader.RequestStartProcess(new LaunchRequest() { name = crashingApp, path = @"..\..\..\..\CrashingApp\bin\Debug\net6.0\CrashingApp.exe" });

            Console.ReadLine();

            Console.WriteLine("Exiting subprocesses");
            loader.RequestStopProcess(crashingApp);

            while (!canExit)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(200));
            }

            Console.WriteLine("Bye, ComposeUI!");
        }
    }
}
