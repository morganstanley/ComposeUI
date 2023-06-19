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

using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Entities;
using ProcessExplorer.Abstractions.Infrastructure.Protos;
using ProcessInfoCollectorData = MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Entities.ProcessInfoCollectorData;
using ProtoProcessInfoCollectorData = ProcessExplorer.Abstractions.Infrastructure.Protos.ProcessInfoCollectorData;

namespace MorganStanley.ComposeUI.ProcessExplorer.Server.Extensions;

internal static class ProtoConvertHelper
{
    public static ProcessInfoCollectorData DeriveProcessInfoCollectorData(
        this ProtoProcessInfoCollectorData protoProcessInfoCollectorData)
    {
        return new ProcessInfoCollectorData()
        {
            Id = protoProcessInfoCollectorData.Id,
            Registrations = protoProcessInfoCollectorData.Registrations.Select(registration => registration.DeriveRegistration()),
            Connections = protoProcessInfoCollectorData.Connections.Select(connection => connection.DeriveConnectionInfo()),
            Modules = protoProcessInfoCollectorData.Modules.Select(module => module.DeriveModule()),
            EnvironmentVariables = protoProcessInfoCollectorData.EnvironmentVariables,
        };
    }

    public static RegistrationInfo DeriveRegistration(
        this Registration protoRegistration)
    {
        return new()
        {
            ServiceType = protoRegistration.ServiceType,
            ImplementationType = protoRegistration.ImplementationType,
            LifeTime = protoRegistration.LifeTime
        };
    }

    public static IConnectionInfo DeriveConnectionInfo(
        this Connection protoConenctionInfo)
    {
        return new ConnectionInfo(
            id: Guid.Parse(protoConenctionInfo.Id),
            name: protoConenctionInfo.Name,
            status: protoConenctionInfo.Status.DeriveConnectionStatus(),
            localEndpoint: protoConenctionInfo.LocalEndpoint,
            remoteEndpoint: protoConenctionInfo.RemoteEndpoint,
            remoteApplication: protoConenctionInfo.RemoteApplication,
            remoteHostname: protoConenctionInfo.RemoteHostname,
            connectionInformation: protoConenctionInfo.ConnectionInformation);
    }

    public static ModuleInfo DeriveModule(
        this Module protoModule)
    {
        return new()
        {
            Name = protoModule.Name,
            Location = protoModule.Location,
            Version = Guid.Parse(protoModule.Version),
            VersionRedirectedFrom = protoModule.VersionRedirectedFrom,
            PublicKeyToken = protoModule.PublicKeyToken.ToArray()
        };
    }

    private static ConnectionStatus DeriveConnectionStatus(
        this string status)
    {
        return status switch
        {
            nameof(ConnectionStatus.Stopped) => ConnectionStatus.Stopped,
            nameof(ConnectionStatus.Running) => ConnectionStatus.Running,
            nameof(ConnectionStatus.Failed) => ConnectionStatus.Failed,
            _ => ConnectionStatus.Unknown
        };
    }
}
