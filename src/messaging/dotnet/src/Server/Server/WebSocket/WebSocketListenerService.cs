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

using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace MorganStanley.ComposeUI.Messaging.Server.WebSocket;

internal sealed class WebSocketListenerService : IHostedService, IMessageRouterWebSocketServer
{
    private readonly IServiceProvider _serviceProvider;
    private readonly MessageRouterWebSocketServerOptions _options;
    private readonly CancellationTokenSource _stopTokenSource = new();
    private readonly TaskCompletionSource _stopTaskSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly ConcurrentBag<Task> _connectionTasks = new();
    private readonly TaskCompletionSource _startTaskSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public WebSocketListenerService(
        IOptions<MessageRouterWebSocketServerOptions> options,
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
    }

    private HttpListener _httpListener = null!;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Task.Factory.StartNew(StartAsyncCore, TaskCreationOptions.RunContinuationsAsynchronously);
        return _startTaskSource.Task;
    }

    private async Task StartAsyncCore()
    {
        CreateHttpListener(_options, out _httpListener, out var port);
        WebSocketUrl = new Uri($"ws://localhost:{port}{_options.RootPath}");
        _startTaskSource.SetResult();

        try
        {
            while (!_stopTokenSource.IsCancellationRequested)
            {
                var context = await _httpListener.GetContextAsync().WaitAsync(_stopTokenSource.Token);

                if (context.Request.IsWebSocketRequest)
                {
                    var webSocketContext = await context.AcceptWebSocketAsync(null);
                    var webSocket = webSocketContext.WebSocket;
                    var connection = ActivatorUtilities.CreateInstance<WebSocketConnection>(_serviceProvider);
                    _connectionTasks.Add(connection.HandleWebSocketRequest(webSocket, _stopTokenSource.Token));
                }
                else
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    context.Response.Close();
                }
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            _httpListener.Close();
        }

        _stopTaskSource.SetResult();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _stopTokenSource.Cancel();
        await Task.WhenAll(_connectionTasks);
        await _stopTaskSource.Task;
    }

    private static void CreateHttpListener(
        MessageRouterWebSocketServerOptions options,
        out HttpListener httpListener,
        out int port)
    {
        var rootPath = options.RootPath.TrimEnd('/') + '/';
        
        if (options.Port.HasValue)
        {
            port = options.Port.Value;
            httpListener = CreateListener(rootPath, port);
            httpListener.Start();

            return;
        }

        var globalProperties = IPGlobalProperties.GetIPGlobalProperties();
        var usedPorts = globalProperties.GetActiveTcpListeners().Select(i => i.Port).ToHashSet();
        const int minPort = 49215;
        const int maxPort = 65535;
        var random = new Random();

        for (;;)
        {
            port = random.Next(minPort, maxPort);

            if (usedPorts.Contains(port))
                continue;

            httpListener = CreateListener(rootPath, port);

            try
            {
                httpListener.Start();

                return;
            }
            catch
            {
                usedPorts.Add(port);
                // HttpListener is disposed automatically when Start throws
            }
        }

        static HttpListener CreateListener(string rootPath, int port)
        {
            var httpListener = new HttpListener();
            httpListener.Prefixes.Add($"http://localhost:{port}" + rootPath);

            return httpListener;
        }
    }

    public Uri WebSocketUrl { get; private set; } = null!;
}
