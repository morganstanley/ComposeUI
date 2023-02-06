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
using ProcessExplorer.Abstraction;
using ProcessExplorer.Abstraction.Subsystems;

namespace ProcessExplorer.Server.Server.Abstractions;

public abstract class ProcessExplorerServer
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

    internal void SetupProcessExplorer(
        ProcessExplorerServerOptions options,
        IProcessInfoAggregator processInfoAggregator)
    {
        try
        {
            if (options.Processes != null)
            {
                var processes = options.Processes
                  .Select(process => process.ProcessInfo.PID)
                  .ToArray();

                if (processes != null) processInfoAggregator.InitProcesses(processes);
            }

            if (options.Modules != null)
            {
                var subsystems = new Dictionary<Guid, SubsystemInfo>();
                foreach (var module in options.Modules)
                {
                    subsystems.TryAdd(module.Key, SubsystemInfo.FromModule(module.Value));
                }

                processInfoAggregator.InitializeSubsystems(subsystems);
            }

            if (options.EnableProcessExplorer)
                processInfoAggregator.EnableWatchingSavedProcesses();
        }
        catch (Exception exception)
        {
            _logger.LogError($"Setting up PE was unsuccessful. {0}", exception);
        }
    }
}
