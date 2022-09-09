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

using MorganStanley.ComposeUI.Tryouts.Core.Abstractions.Modules;
using MorganStanley.ComposeUI.Tryouts.Core.Services.ModulesService;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace MorganStanley.ComposeUI.Prototypes.ModulesPrototype;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        var manifestString = File.ReadAllText("manifest.json");
        var manifest = JsonSerializer.Deserialize<Dictionary<string, ModuleManifest>>(manifestString);

        var catalogue = new ModuleCatalogue(manifest);

        var factory = new ModuleLoaderFactory();
        var loader = factory.Create(catalogue);
        int canExit = 0;

        loader.LifecycleEvents.Subscribe(e =>
        {
            var unexpected = e.IsExpected ? string.Empty : " unexpectedly";
            Console.WriteLine($"LifecycleEvent detected: {e.ProcessInfo.uiHint ?? "non-visual module"} {e.EventType}{unexpected}");

            if (e.EventType == LifecycleEventType.Started && e.ProcessInfo.uiType == UIType.Web)
            {
                StartBrowser(e.ProcessInfo.uiHint);
            }

            if (e.EventType == LifecycleEventType.Stopped)
            {
                if (!e.IsExpected)
                {
                    loader.RequestStartProcess(new LaunchRequest() { name = e.ProcessInfo.name, instanceId = e.ProcessInfo.instanceId });
                }
                else { canExit++; }
            }
        });

        const string messageRouter = "messageRouter";
        const string dataService = "dataservice";
        const string datagrid = "datagrid";
        const string chart = "chart";

        var messagingInstanceId = Guid.NewGuid();
        var dataServiceInstanceId = Guid.NewGuid();
        var datagridInstanceId = Guid.NewGuid();
        var chartInstanceId = Guid.NewGuid();

        loader.RequestStartProcess(new LaunchRequest { name = messageRouter, instanceId = messagingInstanceId });
        loader.RequestStartProcess(new LaunchRequest { name = dataService, instanceId = dataServiceInstanceId });
        loader.RequestStartProcess(new LaunchRequest { name = datagrid, instanceId = datagridInstanceId });
        loader.RequestStartProcess(new LaunchRequest { name = chart, instanceId = chartInstanceId });

        Console.ReadLine();

        Console.WriteLine("Exiting subprocesses");

        loader.RequestStopProcess(new StopRequest { instanceId = chartInstanceId });
        loader.RequestStopProcess(new StopRequest { instanceId = datagridInstanceId });
        loader.RequestStopProcess(new StopRequest { instanceId = dataServiceInstanceId });
        loader.RequestStopProcess(new StopRequest { instanceId = messagingInstanceId });


        while (canExit < 4)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(200));
        }

        Console.WriteLine("Bye, ComposeUI!");
    }

    private static void StartBrowser(string url)
    {

        Process.Start(@"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe", url);
    }
}