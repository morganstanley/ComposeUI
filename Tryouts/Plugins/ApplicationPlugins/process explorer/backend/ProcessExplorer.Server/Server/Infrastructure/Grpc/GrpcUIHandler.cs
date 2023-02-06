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

using Google.Protobuf.Collections;
using Grpc.Core;
using LocalCollector.Connections;
using LocalCollector.Modules;
using LocalCollector.Registrations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ProcessExplorer.Abstraction.Infrastructure;
using ProcessExplorer.Abstraction.Processes;
using ProcessExplorer.Abstraction.Subsystems;
using ProcessExplorer.Server.Logging;
using ProcessExplorer.Server.Server.Helper;
using ProcessExplorer.Server.Server.Infrastructure.Protos;

namespace ProcessExplorer.Server.Server.Infrastructure.Grpc;

internal class GrpcUIHandler : IUIHandler
{
    private readonly object _lock = new();
    private readonly ILogger _logger;
    private readonly KeyValuePair<Guid, IServerStreamWriter<Message>> stream;

    public GrpcUIHandler(
        IServerStreamWriter<Message> responseStream,
        Guid id,
        ILogger? logger)
    {
        _logger = logger ?? NullLogger.Instance;
        stream = new(id, responseStream);
    }

    public Task AddConnections(string assemblyId, IEnumerable<ConnectionInfo> connections)
    {
        try
        {
            var message = new Message()
            {
                Action = ActionType.AddConnectionListAction,
                AssemblyId = assemblyId,
                Connections = { connections.Select(conn => conn.DeriveProtoConnectionType()) }
            };

            lock (_lock)
                return stream.Value.WriteAsync(message);
        }
        catch (Exception exception)
        {
            _logger.AddConnectionsError(exception, exception);
        }

        return Task.CompletedTask;
    }

    public Task AddProcess(ProcessInfoData process)
    {
        try
        {
            var list = new List<Process>
            {
                process.DeriveProtoProcessType()
            };

            var message = new Message()
            {
                Action = ActionType.AddProcessAction,
                Processes = { list }
            };

            lock (_lock)
                return stream.Value.WriteAsync(message);
        }
        catch (Exception exception)
        {
            _logger.AddProcessError(exception, exception);
        }

        return Task.CompletedTask;
    }

    public Task AddProcesses(IEnumerable<ProcessInfoData> processes)
    {
        try
        {
            var message = new Message()
            {
                Action = ActionType.AddProcessListAction,
                Processes = { processes.Select(proc => proc.DeriveProtoProcessType()) }
            };

            lock (_lock)
                return stream.Value.WriteAsync(message);
        }
        catch (Exception exception)
        {
            _logger.AddProcessesError(exception, exception);
        }

        return Task.CompletedTask;
    }

    public Task AddRuntimeInfo(string assemblyId, LocalCollector.ProcessInfoCollectorData dataObject)
    {
        try
        {
            var list = new List<ProcessInfoCollectorData>()
            {
                dataObject.DeriveProtoRuntimeInfoType()
            };

            var message = new Message()
            {
                Action = ActionType.AddRuntimeInfoAction,
                AssemblyId = assemblyId,
                RuntimeInfo = { list }
            };

            lock (_lock)
                return stream.Value.WriteAsync(message);
        }
        catch (Exception exception)
        {
            _logger.AddRuntimeError(exception, exception);
        }

        return Task.CompletedTask;
    }

    public Task AddRuntimeInfo(IEnumerable<KeyValuePair<string, LocalCollector.ProcessInfoCollectorData>> runtimeInfo)
    {
        try
        {
            var message = new Message()
            {
                Action = ActionType.AddMultipleRuntimeInfoAction,
                MultipleRuntimeInfo = { runtimeInfo.DeriveProtoDictionaryType(ProtoConvertHelper.DeriveProtoRuntimeInfoType) }
            };

            lock (_lock)
                return stream.Value.WriteAsync(message);
        }
        catch (Exception exception)
        {
            _logger.AddRuntimesError(exception, exception);
        }

        return Task.CompletedTask;
    }

    public Task TerminateProcess(int pid)
    {
        try
        {
            var message = new Message()
            {
                Action = ActionType.RemoveProcessByIdAction,
                Pid = pid
            };

            lock (_lock)
                return stream.Value.WriteAsync(message);
        }
        catch (Exception exception)
        {
            _logger.RemoveProcessByIdError(exception, exception);
        }

        return Task.CompletedTask;
    }

    public Task UpdateConnection(string assemblyId, ConnectionInfo connection)
    {
        try
        {
            var list = new List<Connection>()
            {
                connection.DeriveProtoConnectionType()
            };

            var message = new Message()
            {
                Action = ActionType.UpdateConnectionAction,
                Connections = { list }
            };

            lock (_lock)
                return stream.Value.WriteAsync(message);
        }
        catch (Exception exception)
        {
            _logger.UpdateConnectionError(exception, exception);
        }

        return Task.CompletedTask;
    }

    public Task UpdateEnvironmentVariables(string assemblyId, IEnumerable<KeyValuePair<string, string>> environmentVariables)
    {
        try
        {
            var message = new Message()
            {
                Action = ActionType.UpdateEnvironmentVariablesAction,
                AssemblyId = assemblyId,
                EnvironmentVariables = { environmentVariables.DeriveProtoDictionaryType() }
            };

            lock (_lock)
                return stream.Value.WriteAsync(message);
        }
        catch (Exception exception)
        {
            _logger.UpdateEnvironmentVariablesError(exception, exception);
        }

        return Task.CompletedTask;
    }

    public Task UpdateModules(string assemblyId, IEnumerable<ModuleInfo> modules)
    {
        try
        {
            var message = new Message()
            {
                Action = ActionType.UpdateModulesAction,
                AssemblyId = assemblyId,
                Modules = { modules.Select(module => module.DeriveProtoModuleType()) }
            };

            lock (_lock)
                return stream.Value.WriteAsync(message);
        }
        catch (Exception exception)
        {
            _logger.UpdateModulesError(exception, exception);
        }

        return Task.CompletedTask;
    }

    public Task UpdateProcess(ProcessInfoData process)
    {
        try
        {
            var list = new List<Process>()
            {
                process.DeriveProtoProcessType()
            };

            var message = new Message()
            {
                Action = ActionType.UpdateProcessAction,
                Processes = { list }
            };

            lock (_lock)
                return stream.Value.WriteAsync(message);
        }
        catch (Exception exception)
        {
            _logger.UpdateProcessError(exception, exception);
        }

        return Task.CompletedTask;
    }

    public Task UpdateRegistrations(string assemblyId, IEnumerable<RegistrationInfo> registrations)
    {
        try
        {
            var message = new Message()
            {
                Action = ActionType.UpdateRegistrationsAction,
                AssemblyId = assemblyId,
                Registrations = { registrations.Select(registration => registration.DeriveProtoRegistrationType()) }
            };

            lock (_lock)
                return stream.Value.WriteAsync(message);
        }
        catch (Exception exception)
        {
            _logger.UpdateRegistrationsError(exception, exception);
        }

        return Task.CompletedTask;
    }

    public Task AddSubsystem(Guid subsystemId, SubsystemInfo subsystem)
    {
        try
        {
            var map = new MapField<string, Subsystem>
            {
                { subsystemId.ToString(), subsystem.DeriveProtoSubsystemType() }
            };

            var message = new Message()
            {
                Action = ActionType.AddSubsystemAction,
                Subsystems = { map }
            };

            lock (_lock)
                return stream.Value.WriteAsync(message);
        }
        catch (Exception exception)
        {
            _logger.LogError("Error {exception}", exception);
        }

        return Task.CompletedTask;
    }

    public Task AddSubsystems(IEnumerable<KeyValuePair<Guid, SubsystemInfo>> subsystems)
    {
        try
        {
            var subsystemsCopy = subsystems.Select(subsystem => new KeyValuePair<string, SubsystemInfo>(subsystem.Key.ToString(), subsystem.Value));

            var message = new Message()
            {
                Action = ActionType.AddSubsystemsAction,
                Subsystems = { subsystemsCopy.DeriveProtoDictionaryType(ProtoConvertHelper.DeriveProtoSubsystemType) }
            };

            lock (_lock)
                return stream.Value.WriteAsync(message);
        }
        catch (Exception exception)
        {
            _logger.AddSubsystemsError(exception, exception);
        }

        return Task.CompletedTask;
    }

    public Task UpdateSubsystemInfo(Guid subsystemId, SubsystemInfo subsystem)
    {
        try
        {
            var map = new MapField<string, Subsystem>()
            {
                { subsystemId.ToString(), subsystem.DeriveProtoSubsystemType() }
            };

            var message = new Message()
            {
                Action = ActionType.UpdateSubsystemAction,
                Subsystems = { map }
            };

            lock (_lock)
                return stream.Value.WriteAsync(message);
        }
        catch (Exception exception)
        {
            _logger.UpdateSubsystemError(exception, exception);
        }

        return Task.CompletedTask;
    }

    public Task UpdateProcessStatus(KeyValuePair<int, Abstraction.Processes.Status> process)
    {
        try
        {
            var map = new MapField<int, string>
            {
                { process.Key, process.Value.ToString() }
            };

            var message = new Message()
            {
                Action = ActionType.UpdateProcessStatusAction,
                ProcessStatusChanges = { map }
            };

            lock (_lock)
                return stream.Value.WriteAsync(message);
        }
        catch (Exception exception)
        {
            _logger.UpdateProcessStatusError(process.Key, exception, exception);
        }

        return Task.CompletedTask;
    }
}
