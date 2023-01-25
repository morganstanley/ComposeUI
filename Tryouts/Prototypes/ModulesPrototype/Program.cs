// Morgan Stanley makes this available to you under the Apache License,
// Version 2.0 (the "License"). You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0.
// 
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership. Unless required by applicable law or agreed
// to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
// or implied. See the License for the specific language governing permissions
// and limitations under the License.

using MorganStanley.ComposeUI.Tryouts.Core.Abstractions.Modules;
using MorganStanley.ComposeUI.Tryouts.Core.Services.ModulesService;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MorganStanley.ComposeUI.Messaging.Server.WebSocket;
using Nito.AsyncEx;
using ModulesPrototype.Infrastructure;
using System.Linq;
using ModuleProcessMonitor.Subsystems;
using ModuleProcessMonitor.Processes;
using System.Collections.ObjectModel;
using MorganStanley.ComposeUI.Messaging.Client.WebSocket;
using ModuleProcessMonitor;

namespace ModulesPrototype;

internal class Program
{
    public static async Task Main(string[] args)
    {
        var host = new HostBuilder()
            .ConfigureAppConfiguration(
                config => config.AddJsonFile("appsettings.json"))
            .ConfigureLogging(l => l.AddConsole())
            .ConfigureServices(
                (context, services) => services
                    .AddMessageRouterServer(mr => mr.UseWebSockets())
                    .Configure<MessageRouterWebSocketServerOptions>(
                        context.Configuration.GetSection("MessageRouter:WebSocket")))
            .Build();

        var cts = new CancellationTokenSource();
        var stopTaskSource = new TaskCompletionSource();

        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cts.Cancel();
            stopTaskSource.SetResult();
        };

        await host.StartAsync(cts.Token);

        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        var manifestString = File.ReadAllText("manifest.json");
        var manifest = JsonSerializer.Deserialize<Dictionary<string, ModuleManifest>>(manifestString);
        var catalogue = new ModuleCatalogue(manifest);
        var factory = new ModuleLoaderFactory();
        var loader = factory.Create(catalogue);
        var moduleCounter = new AsyncCountdownEvent(0);

        var loggerFactory = GetServiceProvider()
            .GetRequiredService<ILoggerFactory>();
        var infoCollector = GetServiceProvider()
            .GetRequiredService<IProcessInfoHandler>();
        infoCollector.SetSubsystemHandler(loader, loggerFactory);

        var processInfo = new ObservableCollection<ProcessInformation>();

        loader.LifecycleEvents.Subscribe(
            e =>
            {
                var unexpected = e.IsExpected ? string.Empty : " unexpectedly";

                logger.LogInformation(
                    $"LifecycleEvent detected: {e.ProcessInfo.uiHint ?? "non-visual module"} {e.EventType}{unexpected}");

                if (e.EventType == LifecycleEventType.Started && e.ProcessInfo.uiType == UIType.Web)
                {
                    var webId = StartBrowser(e.ProcessInfo.uiHint!);
                }

                if (e.EventType == LifecycleEventType.Stopped)
                {
                    //await infoCollector.SendModifiedSubsystemStateAsync(e.ProcessInfo.instanceId, SubsystemState.Stopped);

                    if (!e.IsExpected)
                    {
                        loader.RequestStartProcess(
                            new LaunchRequest() { name = e.ProcessInfo.name, instanceId = e.ProcessInfo.instanceId });

                        //await infoCollector.SendModifiedSubsystemStateAsync(e.ProcessInfo.instanceId, SubsystemState.Started);
                    }
                    else
                    {
                        moduleCounter.Signal();
                    }
                }

                //if (e.EventType == LifecycleEventType.Started)
                //{
                //    await infoCollector.SendModifiedSubsystemStateAsync(e.ProcessInfo.instanceId, SubsystemState.Started);
                //}

                var proc = new ProcessInformation(e.ProcessInfo.name,
                    e.ProcessInfo.instanceId,
                    e.ProcessInfo.uiType,
                    e.ProcessInfo.uiHint!,
                    (int)e.ProcessInfo.pid!);

                processInfo.Add(proc);
            });

        var instances = new Dictionary<Guid, Module>();
        foreach (var module in manifest)
        {
            var instanceId = Guid.NewGuid();
            instances.Add(instanceId, (Module)module.Value);
            moduleCounter.AddCount();
        }

        foreach (var module in instances)
        {
            //for the demo's sake we are starting just the Process Explorer.
            //the other applications, that are declared in the manifest can be started via the Process Explorer frontend
            //if (module.Value.Name != "processExplorerService") continue;
            module.Value.State = ModuleState.Started;
            loader.RequestStartProcess(new LaunchRequest { name = module.Value.Name, instanceId = module.Key });
        }

        //if (!instances
        //    .Where(module =>
        //        module.Value.Name == "processExplorerService"
        //        && (module.Value.State == SubsystemState.Started || module.Value.State == SubsystemState.Running))
        //    .Any())
        //    goto endState;

        //    await infoCollector.InitializeSubsystemControllerRouteAsync();
        //    var consoleShellPrototype = new ProcessInformation(Process.GetCurrentProcess());
        //    processInfo.Add(consoleShellPrototype);
        //    var serializedInstances = JsonSerializer.Serialize(instances);
        //    if (serializedInstances != string.Empty) await infoCollector.SendRegisteredSubsystemsAsync(serializedInstances);
        //    if (processInfo.Count != 0) await infoCollector.SendInitProcessInfoAsync(processInfo);
        //    await infoCollector.EnableProcessMonitorAsync();

        //endState:
        logger.LogInformation("ComposeUI application running, press Ctrl+C to exit");

        await stopTaskSource.Task;

        logger.LogInformation("Exiting subprocesses");

        instances.Reverse();

        foreach (var item in instances)
        {
            loader.RequestStopProcess(new StopRequest { instanceId = item.Key });
        }

        await moduleCounter.WaitAsync();

        logger.LogInformation("Bye, ComposeUI!");
    }

    private static IServiceProvider GetServiceProvider()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddDebug();
        });

        var serviceProvider = new ServiceCollection()
                    .AddLogging(builder =>
                    {
                        builder.AddDebug();
                    })
                    .AddSingleton<ILoggerFactory>(loggerFactory)
                    .AddMessageRouter(
                        mr => mr.UseWebSocket(new MessageRouterWebSocketOptions { Uri = new("ws://localhost:5000/ws") }))
                    .AddSingleton<IProcessInfoHandler, ProcessInfoHandler>()
                    .BuildServiceProvider();

        return serviceProvider;
    }

    private static int StartBrowser(string url)
    {
        var prs2 = new ProcessStartInfo
        {
            FileName = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
        };
        var pr2 = new Process
        {
            StartInfo = prs2,
        };
        pr2.StartInfo.Arguments = url;
        pr2.Start();
        return pr2.Id;
    }
}
