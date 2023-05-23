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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions;
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Entities;
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Extensions;
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Infrastructure;
using MorganStanley.ComposeUI.ProcessExplorer.Core.DependencyInjection;
using MorganStanley.ComposeUI.ProcessExplorer.LocalCollector.DependencyInjection;
using MorganStanley.ComposeUI.ProcessExplorer.LocalCollector.Infrastructure;
using MorganStanley.ComposeUI.ProcessExplorer.Server.DependencyInjection;
using MorganStanley.ComposeUI.ProcessExplorer.Server.Server.Abstractions;
using Xunit;

namespace MorganStanley.ComposeUI.ProcessExplorer.IntegrationTests;

public class LocalCollectorEndToEndTests
{
    private IHost? _host;
    public readonly string Host = "localhost";
    public int Port;
    private static readonly string _dummyAssemblyId = "dummyAssemblyId";
    private static readonly int _dummyProcessId = 6666;
    private static readonly IEnumerable<IConnectionInfo> _dummyConnections = new List<ConnectionInfo>()
    {
        new(id: Guid.NewGuid(), name: "dummyConnection1", status: ConnectionStatus.Failed),
        new(id: Guid.NewGuid(), name: "dummyConnection2", status: ConnectionStatus.Stopped),
        new(id: Guid.NewGuid(), name: "dummyConnection3", status: ConnectionStatus.Running),
    };
    private static readonly IEnumerable<KeyValuePair<string, string>> _dummyEnvironmentVariables = new Dictionary<string, string>()
    {
        { "dummyKey1", "dummyValue1" },
        { "dummyKey2", "dummyValue2" },
        { "dummyKey3", "dummyValue3" },
    };
    private static readonly IEnumerable<ModuleInfo> _dummyModules = new List<ModuleInfo>()
    {
        new(){ Name = "dummyModule1" , Location = "dummyLocation1", Version = Guid.NewGuid() },
        new(){ Name = "dummyModule2" , Location = "dummyLocation2", Version = Guid.NewGuid() },
        new(){ Name = "dummyModule3" , Location = "dummyLocation3", Version = Guid.NewGuid() },
    };

    private static readonly IEnumerable<RegistrationInfo> _dummyRegistrations = new List<RegistrationInfo>()
    {
        new() {ImplementationType = "dummyImplementationType", ServiceType = "dummyServiceType", LifeTime = "dummyLifetime" }
    };

    public async Task DisposeAsync()
    {
        if (_host != null)
            await _host.StopAsync();
    }

    [Fact]
    public async Task AddRuntimeInfo_will_add_ProcessInfoCollectorData()
    {
        await InitializeAsync(new Random().Next(6000));
        var loggerMock = CreateGrpcCommunicatorLoggerMock();
        var client = new GrpcCommunicator(
            options: new LocalCollectorServiceOptions()
            {
                Host = Host,
                Port = Port
            },
            logger: loggerMock.Object);

        var processInfoCollectorData = CreateProcessInfoCollectorData();

        await client.AddRuntimeInfo(new(
            new() { Name = _dummyAssemblyId },
            processInfoCollectorData));

        var aggregator = _host?.Services.GetRequiredService<IProcessInfoAggregator>();
        if (aggregator == null) throw new ArgumentNullException(nameof(aggregator));

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Sending runtime information collected by LocalCollector.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        var resultProcessInfoCollectorDataCollection = aggregator.GetRuntimeInformation();
        Assert.Single(resultProcessInfoCollectorDataCollection);

        var result = resultProcessInfoCollectorDataCollection.First();
        Assert.Equal(result.Key, _dummyAssemblyId);
        Assert.Equal(_dummyConnections.Count(), result.Value.Connections.Count());
        foreach (var connection in _dummyConnections)
        {
            Assert.Contains(connection.Id, result.Value.Connections.Select(connection => connection.Id)); //Be aware protobuf message with null value couldn't be sent. And also we could use the FluentAssertions assertion instead of this approach to check if the 2 unordered collection equals
        }

        Assert.Equal(_dummyEnvironmentVariables.Count(), result.Value.EnvironmentVariables.Count());
        foreach (var environmentVariable in _dummyEnvironmentVariables)
        {
            Assert.Contains(environmentVariable, result.Value.EnvironmentVariables);
        }

        Assert.Equal(_dummyModules.Count(), result.Value.Modules.Count());
        foreach (var module in _dummyModules)
        {
            Assert.Contains(module.Version, result.Value.Modules.Select(module => module.Version));
        }

        Assert.Equal(_dummyRegistrations.Count(), result.Value.Registrations.Count());
        foreach (var registration in _dummyRegistrations)
        {
            Assert.Contains(registration, result.Value.Registrations);
        }

        Assert.Equal(_dummyProcessId, result.Value.Id);
        await DisposeAsync();
    }

    [Fact] 
    public async Task AddRuntimeInfo_will_fail_due_no_host()
    {
        var loggerMock = CreateGrpcCommunicatorLoggerMock();

        var client = new GrpcCommunicator(
            options: new LocalCollectorServiceOptions() { Host = Host, Port = Port },
            logger: loggerMock.Object);

        await client.AddRuntimeInfo(new(
            new() { Name = _dummyAssemblyId },
            CreateProcessInfoCollectorData()));

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("LocalCollector could not send message about runtimeinformation to the server. Detailed exception: ")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task AddConnectionCollection_will_update_collection()
    {
        await InitializeAsync(new Random().Next(6000));
        var loggerMock = CreateGrpcCommunicatorLoggerMock();

        var client = new GrpcCommunicator(
            options: new LocalCollectorServiceOptions() { Host = Host, Port = Port },
            logger: loggerMock.Object);

        var processInfoCollectorData = CreateProcessInfoCollectorData();

        await client.AddRuntimeInfo(new(
            new() { Name = _dummyAssemblyId },
            processInfoCollectorData));

        var index = new Random().Next(_dummyConnections.Count() - 1);
        var randomConnection = _dummyConnections
            .ElementAt(index);

        var updatedConnections = new List<IConnectionInfo>()
        {
            new ConnectionInfo(
                id: Guid.NewGuid(),
                name: "dummyNewConnection4",
                status: ConnectionStatus.Running),

            new ConnectionInfo(
                id: randomConnection.Id,
                name: randomConnection.Name,
                status: ConnectionStatus.Failed)
        };

        await client.AddConnectionCollection(
            new
                (new() { Name = _dummyAssemblyId }, 
                    updatedConnections));

        var aggregator = _host?.Services.GetRequiredService<IProcessInfoAggregator>();
        if (aggregator == null) throw new ArgumentNullException(nameof(aggregator));

        var expectedConnections = _dummyConnections
            .Replace(index, updatedConnections.Last())
            .Append(updatedConnections.First());

        var resultProcessInfoCollectorDataCollection = aggregator.GetRuntimeInformation();
        Assert.Single(resultProcessInfoCollectorDataCollection);

        var result = resultProcessInfoCollectorDataCollection.First();
        Assert.Equal(result.Key, _dummyAssemblyId);
        Assert.Equal(_dummyConnections.Count() + 1, result.Value.Connections.Count());

        foreach (var connection in expectedConnections)
        {
            Assert.Contains(connection, result.Value.Connections);
        }
        await DisposeAsync();
    }

    [Fact]
    public async Task AddConnectionCollection_will_return()
    {
        await InitializeAsync(new Random().Next(6000));
        var loggerMock = CreateGrpcCommunicatorLoggerMock();

        var client = new GrpcCommunicator(
            options: new LocalCollectorServiceOptions() { Host = Host, Port = Port },
            logger: loggerMock.Object);

        var processInfoCollectorData = CreateProcessInfoCollectorData();

        await client.AddRuntimeInfo(new(
            new() { Name = _dummyAssemblyId },
            processInfoCollectorData));

        await client.AddConnectionCollection(
            new
            (new() { Name = _dummyAssemblyId },
                Enumerable.Empty<IConnectionInfo>()));

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Sending connection collection collected by LocalCollector.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
        await DisposeAsync();
    }

    [Fact]
    public async Task AddConnectionCollection_will_fail_due_no_host()
    {
        var loggerMock = CreateGrpcCommunicatorLoggerMock();

        var client = new GrpcCommunicator(
            options: new LocalCollectorServiceOptions() { Host = Host, Port = Port },
            logger: loggerMock.Object);

        var updatedConnections = new List<IConnectionInfo>()
        {
            new ConnectionInfo(
                id: Guid.NewGuid(),
                name: "dummyNewConnection4",
                status: ConnectionStatus.Running),
        };

        await client.AddConnectionCollection(
            new
            (new() { Name = _dummyAssemblyId },
                updatedConnections));

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Sending connection collection collected by LocalCollector.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("LocalCollector could not send message about connection collection to the server. Detailed exception:")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateConnectionInformation_will_send_update()
    {
        await InitializeAsync(new Random().Next(6000));

        var loggerMock = CreateGrpcCommunicatorLoggerMock();

        var client = new GrpcCommunicator(
            options: new LocalCollectorServiceOptions() { Host = Host, Port = Port },
            logger: loggerMock.Object);

        var processInfoCollectorData = CreateProcessInfoCollectorData();

        await client.AddRuntimeInfo(new(
            new() { Name = _dummyAssemblyId },
            processInfoCollectorData));

        var index = new Random().Next(_dummyConnections.Count() - 1);
        
        var randomConnection = _dummyConnections
            .ElementAt(index);

        var updatedConnection = new ConnectionInfo(
            id: randomConnection.Id, 
            name: randomConnection.Name, 
            status: ConnectionStatus.Unknown,
            localEndpoint: "NewLocalEndpoint", 
            remoteEndpoint: "newRemoteEndpoint", 
            remoteApplication: "test");

        await client.UpdateConnectionInformation(
            new(
                new() { Name = _dummyAssemblyId }, 
                updatedConnection));

        var aggregator = _host?.Services.GetRequiredService<IProcessInfoAggregator>();
        if (aggregator == null) throw new ArgumentNullException(nameof(aggregator));

        var result = aggregator.GetRuntimeInformation()
            .First()
            .Value
            .Connections;

        Assert.Equal(_dummyConnections.Count(), result.Count());

        var resultConnection = result.FirstOrDefault(connection => connection.Id == updatedConnection.Id);

        Assert.NotNull(resultConnection);

        Assert.Equal(updatedConnection.Name, resultConnection.Name);
        Assert.Equal(updatedConnection.LocalEndpoint, resultConnection.LocalEndpoint);
        Assert.Equal(updatedConnection.RemoteApplication, resultConnection.RemoteApplication);
        Assert.Equal(updatedConnection.RemoteEndpoint, resultConnection.RemoteEndpoint);
        Assert.Empty(resultConnection.RemoteHostname);
        Assert.Empty(resultConnection.ConnectionInformation);
        Assert.Equal(updatedConnection.Status, resultConnection.Status);

        await DisposeAsync();
    }

    [Fact]
    public async Task UpdateConnectionInformation_will_fail_due_no_host()
    {
        var loggerMock = CreateGrpcCommunicatorLoggerMock();

        var client = new GrpcCommunicator(
            options: new LocalCollectorServiceOptions() { Host = Host, Port = Port },
            logger: loggerMock.Object);

        var index = new Random().Next(_dummyConnections.Count() - 1);

        var randomConnection = _dummyConnections
            .ElementAt(index);

        var updatedConnection = new ConnectionInfo(
            id: randomConnection.Id,
            name: randomConnection.Name,
            status: ConnectionStatus.Unknown,
            localEndpoint: "NewLocalEndpoint",
            remoteEndpoint: "newRemoteEndpoint",
            remoteApplication: "test");

        await client.UpdateConnectionInformation(
            new(
                new() { Name = _dummyAssemblyId },
                updatedConnection));

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Sending a connection collected by LocalCollector. Id:")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("LocalCollector could not send message about updating a connection to the server. Detailed exception:")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateEnvironmentVariableInformation_will_send_update()
    {
        await InitializeAsync(new Random().Next(6000));

        var loggerMock = CreateGrpcCommunicatorLoggerMock();

        var client = new GrpcCommunicator(
            options: new LocalCollectorServiceOptions() { Host = Host, Port = Port },
            logger: loggerMock.Object);

        var processInfoCollectorData = CreateProcessInfoCollectorData();

        await client.AddRuntimeInfo(new(
            new() { Name = _dummyAssemblyId },
            processInfoCollectorData));


        var updatedEnvironmentVariables = new Dictionary<string, string>()
        {
            { "newDummyKey1", "newDummyValue2" }
        };

        await client.UpdateEnvironmentVariableInformation(
            new(
                new RuntimeInformation() { Name = _dummyAssemblyId },
                updatedEnvironmentVariables));

        var aggregator = _host?.Services.GetRequiredService<IProcessInfoAggregator>();
        if (aggregator == null) throw new ArgumentNullException(nameof(aggregator));

        var result = aggregator.GetRuntimeInformation()
            .First()
            .Value
            .EnvironmentVariables;

        Assert.Equal(_dummyEnvironmentVariables.Count() + updatedEnvironmentVariables.Count(), result.Count());

        foreach (var environmentVariable in updatedEnvironmentVariables)
        {
            Assert.Contains(environmentVariable, result);
        }

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Sending environment variables update collected by LocalCollector. Id:")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateEnvironmentVariableInformation_will_return()
    {
        await InitializeAsync(new Random().Next(6000));
        var loggerMock = CreateGrpcCommunicatorLoggerMock();

        var client = new GrpcCommunicator(
            options: new LocalCollectorServiceOptions() { Host = Host, Port = Port },
            logger: loggerMock.Object);

        var processInfoCollectorData = CreateProcessInfoCollectorData();

        await client.AddRuntimeInfo(new(
            new() { Name = _dummyAssemblyId },
            processInfoCollectorData));

        await client.UpdateEnvironmentVariableInformation(
            new
            (new() { Name = _dummyAssemblyId },
                Enumerable.Empty<KeyValuePair<string, string>>()));

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Sending environment variables update collected by LocalCollector. Id:")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
        await DisposeAsync();
    }

    [Fact]
    public async Task UpdateEnvironmentVariableInformation_will_fail_due_no_host()
    {
        var loggerMock = CreateGrpcCommunicatorLoggerMock();

        var client = new GrpcCommunicator(
            options: new LocalCollectorServiceOptions() { Host = Host, Port = Port },
            logger: loggerMock.Object);

        var updatedEnvironmentVariables = new Dictionary<string, string>()
        {
            { "newDummyKey1", "newDummyValue2" }
        };

        await client.UpdateEnvironmentVariableInformation(
            new(
                new RuntimeInformation() { Name = _dummyAssemblyId },
                updatedEnvironmentVariables));

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Sending environment variables update collected by LocalCollector. Id: `{_dummyAssemblyId}`")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("LocalCollector could not send message about updating environment variables to the server. Detailed exception: ")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateRegistrationInformation_will_send_update()
    {
        await InitializeAsync(new Random().Next(6000));

        var loggerMock = CreateGrpcCommunicatorLoggerMock();

        var client = new GrpcCommunicator(
            options: new LocalCollectorServiceOptions() { Host = Host, Port = Port },
            logger: loggerMock.Object);

        var processInfoCollectorData = CreateProcessInfoCollectorData();

        await client.AddRuntimeInfo(new(
            new() { Name = _dummyAssemblyId },
            processInfoCollectorData));

        var registrationInfoToUpdate = _dummyRegistrations.First();

        var updatedRegistrations = new List<RegistrationInfo>()
        {
            new()
            {
                ImplementationType = "NewDummyImplementationType",
                ServiceType = "NewDummyServiceType",
                LifeTime = "NewDummyLifeTime"
            },
            new()
            {
                ImplementationType = registrationInfoToUpdate.ImplementationType,
                ServiceType = registrationInfoToUpdate.ServiceType,
                LifeTime = "Transient"
            }
        };

        await client.UpdateRegistrationInformation(
            new(
                new RuntimeInformation() { Name = _dummyAssemblyId },
                updatedRegistrations));

        var aggregator = _host?.Services.GetRequiredService<IProcessInfoAggregator>();
        if (aggregator == null) throw new ArgumentNullException(nameof(aggregator));

        var result = aggregator.GetRuntimeInformation()
            .First()
            .Value
            .Registrations;

        Assert.Equal(_dummyRegistrations.Count() + updatedRegistrations.Count() - 1, result.Count());

        foreach (var registration in updatedRegistrations)
        {
            Assert.Contains(registration, result);
        }

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Sending registrations update collected by LocalCollector. Id:")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateRegistrationInformation_will_return()
    {
        await InitializeAsync(new Random().Next(6000));
        var loggerMock = CreateGrpcCommunicatorLoggerMock();

        var client = new GrpcCommunicator(
            options: new LocalCollectorServiceOptions() { Host = Host, Port = Port },
            logger: loggerMock.Object);

        var processInfoCollectorData = CreateProcessInfoCollectorData();

        await client.AddRuntimeInfo(new(
            new() { Name = _dummyAssemblyId },
            processInfoCollectorData));

        await client.UpdateRegistrationInformation(
            new
            (new() { Name = _dummyAssemblyId },
                Enumerable.Empty<RegistrationInfo>()));

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Sending registrations update collected by LocalCollector. Id: `{_dummyAssemblyId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
        await DisposeAsync();
    }

    [Fact]
    public async Task UpdateRegistrationInformation_will_fail_due_no_host()
    {
        var loggerMock = CreateGrpcCommunicatorLoggerMock();

        var client = new GrpcCommunicator(
            options: new LocalCollectorServiceOptions() { Host = Host, Port = Port },
            logger: loggerMock.Object);

        var registrationInfoToUpdate = _dummyRegistrations.First();

        var updatedRegistrations = new List<RegistrationInfo>()
        {
            new()
            {
                ImplementationType = "NewDummyImplementationType",
                ServiceType = "NewDummyServiceType",
                LifeTime = "NewDummyLifeTime"
            },
            new()
            {
                ImplementationType = registrationInfoToUpdate.ImplementationType,
                ServiceType = registrationInfoToUpdate.ServiceType,
                LifeTime = "Transient"
            }
        };

        await client.UpdateRegistrationInformation(
            new(
                new RuntimeInformation() { Name = _dummyAssemblyId },
                updatedRegistrations));

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Sending registrations update collected by LocalCollector. Id: `{_dummyAssemblyId}`")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("LocalCollector could not send message about updating registrations to the server. Detailed exception:")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateModuleInformation_will_send_update()
    {
        await InitializeAsync(new Random().Next(6000));

        var loggerMock = CreateGrpcCommunicatorLoggerMock();

        var client = new GrpcCommunicator(
            options: new LocalCollectorServiceOptions() { Host = Host, Port = Port },
            logger: loggerMock.Object);

        var processInfoCollectorData = CreateProcessInfoCollectorData();

        await client.AddRuntimeInfo(new(
            new() { Name = _dummyAssemblyId },
            processInfoCollectorData));

        var moduleToUpdate = _dummyModules.First();

        var updatedModules = new List<ModuleInfo>()
        {
            new()
            {
                Name = moduleToUpdate.Name,
                Location = moduleToUpdate.Location,
                Version = moduleToUpdate.Version,
                PublicKeyToken = Encoding.ASCII.GetBytes("dummyPublicKeyToken")
            },
            new()
            {
                Name = "newModule",
                Location = "newDummyLocation",
                Version = Guid.NewGuid(),
            }
        };

        await client.UpdateModuleInformation(
            new(
                new RuntimeInformation() { Name = _dummyAssemblyId },
                updatedModules));

        var aggregator = _host?.Services.GetRequiredService<IProcessInfoAggregator>();
        if (aggregator == null) throw new ArgumentNullException(nameof(aggregator));

        var result = aggregator.GetRuntimeInformation()
            .First()
            .Value
            .Modules;

        Assert.Equal(_dummyModules.Count() + updatedModules.Count() - 1, result.Count());

        foreach (var module in updatedModules)
        {
            Assert.Contains(module, result);
        }

        var resultModule = result.FirstOrDefault(module => module.Version == updatedModules.First().Version);
        Assert.NotNull(resultModule);
        Assert.Equal(updatedModules.First().PublicKeyToken, resultModule.PublicKeyToken);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Sending modules update collected by LocalCollector. Id: `{_dummyAssemblyId}`")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateModuleInformation_will_return()
    {
        await InitializeAsync(new Random().Next(6000));
        var loggerMock = CreateGrpcCommunicatorLoggerMock();

        var client = new GrpcCommunicator(
            options: new LocalCollectorServiceOptions() { Host = Host, Port = Port },
            logger: loggerMock.Object);

        var processInfoCollectorData = CreateProcessInfoCollectorData();

        await client.AddRuntimeInfo(new(
            new() { Name = _dummyAssemblyId },
            processInfoCollectorData));

        await client.UpdateModuleInformation(
            new
            (new() { Name = _dummyAssemblyId },
                Enumerable.Empty<ModuleInfo>()));

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Sending modules update collected by LocalCollector. Id: `{_dummyAssemblyId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
        await DisposeAsync();
    }

    [Fact]
    public async Task UpdateModuleInformation_will_fail_due_no_host()
    {
        var loggerMock = CreateGrpcCommunicatorLoggerMock();

        var client = new GrpcCommunicator(
            options: new LocalCollectorServiceOptions() { Host = Host, Port = Port },
            logger: loggerMock.Object);

        var moduleToUpdate = _dummyModules.First();

        var updatedModules = new List<ModuleInfo>()
        {
            new()
            {
                Name = moduleToUpdate.Name,
                Location = moduleToUpdate.Location,
                Version = moduleToUpdate.Version,
                PublicKeyToken = Encoding.ASCII.GetBytes("dummyPublicKeyToken")
            },
            new()
            {
                Name = "newModule",
                Location = "newDummyLocation",
                Version = Guid.NewGuid(),
            }
        };

        await client.UpdateModuleInformation(
            new(
                new RuntimeInformation() { Name = _dummyAssemblyId },
                updatedModules));

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Sending modules update collected by LocalCollector. Id: `{_dummyAssemblyId}`")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("LocalCollector could not send message about updating modules to the server. Detailed exception:")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateConnectionStatus_will_send_update()
    {
        await InitializeAsync(new Random().Next(6000));

        var loggerMock = CreateGrpcCommunicatorLoggerMock();

        var client = new GrpcCommunicator(
            options: new LocalCollectorServiceOptions() { Host = Host, Port = Port },
            logger: loggerMock.Object);

        var processInfoCollectorData = CreateProcessInfoCollectorData();

        await client.AddRuntimeInfo(new(
            new() { Name = _dummyAssemblyId },
            processInfoCollectorData));

        var connectionToUpdate = _dummyConnections.First();

        await client.UpdateConnectionStatus(
            assemblyId: _dummyAssemblyId,
            connectionId: connectionToUpdate.Id.ToString(),
            connectionStatus: ConnectionStatus.Stopped);

        var aggregator = _host?.Services.GetRequiredService<IProcessInfoAggregator>();
        if (aggregator == null) throw new ArgumentNullException(nameof(aggregator));

        var result = aggregator.GetRuntimeInformation()
            .First()
            .Value
            .Connections;

        var resultConnection = result.FirstOrDefault(connection => connection.Id == connectionToUpdate.Id);
        Assert.NotNull(resultConnection);

        Assert.Equal(connectionToUpdate.Status, resultConnection.Status);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Sending connection status update collected by LocalCollector. Id: `{connectionToUpdate.Id.ToString()}`")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        await DisposeAsync();
    }

    [Fact]
    public async Task UpdateConnectionStatus_will_fail_due_no_host()
    {
        var loggerMock = CreateGrpcCommunicatorLoggerMock();

        var client = new GrpcCommunicator(
            options: new LocalCollectorServiceOptions() { Host = Host, Port = Port },
            logger: loggerMock.Object);
        
        var connectionToUpdate = _dummyConnections.First();

        await client.UpdateConnectionStatus(
            assemblyId: _dummyAssemblyId,
            connectionId: connectionToUpdate.Id.ToString(),
            connectionStatus: ConnectionStatus.Stopped);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"LocalCollector could not send message about updating connection status to the server. Id: `{connectionToUpdate.Id.ToString()}`. Detailed exception:")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private ProcessInfoCollectorData CreateProcessInfoCollectorData()
    {
        return new ProcessInfoCollectorData()
        {
            Connections = _dummyConnections,
            EnvironmentVariables = _dummyEnvironmentVariables,
            Id = _dummyProcessId,
            Modules = _dummyModules,
            Registrations = _dummyRegistrations
        };
    }

    private static Mock<ILogger<ICommunicator>> CreateGrpcCommunicatorLoggerMock()
    {
        var loggerMock = new Mock<ILogger<ICommunicator>>();

        var loggerFilterOptions = new LoggerFilterOptions();

        loggerFilterOptions.AddFilter("", LogLevel.Debug);

        loggerMock
            .Setup(x => x.IsEnabled(It.IsAny<LogLevel>()))
            .Returns<LogLevel>(level => loggerFilterOptions.MinLevel <= level);

        return loggerMock;
    }

    public async Task InitializeAsync(int port)
    {
        IHostBuilder builder = new HostBuilder();
        
        Port = port;

        builder.ConfigureServices(
            (context, services) => services
                .AddProcessExplorerWindowsServerWithGrpc(pe => pe.UseGrpc())
                .ConfigureSubsystemLauncher(Start, Stop, CreateDummyStartType, CreateDummyStopType)
                .Configure<ProcessExplorerServerOptions>(op =>
                {
                    op.Host = Host;
                    op.Port = port;
                }));

        _host = builder.Build();

        await _host.StartAsync();
    }

    private static DummyStartType CreateDummyStartType(Guid id, string name)
    {
        return new DummyStartType(id, name);
    }

    private static DummyStopType CreateDummyStopType(Guid id)
    {
        return new DummyStopType(id);
    }

    private static void Start(DummyStartType dummy) { }
    private static void Stop(DummyStopType dummy) { }

    private record DummyStartType(Guid id, string name);
    private record DummyStopType(Guid id);
}
