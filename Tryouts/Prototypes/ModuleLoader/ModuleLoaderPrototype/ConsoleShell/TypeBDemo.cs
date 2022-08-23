/// ********************************************************************************************************
///
/// Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License").
/// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0.
/// See the NOTICE file distributed with this work for additional information regarding copyright ownership.
/// Unless required by applicable law or agreed to in writing, software distributed under the License
/// is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
/// See the License for the specific language governing permissions and limitations under the License.
/// 
/// ********************************************************************************************************

using ModuleLoaderPrototype;
using ModuleLoaderPrototype.Interfaces;
using ModuleLoaderPrototype.Modules;

namespace ConsoleShell;

internal class TypeBDemo : IDemo
{
    public async Task RunDemo()
    {
        const string crashingApp = "crashingapp";

        var catalogue = new ModuleCatalogue(new Dictionary<string, ModuleManifest>
        {
            {"crashingapp", new ModuleManifest
                {
                    StartupType = StartupType.Executable,
                    UIType = UIType.Window,
                    Name= "crashingapp",
                    Path = @"..\..\..\..\TestApp\bin\Debug\net6.0-windows\TestApp.exe"
                }
            }
        });

        var loader = new MessageBasedModuleLoader(catalogue, new ModuleHostFactory());
        bool canExit = false;
        var instanceId = Guid.NewGuid();
        int pid;
        loader.LifecycleEvents.Subscribe(e =>
            {
                var unexpected = e.IsExpected ? string.Empty : " unexpectedly";
                Console.WriteLine($"LifecycleEvent detected: {e.ProcessInfo.UiHint} {e.EventType}{unexpected}");

                canExit = e.IsExpected && e.EventType == LifecycleEventType.Stopped;

                if (e.EventType == LifecycleEventType.Stopped && !e.IsExpected)
                {
                    loader.RequestStartProcess(new LaunchRequest() { name = crashingApp, instanceId = instanceId });
                }
            });

        loader.RequestStartProcess(new LaunchRequest() { name = crashingApp, instanceId = instanceId });

        Console.ReadLine();

        Console.WriteLine("Exiting subprocesses");

        loader.RequestStopProcess(new StopRequest { instanceId = instanceId });


        while (!canExit)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(200));
        }

        Console.WriteLine("Bye, ComposeUI!");
    }
}
