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
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions;
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Subsystems;
using MorganStanley.ComposeUI.ProcessExplorer.Core.DependencyInjection;
using MorganStanley.ComposeUI.ProcessExplorer.Server.DependencyInjection;
using MorganStanley.ComposeUI.ProcessExplorer.Server.Server.Abstractions;
using Microsoft.AspNetCore.Builder;
using MorganStanley.ComposeUI.ProcessExplorer.Server.Server.Infrastructure.Grpc;

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

        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        var manifestString = File.ReadAllText("manifest.json");
        var manifest = JsonSerializer.Deserialize<Dictionary<string, ModuleManifest>>(manifestString);
        var catalogue = new ModuleCatalogue(manifest);
        var factory = new ModuleLoaderFactory();
        var loader = factory.Create(catalogue);
        var moduleCounter = new AsyncCountdownEvent(0);

        var processExplorer = WebApplication.CreateBuilder(args);
        processExplorer.Services.AddGrpc();
        processExplorer.Services.AddCors(o => o.AddPolicy("AllowAll", builder =>
        {
            builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding");
        }));
        processExplorer.Services.ConfigureSubsystemLauncher(loader.RequestStartProcess, loader.RequestStopProcess, CreateLaunchRequest, CreateStopRequest);
        processExplorer.Services.AddProcessExplorerWindowsServerWithGrpc(pe => pe.UseGrpc());
        processExplorer.Services.Configure<ProcessExplorerServerOptions>(op =>
                {
                    op.Port = 5056;
                    op.MainProcessId = Process.GetCurrentProcess().Id;
                    op.EnableProcessExplorer = true;
                });

        var processExploreWebApplication = processExplorer.Build();
        processExploreWebApplication.UseGrpcWeb();
        processExploreWebApplication.UseCors();
        processExploreWebApplication.MapGrpcService<ProcessExplorerMessageHandlerService>().EnableGrpcWeb().RequireCors("AllowAll");

        await processExploreWebApplication.StartAsync(cts.Token);
        var infoAggregator = processExploreWebApplication.Services.GetRequiredService<IProcessInfoAggregator>();

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
            instances.TryGetValue(module.Key, out var instance);
            instance.State = SubsystemState.Started;
            loader.RequestStartProcess(new LaunchRequest { name = module.Value.Name, instanceId = module.Key });
        }

        await infoAggregator.SubsystemController.InitializeSubsystems(instances.Select(kvp => new KeyValuePair<Guid, SubsystemInfo>(kvp.Key, SubsystemInfo.FromModule(kvp.Value))));

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

    static LaunchRequest CreateLaunchRequest(Guid id, string name)
    {
        return new LaunchRequest()
        {
            instanceId = id,
            name = name,
        };
    }

    static StopRequest CreateStopRequest(Guid id)
    {
        return new StopRequest()
        {
            instanceId = id,
        };
    }
}
