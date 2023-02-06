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

using LocalCollector;
using LocalCollector.Connections;
using LocalCollector.Modules;
using LocalCollector.Registrations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ProcessExplorer.Abstraction.Infrastructure;
using ProcessExplorer.Abstraction.Processes;
using ProcessExplorer.Abstraction.Subsystems;
using ProcessExplorer.Server.Logging;
using ProcessExplorer.Server.Server.Abstractions;

namespace ProcessExplorer.Server.Server.Infrastructure.WebSocket;

internal class WebSocketUIHandler : IUIHandler
{
    private readonly ILogger _logger;
    private readonly IWebSocketConnection _webSocketConnection;
    private readonly CancellationToken _cancellationToken;

    public WebSocketUIHandler(
        ILogger? logger,
        IWebSocketConnection webSocketConnection,
        CancellationToken cancellationToken = default)
    {
        _logger = logger ?? NullLogger.Instance;
        _webSocketConnection = webSocketConnection;
        _cancellationToken = cancellationToken;
    }

    public async Task AddConnections(
        string assemblyId,
        IEnumerable<ConnectionInfo> connections)
    {
        try
        {
            var message = new WebSocketMessage
            {
                Topic = Topics.AddingConnections,
                Payload = new Dictionary<string, IEnumerable<ConnectionInfo>>()[assemblyId] = connections
            };

            await _webSocketConnection.SendAsync(message, _cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.AddConnectionsError(exception, exception);
        }
    }

    public async Task AddProcess(ProcessInfoData process)
    {
        try
        {
            var message = new WebSocketMessage
            {
                Topic = Topics.AddProcess,
                Payload = process
            };

            await _webSocketConnection.SendAsync(message, _cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.AddProcessError(exception, exception);
        }
    }

    public async Task AddProcesses(IEnumerable<ProcessInfoData> processes)
    {
        try
        {
            var message = new WebSocketMessage
            {
                Topic = Topics.AddProcesses,
                Payload = processes
            };

            await _webSocketConnection.SendAsync(message, _cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.AddProcessesError(exception, exception);
        }
    }

    public async Task AddRuntimeInfo(
        string assemblyId,
        ProcessInfoCollectorData dataObject)
    {
        try
        {
            var message = new WebSocketMessage
            {
                Topic = Topics.UpdatingRuntime,
                Payload = new Dictionary<string, ProcessInfoCollectorData>()[assemblyId] = dataObject
            };

            await _webSocketConnection.SendAsync(message, _cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.AddRuntimeError(exception, exception);
        }
    }

    public async Task AddRuntimeInfo(IEnumerable<KeyValuePair<string, ProcessInfoCollectorData>> runtimeInfo)
    {
        try
        {
            var message = new WebSocketMessage
            {
                Topic = Topics.UpdatingRuntime,
                Payload = runtimeInfo
            };

            await _webSocketConnection.SendAsync(message, _cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.AddRuntimesError(exception, exception);
        }
    }

    public async Task AddSubsystem(
        Guid subsystemId,
        SubsystemInfo subsystem)
    {
        try
        {
            var message = new WebSocketMessage
            {
                Topic = Topics.AddingSubsystem,
                Payload = new Dictionary<Guid, SubsystemInfo>()[subsystemId] = subsystem
            };

            await _webSocketConnection.SendAsync(message, _cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.AddSubsystemsError(exception, exception);
        }
    }

    public async Task AddSubsystems(IEnumerable<KeyValuePair<Guid, SubsystemInfo>> subsystems)
    {
        try
        {
            var message = new WebSocketMessage
            {
                Topic = Topics.AddingSubsystem,
                Payload = subsystems
            };

            await _webSocketConnection.SendAsync(message, _cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.AddSubsystemsError(exception, exception);
        }
    }

    public async Task TerminateProcess(int pid)
    {
        try
        {
            var message = new WebSocketMessage
            {
                Topic = Topics.TeminatingProcess,
                Payload = pid
            };

            await _webSocketConnection.SendAsync(message, _cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.RemoveProcessByIdError(exception, exception);
        }
    }

    public async Task UpdateConnection(
        string assemblyId,
        ConnectionInfo connection)
    {
        try
        {
            var message = new WebSocketMessage
            {
                Topic = Topics.UpdatingConnection,
                Payload = new Dictionary<string, ConnectionInfo>()[assemblyId] = connection
            };

            await _webSocketConnection.SendAsync(message, _cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.UpdateConnectionError(exception, exception);
        }
    }

    public async Task UpdateEnvironmentVariables(
        string assemblyId,
        IEnumerable<KeyValuePair<string, string>> environmentVariables)
    {
        try
        {
            var message = new WebSocketMessage
            {
                Topic = Topics.UpdatingEnvironmentVariables,
                Payload = new Dictionary<string, IEnumerable<KeyValuePair<string, string>>>()[assemblyId] = environmentVariables
            };

            await _webSocketConnection.SendAsync(message, _cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.UpdateEnvironmentVariablesError(exception, exception);
        }
    }

    public async Task UpdateModules(
        string assemblyId,
        IEnumerable<ModuleInfo> modules)
    {
        try
        {
            var message = new WebSocketMessage
            {
                Topic = Topics.UpdatingModules,
                Payload = new Dictionary<string, IEnumerable<ModuleInfo>>()[assemblyId] = modules
            };

            await _webSocketConnection.SendAsync(message, _cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.UpdateModulesError(exception, exception);
        }
    }

    public async Task UpdateProcess(ProcessInfoData process)
    {
        try
        {
            var message = new WebSocketMessage
            {
                Topic = Topics.ChangedProcessInfo,
                Payload = process
            };

            await _webSocketConnection.SendAsync(message, _cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.UpdateProcessError(exception, exception);
        }
    }

    public async Task UpdateProcessStatus(KeyValuePair<int, Status> process)
    {
        try
        {
            var message = new WebSocketMessage
            {
                Topic = Topics.UpdatingProcessStatus,
                Payload = process
            };

            await _webSocketConnection.SendAsync(message, _cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.UpdateProcessStatusError(process.Key, exception, exception);
        }
    }

    public async Task UpdateRegistrations(
        string assemblyId,
        IEnumerable<RegistrationInfo> registrations)
    {
        try
        {
            var message = new WebSocketMessage
            {
                Topic = Topics.UpdatingRegistrations,
                Payload = new Dictionary<string, IEnumerable<RegistrationInfo>>()[assemblyId] = registrations
            };

            await _webSocketConnection.SendAsync(message, _cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.UpdateRegistrationsError(exception, exception);
        }
    }

    public async Task UpdateSubsystemInfo(
        Guid subsystemId,
        SubsystemInfo subsystem)
    {
        try
        {
            var message = new WebSocketMessage
            {
                Topic = Topics.UpdatingSubsystem,
                Payload = new Dictionary<Guid, SubsystemInfo>()[subsystemId] = subsystem
            };

            await _webSocketConnection.SendAsync(message, _cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.UpdateSubsystemError(exception, exception);
        }
    }
}
