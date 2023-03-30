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

using Grpc.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProcessExplorer.Abstractions;
using ProcessExplorer.Abstractions.Infrastructure.Protos;
using ProcessExplorer.Abstractions.Subsystems;
using ProcessExplorer.Core.Factories;
using ProcessExplorer.Server.Logging;
using ProcessExplorer.Server.Server.Abstractions;
using ProcessExplorer.Server.Server.Infrastructure.Grpc;
using GRPCServer = Grpc.Core.Server;

namespace ProcessExplorer.Server.Server.GrpcServer;

internal class GrpcListenerService : ProcessExplorerServer, IHostedService
{
    private readonly CancellationTokenSource _stopTokenSource = new();
    private readonly TaskCompletionSource _startTaskSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _stopTaskSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly IProcessInfoAggregator _processInfoAggregator;
    private readonly ProcessExplorerServerOptions _options;
    private readonly ILogger<GrpcListenerService> _logger;
    private GRPCServer? _grpcServer;

    public GrpcListenerService(
        IProcessInfoAggregator processInfoAggregator,
        IOptions<ProcessExplorerServerOptions> options,
        ILogger<GrpcListenerService>? logger = null,
        ISubsystemLauncher? subsystemLauncher = null,
        ISubsystemLauncherCommunicator? subsystemLauncherCommunicator = null)
        :base(
            options.Value.Port ?? 5056,
            options.Value.Host ?? "localhost",
            logger)
    {
        _processInfoAggregator = processInfoAggregator;
        _options = options.Value;
        _logger = logger ?? NullLogger<GrpcListenerService>.Instance;

        if (subsystemLauncher != null)
        {
            _processInfoAggregator.SetSubsystemController(SubsystemFactory.CreateSubsystemController(subsystemLauncher, _logger));
        }
        else if (subsystemLauncherCommunicator != null)
        {
            _processInfoAggregator.SetSubsystemController(SubsystemFactory.CreateSubsystemController(subsystemLauncherCommunicator, _logger));
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken == CancellationToken.None) _logger.GrpcCancellationTokenWarning();

        Task.Run(() => StartAsyncCore(), _stopTokenSource.Token);
        Task.Run(() => _processInfoAggregator.RunSubsystemStateQueue(_stopTokenSource.Token), _stopTokenSource.Token);

        return _startTaskSource.Task;
    }

    private void StartAsyncCore()
    {
        _logger.GrpcServerStartedDebug();
        _startTaskSource.SetResult();

        SetupGrpcServer();

        if (_grpcServer == null) return;

        try
        {
            _grpcServer.Start();
            SetupProcessExplorer(_options, _processInfoAggregator);
        }
        catch (Exception)
        {
            _logger.GrpcServerStoppedDebug();
        }
    }

    private void SetupGrpcServer()
    {
        _grpcServer = new GRPCServer
        {
            Services = { ProcessExplorerMessageHandler.BindService(new ProcessExplorerMessageHandlerService(_processInfoAggregator, _logger)) },
            Ports = { new ServerPort(Host, Port, ServerCredentials.Insecure) },
        };
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _stopTaskSource.SetResult();
        _stopTokenSource.Cancel();

        if (_grpcServer == null) return;

        _processInfoAggregator.Dispose();
        
        var shutdown = _grpcServer.ShutdownAsync();       
        _logger.GrpcServerStoppedDebug();

        await Task.WhenAll(shutdown, _stopTaskSource.Task);
    }
}
