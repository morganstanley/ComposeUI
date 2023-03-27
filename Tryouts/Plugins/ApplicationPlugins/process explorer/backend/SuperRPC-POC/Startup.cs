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

using MorganStanley.ComposeUI.Messaging;
using MorganStanley.ComposeUI.Messaging.Client.WebSocket;
using ProcessExplorer.Abstractions;
using ProcessExplorer.Abstractions.Processes;
using ProcessExplorer.Core.Factories;
using SuperRPC_POC.Infrastructure;
using SuperRPC_POC.Infrastructure.Messages;

namespace SuperRPC_POC;

public class Startup
{
    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddDebug();
            builder.AddConsole();
            builder.AddFilter(null, LogLevel.Information);
        });

        var processInfoManagerForWindows =
            ProcessMonitorFactory.CreateProcessInfoGeneratorWindows(loggerFactory.CreateLogger<ProcessInfoManager>());

        //var processMonitor =
        //    ProcessMonitorFactory.CreateProcessMonitor(processInfoManagerForWindows, loggerFactory.CreateLogger<IProcessMonitor>());

        var processInfoAggregator =
            ProcessAggregatorFactory.CreateProcessInfoAggregator(loggerFactory.CreateLogger<IProcessInfoAggregator>(), processInfoManagerForWindows);

        services.AddMessageRouter(
            mr => mr.UseWebSocket(
                new MessageRouterWebSocketOptions() { Uri = new("ws://localhost:5000/ws") }));

        var processInfoListRouterMessage =
            new ProcessInfoListRouterMessage(loggerFactory.CreateLogger<ProcessInfoListRouterMessage>(), processInfoAggregator);

        var processInfoRouterMessage =
            new ProcessInfoRouterMessage(loggerFactory.CreateLogger<ProcessInfoRouterMessage>(), processInfoAggregator);

        var processMonitorCheckerRouterMessage =
            new ProcessMonitorCheckerRouterMessage(loggerFactory.CreateLogger<ProcessMonitorCheckerRouterMessage>(),
                processInfoAggregator);

        var runtimeInfoRouterMessage = new RuntimeInformationRouterMessage(processInfoAggregator,
            loggerFactory.CreateLogger<RuntimeInformationRouterMessage>());

        services.AddSingleton<ILoggerFactory>(loggerFactory);
        services.AddSingleton<ProcessInfoListRouterMessage>(processInfoListRouterMessage);
        services.AddSingleton<ProcessInfoRouterMessage>(processInfoRouterMessage);
        services.AddSingleton<ProcessMonitorCheckerRouterMessage>(processMonitorCheckerRouterMessage);
        services.AddSingleton<RuntimeInformationRouterMessage>(runtimeInfoRouterMessage);
        services.AddSingleton<IModuleLoaderInformationReceiver, ModuleLoaderInformationReceiver>();
        services.AddSingleton<IProcessInfoAggregator>(processInfoAggregator);
        services.AddSingleton<ProcessInfoManager>(processInfoManagerForWindows);
        //services.AddSingleton<IProcessMonitor>(processMonitor);

        services.AddLogging(builder =>
        {
            builder.AddDebug();
            builder.AddConsole();
            builder.AddFilter(null, LogLevel.Debug);
        });

    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseWebSockets();
        app.UseMiddleware<SuperRpcWebSocketMiddlewareV2>();
    }
}
