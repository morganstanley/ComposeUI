// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TestServer;

class Program
{
    static async Task Main(string[] args)
    {
        var hostBuilder = Host.CreateDefaultBuilder();

        hostBuilder
            .ConfigureServices(
                (context, services) =>
                {
                    services
                        .AddMessageRouterServer(mr => mr.UseWebSockets(
                            ws =>
                            {
                                ws.Port = 5000;
                                ws.RootPath = "/ws";
                            }));
                })
            .ConfigureLogging(l => l.SetMinimumLevel(LogLevel.Debug));

        var host = hostBuilder.Build();

        await host.StartAsync();

        var stop = new TaskCompletionSource();

        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            eventArgs.Cancel = true;
            stop.SetResult();
        };

        await stop.Task;
        await host.StopAsync();
    }
}
