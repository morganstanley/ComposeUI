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
using System.Linq;
using ModuleProcessMonitor.Processes;
using System.Collections.ObjectModel;
using ProcessExplorer.Server.DependencyInjection;
using System.Reactive.Linq;
using ModulesPrototype.Infrastructure;
using ProcessExplorer.Abstraction.Subsystems;
using ProcessExplorer.Abstraction;
using ProcessExplorer.Server.Server.Abstractions;

namespace ModulesPrototype;

internal class Program
{
    public static async Task Main(string[] args)
    {
        var host = new HostBuilder()
            .ConfigureAppConfiguration(
                config => config.AddJsonFile("appsettings.json"))
            .ConfigureLogging(l => l.AddConsole().SetMinimumLevel(LogLevel.Debug))
            .ConfigureServices(
                (context, services) => services
                    .AddMessageRouterServer(mr => mr.UseWebSockets())
                    .Configure<MessageRouterWebSocketServerOptions>(
                        context.Configuration.GetSection("MessageRouter:WebSocket"))
                    .Configure<LoggerFactoryOptions>(context.Configuration.GetSection("Logging")))
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

        var processExplorer = new HostBuilder()
            .ConfigureLogging(l => l.AddConsole().SetMinimumLevel(LogLevel.Debug))
            .ConfigureServices(
                (context, services) => services
                    //.AddSingleton<IModuleLoader>(loader)
                    .AddProcessExplorerWindowsServer(pe => pe.UseGrpc())
                    .Configure<ProcessExplorerServerOptions>(op =>
                    {
                        op.Port = 5056;
                        //op.Modules = instances;
                        //op.Processes = processInfo;
                        op.MainProcessID = Process.GetCurrentProcess().Id;
                        op.EnableProcessExplorer = true;
                        //op.SubsystemLauncher = new SubsystemLauncher(host.Services.GetRequiredService<ILogger<SubsystemLauncher>>(), loader);
                    }))
            .Build();

        await processExplorer.StartAsync(cts.Token);

        var infoAggregator = processExplorer.Services.GetRequiredService<IProcessInfoAggregator>();

        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        var manifestString = File.ReadAllText("manifest.json");
        var manifest = JsonSerializer.Deserialize<Dictionary<string, ModuleManifest>>(manifestString);
        var catalogue = new ModuleCatalogue(manifest);
        var factory = new ModuleLoaderFactory();
        var loader = factory.Create(catalogue);
        var moduleCounter = new AsyncCountdownEvent(0);


        var processInfo = new ObservableCollection<ProcessInformation>();

        //var asyncObservable = Observable.Create<LifecycleEvent>(async observer =>
        //{
        //    using var subscription = loader.LifecycleEvents.Subscribe(async data =>
        //    {
        //        var unexpected = data.IsExpected ? string.Empty : " unexpectedly";

        //        logger.LogInformation(
        //            $"LifecycleEvent detected: {data.ProcessInfo.uiHint ?? "non-visual module"} {data.EventType}{unexpected}");

        //        if (data.EventType == LifecycleEventType.Started && data.ProcessInfo.uiType == UIType.Web)
        //        {
        //            var webId = StartBrowser(data.ProcessInfo.uiHint!);
        //            await subsystemLauncher.ModifySubsystemState(data.ProcessInfo.instanceId, SubsystemState.Started);
        //        }

        //        if (data.EventType == LifecycleEventType.Stopped)
        //        {
        //            instances[data.ProcessInfo.instanceId].State = SubsystemState.Stopped;
        //            await subsystemLauncher.ModifySubsystemState(data.ProcessInfo.instanceId, SubsystemState.Stopped);

        //            if (!data.IsExpected)
        //            {
        //                loader.RequestStartProcess(
        //                    new LaunchRequest() { name = data.ProcessInfo.name, instanceId = data.ProcessInfo.instanceId });

        //                instances[data.ProcessInfo.instanceId].State = SubsystemState.Started;
        //                await subsystemLauncher.ModifySubsystemState(data.ProcessInfo.instanceId, SubsystemState.Started);
        //            }
        //            else
        //            {
        //                moduleCounter.Signal();
        //            }
        //        }

        //        if (data.EventType == LifecycleEventType.Started)
        //        {
        //            instances[data.ProcessInfo.instanceId].State = SubsystemState.Started;
        //            await subsystemLauncher.ModifySubsystemState(data.ProcessInfo.instanceId, SubsystemState.Started);
        //        }

        //        var proc = new ProcessInformation(e.ProcessInfo.name,
        //            data.ProcessInfo.instanceId,
        //            data.ProcessInfo.uiType,
        //            data.ProcessInfo.uiHint!,
        //            (int)data.ProcessInfo.pid!);

        //        processInfo.Add(proc);
        //        observer.OnNext(data);
        //    },
        //    observer.OnError,
        //    observer.OnCompleted);

        //    await subscription;
        //});

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
                    if (!e.IsExpected)
                    {
                        loader.RequestStartProcess(
                            new LaunchRequest() { name = e.ProcessInfo.name, instanceId = e.ProcessInfo.instanceId });
                    }
                    else
                    {
                        moduleCounter.Signal();
                    }
                    infoAggregator.ScheduleSubsystemStateChanged(e.ProcessInfo.instanceId, SubsystemState.Stopped.ToString());
                }

                infoAggregator.ScheduleSubsystemStateChanged(e.ProcessInfo.instanceId, SubsystemState.Started.ToString());
                //var proc = new ProcessInformation(
                //    e.ProcessInfo.name,
                //    (int)e.ProcessInfo.pid!);

                //processInfo.Add(proc);
            });

        var instances = new Dictionary<Guid, Module>();
        foreach (var module in manifest)
        {
            var instanceId = Guid.NewGuid();
           
            instances.Add(instanceId, new()
            {
                Name = module.Value.Name,
                StartupType = module.Value.StartupType,
                UIType = module.Value.UIType,
                Path = module.Value.Path ?? string.Empty,
                Url = module.Value.Url,
                Arguments = module.Value.Arguments,
                Port = module.Value.Port,
            });

            moduleCounter.AddCount();
        }

        foreach (var module in instances)
        {
            if(module.Value.Name == "dataservice")
            {
                instances.TryGetValue(module.Key, out var instance);
                instance = new()
                {
                    StartupType = module.Value.StartupType,
                    Arguments = module.Value.Arguments,
                    State = SubsystemState.Started.ToString(),
                    Name = module.Value.Name,
                    Path = module.Value.Path,
                    Port = module.Value.Port,
                    UIType = module.Value.UIType,
                    Url = module.Value.Url,
                };

                loader.RequestStartProcess(new LaunchRequest { name = module.Value.Name, instanceId = module.Key });
            }
        }

        //var consoleShellPrototype = new ProcessInformation(Process.GetCurrentProcess());
        //processInfo.Add(consoleShellPrototype);

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
