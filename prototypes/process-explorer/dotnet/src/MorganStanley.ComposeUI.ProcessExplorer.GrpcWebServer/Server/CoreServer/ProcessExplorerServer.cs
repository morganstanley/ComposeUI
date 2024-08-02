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

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions;
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Subsystems;
using MorganStanley.ComposeUI.ProcessExplorer.GrpcWebServer.Logging;

namespace MorganStanley.ComposeUI.ProcessExplorer.GrpcWebServer.Server.CoreServer;

internal class ProcessExplorerServer : IHostedService
{
    private readonly ILogger _logger;
    private readonly CancellationTokenSource _stopTokenSource = new();
    private readonly TaskCompletionSource _startTaskSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _stopTaskSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly IProcessInfoAggregator _processInfoAggregator;
    private readonly ProcessExplorerServerOptions _options;

    public ProcessExplorerServer(
        IProcessInfoAggregator processInfoAggregator,
        IOptions<ProcessExplorerServerOptions> options,
        ILogger? logger = null)
    {
        _processInfoAggregator = processInfoAggregator;
        _options = options.Value;
        _logger = logger ?? NullLogger.Instance;
    }

    public async void SetupProcessExplorer(
        IOptions<ProcessExplorerServerOptions> options,
        IProcessInfoAggregator processInfoAggregator)
    {
        try
        {
            if (options.Value.Processes != null)
            {
                var processes = options.Value.Processes
                  .Select(process => process.ProcessInfo.ProcessId)
                  .ToArray();

                processInfoAggregator.InitProcesses(processes);
            }

            if (options.Value.Modules != null)
            {
                var subsystems = new Dictionary<Guid, SubsystemInfo>();
                foreach (var module in options.Value.Modules)
                {
                    subsystems.TryAdd(module.Key, SubsystemInfo.FromModule(module.Value));
                }

                await processInfoAggregator.SubsystemController.InitializeSubsystems(subsystems);
            }

            if (options.Value.MainProcessId != null)
            {
                processInfoAggregator.MainProcessId = (int)options.Value.MainProcessId;
            }

            if (options.Value.EnableWatchingProcesses)
            {
                processInfoAggregator.EnableWatchingSavedProcesses();
            }
        }
        catch (Exception exception)
        {
            _logger.ProcessExplorerSetupError(exception, exception);
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken == CancellationToken.None) _logger.GrpcCancellationTokenWarning();

        Task.Run(StartAsyncCore, _stopTokenSource.Token);
        Task.Run(() => _processInfoAggregator.RunSubsystemStateQueue(_stopTokenSource.Token), _stopTokenSource.Token);

        return _startTaskSource.Task;
    }

    private void StartAsyncCore()
    {
        _logger.GrpcServerStartedDebug();
        _startTaskSource.SetResult();

        try
        {
            SetupProcessExplorer(_options, _processInfoAggregator);
        }
        catch (Exception)
        {
            _logger.GrpcServerStoppedDebug();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _stopTaskSource.SetResult();
            _stopTokenSource.Cancel();
            _logger.GrpcServerStoppedDebug();
        }
        catch (InvalidOperationException exception)
        {
            _logger.GrpcServerStopAsyncError(exception, exception);
        }

        return Task.CompletedTask;
    }
}
