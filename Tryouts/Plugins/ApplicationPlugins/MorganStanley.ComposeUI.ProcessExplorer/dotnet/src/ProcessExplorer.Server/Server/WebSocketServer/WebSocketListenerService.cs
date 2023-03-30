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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProcessExplorer.Abstractions;
using ProcessExplorer.Abstractions.Subsystems;
using ProcessExplorer.Core.Factories;
using ProcessExplorer.Server.Logging;
using ProcessExplorer.Server.Server.Abstractions;
using ProcessExplorer.Server.Server.Extensions;

namespace ProcessExplorer.Server.Server.WebSocketServer;

internal sealed class WebSocketListenerService : ProcessExplorerServer, IHostedService
{
    private readonly ILogger<WebSocketListenerService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ProcessExplorerServerOptions _options;
    private readonly HttpListener _httpListener;
    private readonly IProcessInfoAggregator _processInfoAggregator;
    private readonly ConcurrentBag<Task> _connectionTasks = new();

    public WebSocketListenerService(
        ILogger<WebSocketListenerService>? logger,
        IServiceProvider serviceProvider,
        IProcessInfoAggregator processInfoAggregator,
        IOptions<ProcessExplorerServerOptions> options)
        : base(
            options.Value.Port ?? 5056,
            options.Value.Host ?? "localhost",
            logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _httpListener = new();
        _logger = logger ?? NullLogger<WebSocketListenerService>.Instance;
        _processInfoAggregator = processInfoAggregator;

        if (_options.SubsystemLauncherCommunicator != null)
            _processInfoAggregator.SetSubsystemController(DefineSubsystemController(_options.SubsystemLauncherCommunicator));
    }

    public ISubsystemController DefineSubsystemController(ISubsystemLauncherCommunicator subsystemLauncherCommunicator)
    {
        return SubsystemFactory.CreateSubsystemController(subsystemLauncherCommunicator, _logger);
    }

    public ISubsystemController DefineSubsystemController(ISubsystemLauncher subsystemLauncher)
    {
        return SubsystemFactory.CreateSubsystemController(subsystemLauncher, _logger);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Task.Run(() => StartAsyncCore(cancellationToken));
        return Task.CompletedTask;
    }

    private async Task StartAsyncCore(CancellationToken cancellationToken)
    {
        _logger.WebSocketServerStartedDebug();

        _httpListener.Prefixes.Add($"http://localhost:{Port}/");

        SetupProcessExplorer(_options, _processInfoAggregator);

        _httpListener.Start();

        while (!cancellationToken.IsCancellationRequested)
        {
            var context = await _httpListener.GetContextAsync()
                .WithCancellationToken(cancellationToken);

            if (context == null) return;
            if (!context.Request.IsWebSocketRequest) context.Response.Abort();
            else
            {
                var webSocketContext = await context.AcceptWebSocketAsync(subProtocol: null)
                    .WithCancellationToken(cancellationToken);

                if (webSocketContext == null) return;

                var webSocket = webSocketContext.WebSocket;

                if (webSocket == null) return;

                var connection = new WebSocketConnection(
                    _logger,
                    _processInfoAggregator);

                _connectionTasks.Add(connection.HandleWebSocketRequest(webSocket, cancellationToken));
                _logger.WebSocketConnectionHandlerAddedDebug();
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _httpListener.Stop();
        _logger.WebSocketServerStoppedDebug();

        return Task.CompletedTask;
    }
}
