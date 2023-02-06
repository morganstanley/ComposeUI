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
using LocalCollector.Modules;
using LocalCollector.Registrations;
using Microsoft.Extensions.Logging.Abstractions;
using ProcessExplorer.Abstraction.Infrastructure;
using ProcessExplorer.Abstraction.Processes;
using ProcessExplorer.Abstraction.Subsystems;
using Super.RPC;
using SuperRPC_POC.Protocol.ProxyObjects;
using ConnectionInfo = LocalCollector.Connections.ConnectionInfo;

namespace SuperRPC_POC.ClientBehavior;

public class UIHandler : IUIHandler
{
    private SuperRPC? rpc;
    private IProcessAggregatorUIServiceObject? processServiceProxy;
    private ISubsystemServiceObject? subsystemServiceObject;
    private readonly ILogger<UIHandler> logger;

    public UIHandler(ILogger<UIHandler>? logger = null)
    {
        this.logger = logger ?? NullLogger<UIHandler>.Instance;
    }

    public async Task InitSuperRPCForProcessAsync(SuperRPC superRpc)
    {
        this.rpc = superRpc;
        await rpc.RequestRemoteDescriptors();
        processServiceProxy = rpc.GetProxyObject<IProcessAggregatorUIServiceObject>("ServiceProcessObject");
    }

    public async Task InitSuperRPCForSubsystemAsync(SuperRPC superRpc)
    {
        this.rpc = superRpc;
        await rpc.RequestRemoteDescriptors();
        subsystemServiceObject = rpc.GetProxyObject<ISubsystemServiceObject>("SubsystemServiceObject");
    }

    public async Task AddProcesses(IEnumerable<ProcessInfoData>? processes)
    {
        try
        {
            if (processServiceProxy == null)
            {
                return;
            }
           await processServiceProxy.AddProcesses(processes);
        }
        catch (Exception exception)
        {
            logger.LogError(exception.Message);
        }
    }

    public async Task AddProcess(ProcessInfoData process)
    {
        if (processServiceProxy == null)
        {
            return;
        }
        await processServiceProxy.AddProcess(process);
    }

    public async Task UpdateProcess(ProcessInfoData process)
    {
        if (processServiceProxy == null)
        {
            return;
        }
        await processServiceProxy.UpdateProcess(process);
    }

    public async Task RemoveProcess(int pid)
    {
        if (processServiceProxy == null)
        {
            return;
        }
        await processServiceProxy.TerminateProcess(pid);
    }

    public async Task AddRuntimeInfo(string assemblyId, ProcessInfoCollectorData dataObject)
    {
        if (processServiceProxy == null)
        {
            return;
        }
        await processServiceProxy.AddRuntimeInfo(assemblyId, dataObject);
    }

    public async Task AddConnections(string assemblyId, IEnumerable<ConnectionInfo> connections)
    {
        if (processServiceProxy == null)
        {
            return;
        }
        await processServiceProxy.AddConnections(assemblyId, connections);
    }

    public async Task AddConnection(string assemblyId, ConnectionInfo connection)
    {
        if (processServiceProxy == null)
        {
            return;
        }
        await processServiceProxy.AddConnection(assemblyId, connection);
    }

    public async Task UpdateConnection(string assemblyId, ConnectionInfo connection)
    {
        if (processServiceProxy == null)
        {
            return;
        }
        await processServiceProxy.UpdateConnection(assemblyId, connection);
    }

    public async Task UpdateEnvironmentVariables(string assemblyId, IEnumerable<KeyValuePair<string, string>> environmentVariables)
    {
        if (processServiceProxy == null)
        {
            return;
        }
        await processServiceProxy.UpdateEnvironmentVariables(assemblyId, environmentVariables);
    }

    public async Task UpdateRegistrations(string assemblyId, IEnumerable<RegistrationInfo> registrations)
    {
        if (processServiceProxy == null)
        {
            return;
        }
        await processServiceProxy.UpdateRegistrations(assemblyId, registrations);
    }

    public async Task UpdateModules(string assemblyId, IEnumerable<ModuleInfo> modules)
    {
        if (processServiceProxy == null)
        {
            return;
        }
        await processServiceProxy.UpdateModules(assemblyId, modules);
    }

    public async Task AddRuntimeInfo(IEnumerable<KeyValuePair<string, ProcessInfoCollectorData>> runtimeInfos)
    {
        if (processServiceProxy == null)
        {
            return;
        }
        await processServiceProxy.AddRuntimeInfos(runtimeInfos);
    }

    public async Task TerminateProcess(int pid)
    {
        if (processServiceProxy == null)
        {
            return;
        }
        await processServiceProxy.TerminateProcess(pid);
    }


    //subsystemPart
    public async Task UpdateSubsystemInfo(Guid subsystemId, SubsystemInfo subsystem)
    {
        if (subsystemServiceObject == null)
        {
            return;
        }
        await subsystemServiceObject.UpdateSubsystemInfoAsync(subsystemId.ToString(), subsystem);
    }

    public async Task AddSubsystems(IEnumerable<KeyValuePair<Guid, SubsystemInfo>> subsystems)
    {
        if (subsystemServiceObject == null)
        {
            return;
        }

        var subsystemsWithStringIds = new Dictionary<string, SubsystemInfo>();

        if (subsystems.Any())
        {
            foreach (var subsystem in subsystems)
            {
                subsystemsWithStringIds.Add(subsystem.Key.ToString(), subsystem.Value);
            }
        }

        await subsystemServiceObject.AddSubsystemsAsync(subsystemsWithStringIds);
    }

    public async Task AddSubsystem(Guid subsystemId, SubsystemInfo subsystem)
    {
        if (subsystemServiceObject == null)
        {
            return;
        }

        await subsystemServiceObject.AddSubsystemAsync(subsystemId.ToString(), subsystem);
    }

    public async Task RemoveSubsystem(Guid subsystemId)
    {
        if (subsystemServiceObject == null)
        {
            return;
        }

        await subsystemServiceObject.RemoveSubsystemAsync(subsystemId.ToString());
    }

    public Task UpdateProcessStatus(KeyValuePair<int, Status> process)
    {
        throw new NotImplementedException();
    }
}
