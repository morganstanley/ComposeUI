/// ********************************************************************************************************
///
/// Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License").
/// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0.
/// See the NOTICE file distributed with this work for additional information regarding copyright ownership.
/// Unless required by applicable law or agreed to in writing, software distributed under the License
/// is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
/// See the License for the specific language governing permissions and limitations under the License.
/// Microsoft Visual Studio Solution File, Format Version 12.00
/// 
/// ********************************************************************************************************

using MorganStanley.ComposeUI.Tryouts.Core.Abstractions.Modules;
using MorganStanley.ComposeUI.Tryouts.Core.Services.ModulesService;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MorganStanley.ComposeUI.Prototypes.ModulesPrototype
{
    internal static class Program
    {
        public static async Task Main(string[] args)
        {
            const string crashingApp = "crashingapp";

            var catalogue = new ModuleCatalogue
            (
                new Dictionary<string, ModuleManifest>
                {
                    {
                        crashingApp,
                        new ModuleManifest
                        {
                            StartupType = StartupType.Executable,
                            UIType = UIType.Window,
                            Name= "crashingapp",
                            Path = @"Plugins\ApplicationPlugins\ModuleTestSimpleWpfApp\ModuleTestSimpleWpfApp.exe"
                        }
                    }
                });

            var moduleHostFactory = new ModuleHostFactory();
            var loader = new ModuleLoader(catalogue, moduleHostFactory);
            bool canExit = false;
            var instanceId = Guid.NewGuid();

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
}