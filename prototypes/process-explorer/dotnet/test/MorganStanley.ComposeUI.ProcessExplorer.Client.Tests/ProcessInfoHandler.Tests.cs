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
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Entities;
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Infrastructure;
using MorganStanley.ComposeUI.ProcessExplorer.Client;
using MorganStanley.ComposeUI.ProcessExplorer.Client.DependencyInjection;
using Xunit;

namespace MorganStanley.ComposeUI.ProcessExplorer.LocalHandler.Tests;

public class ProcessInfoHandlerTests
{
    private static readonly string _dummyAssemblyId = "dummyAssemblyId";
    private static readonly int _dummyProcessId = 6666;
    private static readonly string _dummyHost = "dummyHost";
    private static readonly int _dummyPort = 101010;
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
    private static readonly IServiceCollection _dummyServices = GetServiceCollection();

    [Fact]
    public async Task SendRuntimeInfo_will_call_communicator_AddRuntimeInfo_method()
    {
        var loggerMock = CreateProcessInfoHandlerLoggerMock();
        var communicatorMock = new Mock<ICommunicator>();
        var options = CreateLocalCollectorServiceOptions();

        var processInfoHandler = new ProcessInfoHandler(
            communicator: communicatorMock.Object,
            logger: loggerMock.Object,
            options: options);

        await processInfoHandler.SendRuntimeInfo();

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Sending runtime information collected by LocalCollector. Id:")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        communicatorMock.Verify(communicator => communicator.AddRuntimeInfo(It.IsAny<KeyValuePair<RuntimeInformation, ProcessInfoCollectorData>>()), Times.Once);
    }

    [Fact]
    public async Task AddConnections_will_add_or_update_connections_and_call_communicator()
    {
        var loggerMock = CreateProcessInfoHandlerLoggerMock();
        var communicatorMock = new Mock<ICommunicator>();
        var options = CreateLocalCollectorServiceOptions();

        var processInfoHandler = new ProcessInfoHandler(
            communicator: communicatorMock.Object,
            logger: loggerMock.Object,
            options: options);

        var updated = _dummyConnections.First();
        updated.UpdateConnection(remoteEndpoint: "dummyRemoteEndpoint");

        var newConnections = new List<IConnectionInfo>()
        {
            new ConnectionInfo(id: Guid.NewGuid(), name: "dummyConnection4", status: ConnectionStatus.Running),
            updated
        };

        await processInfoHandler.AddConnections(newConnections);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Sending connection collection collected by LocalCollector. Id: ")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        communicatorMock.Verify(communicator => communicator.AddConnectionCollection(It.IsAny<KeyValuePair<RuntimeInformation, IEnumerable<IConnectionInfo>>>()), Times.Once);

        var result = processInfoHandler.GetProcessInfoCollectorData().Connections.ToArray();
        Assert.Equal(4, result.Count());

        var expected = _dummyConnections.Concat(newConnections.Take(1));

        foreach (var connection in expected)
        {
            Assert.Contains(connection, result);
        }
    }

    [Fact]
    public async Task AddConnections_will_fail_with_null()
    {
        var loggerMock = CreateProcessInfoHandlerLoggerMock();
        var communicatorMock = new Mock<ICommunicator>();
        var options = CreateLocalCollectorServiceOptions();

        var processInfoHandler = new ProcessInfoHandler(
            communicator: communicatorMock.Object,
            logger: loggerMock.Object,
            options: options);

        var act = async () => await processInfoHandler.AddConnections(null);

        await Assert.ThrowsAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task AddEnvironmentVariables_will_add_or_update_environment_variables_and_call_communicator()
    {
        var loggerMock = CreateProcessInfoHandlerLoggerMock();
        var communicatorMock = new Mock<ICommunicator>();
        var options = CreateLocalCollectorServiceOptions();

        var processInfoHandler = new ProcessInfoHandler(
            communicator: communicatorMock.Object,
            logger: loggerMock.Object,
            options: options);

        var updatedEnvironmentVariables = new Dictionary<string, string>()
        {
            { "dummyKey1", "dummyValueNew" },
            { "dummyKey4", "dummyValue4" }
        };

        await processInfoHandler.AddEnvironmentVariables(updatedEnvironmentVariables);

        var result = processInfoHandler.GetProcessInfoCollectorData().EnvironmentVariables;

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Sending environment variables update collected by LocalCollector. Id:")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        Assert.Equal(4, result.Count());

        var expectedEnvironmentVariables = _dummyEnvironmentVariables
            .TakeLast(_dummyEnvironmentVariables.Count() - 1)
            .Concat(updatedEnvironmentVariables);

        foreach (var environmentVariable in expectedEnvironmentVariables)
        {
            Assert.Contains(environmentVariable, result);
        }
    }

    [Fact]
    public async Task AddEnvironmentVariables_will_fail_with_null()
    {
        var loggerMock = CreateProcessInfoHandlerLoggerMock();
        var communicatorMock = new Mock<ICommunicator>();
        var options = CreateLocalCollectorServiceOptions();

        var processInfoHandler = new ProcessInfoHandler(
            communicator: communicatorMock.Object,
            logger: loggerMock.Object,
            options: options);

        var act = async () => await processInfoHandler.AddEnvironmentVariables(null);

        await Assert.ThrowsAsync<NullReferenceException>(act);
    }

    [Fact]
    public async Task AddRegistrations_will_add_or_update_registrations_and_call_communicator()
    {
        var loggerMock = CreateProcessInfoHandlerLoggerMock();
        var communicatorMock = new Mock<ICommunicator>();
        var options = CreateLocalCollectorServiceOptions();

        var processInfoHandler = new ProcessInfoHandler(
            communicator: communicatorMock.Object,
            logger: loggerMock.Object,
            options: options);

        var updatedRegistrations = new List<RegistrationInfo>()
        {
            new()
            {
                ImplementationType = "newDummyImplementationType",
                LifeTime = "newDummyLifeTime",
                ServiceType = "newDummyServiceType"
            }
        };

        await processInfoHandler.AddRegistrations(updatedRegistrations);

        var result = processInfoHandler.GetProcessInfoCollectorData().Registrations;

        Assert.Contains(updatedRegistrations.First(), result);
        Assert.Equal(_dummyServices.Count() + 1, result.Count());
    }

    [Fact]
    public async Task AddRegistrations_will_fail_with_null()
    {
        var loggerMock = CreateProcessInfoHandlerLoggerMock();
        var communicatorMock = new Mock<ICommunicator>();
        var options = CreateLocalCollectorServiceOptions();

        var processInfoHandler = new ProcessInfoHandler(
            communicator: communicatorMock.Object,
            logger: loggerMock.Object,
            options: options);

        var act = async () => await processInfoHandler.AddRegistrations(null);

        await Assert.ThrowsAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task AddModules_will_add_or_update_modules_and_call_communicator()
    {
        var loggerMock = CreateProcessInfoHandlerLoggerMock();
        var communicatorMock = new Mock<ICommunicator>();
        var options = CreateLocalCollectorServiceOptions();

        var processInfoHandler = new ProcessInfoHandler(
            communicator: communicatorMock.Object,
            logger: loggerMock.Object,
            options: options);

        var updatedModules = new List<ModuleInfo>()
        {
            new() { Name = "dummyModule1" , Location = "dummyLocation1", Version = _dummyModules.First().Version, VersionRedirectedFrom = "newVersionRedirectedFrom" },
            new() { Name = "dummyModule4" , Location = "dummyLocation4", Version = Guid.NewGuid() }
        };

        await processInfoHandler.AddModules(updatedModules);

        var result = processInfoHandler.GetProcessInfoCollectorData().Modules;

        Assert.Equal(_dummyModules.Count() + 1, result.Count());

        var expectedModules = _dummyModules
            .TakeLast(_dummyModules.Count() - 1)
            .Concat(updatedModules);

        foreach (var expectedModule in expectedModules)
        {
            Assert.Contains(expectedModule, result);
        }
    }

    [Fact]
    public async Task AddModules_will_fail_with_null()
    {
        var loggerMock = CreateProcessInfoHandlerLoggerMock();
        var communicatorMock = new Mock<ICommunicator>();
        var options = CreateLocalCollectorServiceOptions();

        var processInfoHandler = new ProcessInfoHandler(
            communicator: communicatorMock.Object,
            logger: loggerMock.Object,
            options: options);

        var act = async () => await processInfoHandler.AddModules(null);

        await Assert.ThrowsAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task AddRuntimeInformation_will_add_or_update_all_the_information_and_call_communicator()
    {
        var loggerMock = CreateProcessInfoHandlerLoggerMock();
        var communicatorMock = new Mock<ICommunicator>();
        var options = CreateLocalCollectorServiceOptions();

        var processInfoHandler = new ProcessInfoHandler(
            communicator: communicatorMock.Object,
            logger: loggerMock.Object,
            options: options);

        var updated = _dummyConnections.First();
        updated.UpdateConnection(remoteEndpoint: "dummyRemoteEndpoint");

        var updatedConnections = new List<IConnectionInfo>()
        {
            new ConnectionInfo(id: Guid.NewGuid(), name: "dummyConnection4", status: ConnectionStatus.Running),
            updated
        };

        var updatedEnvironmentVariables = new Dictionary<string, string>()
        {
            { "dummyKey1", "dummyValueNew" },
            { "dummyKey4", "dummyValue4" }
        };

        var updatedRegistrations = new List<RegistrationInfo>()
        {
            new()
            {
                ImplementationType = "newDummyImplementationType",
                LifeTime = "newDummyLifeTime",
                ServiceType = "newDummyServiceType"
            },
            new()
            {
                ImplementationType = "DummyFakeService",
                ServiceType = "IDummyFakeService",
                LifeTime = "Transient"
            }
        };

        var updatedModules = new List<ModuleInfo>()
        {
            new() { Name = "dummyModule1" , Location = "dummyLocation1", Version = _dummyModules.First().Version, VersionRedirectedFrom = "newVersionRedirectedFrom" },
            new() { Name = "dummyModule4" , Location = "dummyLocation4", Version = Guid.NewGuid() }
        };

        await processInfoHandler.AddRuntimeInformation(
            connections: updatedConnections,
            environmentVariables: updatedEnvironmentVariables,
            registrations: updatedRegistrations,
            modules: updatedModules);

        var result = processInfoHandler.GetProcessInfoCollectorData();

        Assert.NotNull(result);

        //assertion of connections
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Sending connection collection collected by LocalCollector.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        Assert.Equal(4, result.Connections.Count());
        var expected = _dummyConnections.Concat(updatedConnections.Take(1));

        foreach (var connection in expected)
        {
            Assert.Contains(connection, result.Connections);
        }

        //assertion of environment variables
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Sending environment variables update collected by LocalCollector. Id:")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        Assert.Equal(4, result.EnvironmentVariables.Count());

        var expectedEnvironmentVariables = _dummyEnvironmentVariables
            .TakeLast(_dummyEnvironmentVariables.Count() - 1)
            .Concat(updatedEnvironmentVariables);

        foreach (var environmentVariable in expectedEnvironmentVariables)
        {
            Assert.Contains(environmentVariable, result.EnvironmentVariables);
        }

        //assertion of registrations
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Sending registrations update collected by LocalCollector. Id:")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        Assert.Contains(updatedRegistrations.First(), result.Registrations);
        Assert.Contains(updatedRegistrations.Last(), result.Registrations);
        Assert.Equal(_dummyServices.Count() + 1, result.Registrations.Count());

        //assertions of modules
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Sending modules update collected by LocalCollector. Id:")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        Assert.Equal(_dummyModules.Count() + 1, result.Modules.Count());

        var expectedModules = _dummyModules
            .TakeLast(_dummyModules.Count() - 1)
            .Concat(updatedModules);

        foreach (var expectedModule in expectedModules)
        {
            Assert.Contains(expectedModule, result.Modules);
        }
    }

    [Fact]
    public async Task AddRuntimeInformation_will_fail_with_null()
    {
        var loggerMock = CreateProcessInfoHandlerLoggerMock();
        var communicatorMock = new Mock<ICommunicator>();
        var options = CreateLocalCollectorServiceOptions();

        var processInfoHandler = new ProcessInfoHandler(
            communicator: communicatorMock.Object,
            logger: loggerMock.Object,
            options: options);

        var act = async () => await processInfoHandler.AddRuntimeInformation(
            connections: null,
            environmentVariables: null,
            registrations: null,
            modules: null);

        await Assert.ThrowsAsync<ArgumentNullException>(act);
    }

    private static ClientServiceOptions CreateLocalCollectorServiceOptions()
    {
        return new ClientServiceOptions()
        {
            AssemblyId = _dummyAssemblyId,
            ProcessId = _dummyProcessId,
            Host = _dummyHost,
            Port = _dummyPort,
            Connections = _dummyConnections,
            EnvironmentVariables = _dummyEnvironmentVariables,
            Modules = _dummyModules,
            LoadedServices = _dummyServices
        };
    }

    private static Mock<ILogger<IProcessInfoHandler>> CreateProcessInfoHandlerLoggerMock()
    {
        var loggerMock = new Mock<ILogger<IProcessInfoHandler>>();

        var loggerFilterOptions = new LoggerFilterOptions();

        loggerFilterOptions.AddFilter("", LogLevel.Debug);

        loggerMock
            .Setup(x => x.IsEnabled(It.IsAny<LogLevel>()))
            .Returns<LogLevel>(level => loggerFilterOptions.MinLevel <= level);

        return loggerMock;
    }

    private static IServiceCollection GetServiceCollection()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IDummyFakeService, DummyFakeService>();
        return serviceCollection;
    }

    private interface IDummyFakeService
    {
        string DummyMethod();
    }

    private class DummyFakeService : IDummyFakeService
    {
        public string DummyMethod()
        {
            return "dummy";
        }
    }
}
