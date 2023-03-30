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

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProcessExplorer.Abstractions;
using ProcessExplorer.Abstractions.Subsystems;
using ProcessExplorer.Server.Logging;

namespace ProcessExplorer.Server.Server.Abstractions;

internal abstract class ProcessExplorerServer
{
    private readonly ILogger _logger;
    public int Port { get; }
    public string Host { get; }

    public ProcessExplorerServer(
        int port,
        string host,
        ILogger? logger = null)
    {
        _logger = logger ?? NullLogger.Instance;
        Host = host;
        Port = port;
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

                if (processes != null) processInfoAggregator.InitProcesses(processes);
            }

            if (options.Value.Modules != null)
            {
                var subsystems = new Dictionary<Guid, SubsystemInfo>();
                foreach (var module in options.Value.Modules)
                {
                    subsystems.TryAdd(module.Key, SubsystemInfo.FromModule(module.Value));
                }

                if (processInfoAggregator != null && processInfoAggregator.SubsystemController != null)
                    await processInfoAggregator.SubsystemController.InitializeSubsystems(subsystems);
            }

            if(options.Value.MainProcessId != null)
                processInfoAggregator?.SetMainProcessId((int)options.Value.MainProcessId);

            if (options.Value.EnableProcessExplorer)
                processInfoAggregator?.EnableWatchingSavedProcesses();
        }
        catch (Exception exception)
        {
            _logger.ProcessExplorerSetupError(exception, exception);
        }
    }
}
