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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using LocalCollector;
using LocalCollector.Connections;
using LocalCollector.Modules;
using LocalCollector.Registrations;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ProcessExplorer.Abstractions;
using ProcessExplorer.Abstractions.Infrastructure;
using ProcessExplorer.Abstractions.Processes;
using ProcessExplorer.Abstractions.Subsystems;
using ProcessExplorer.Core.Tests.Subsystems;
using Xunit;

namespace ProcessExplorer.Core.Tests;

public class ProcessInfoAggregatorTests
{
    [Fact]
    public async Task RunSubsystemQueue_will_cancel_after_timeout()
    {
        //Creating a token to cancel after 3 seconds
        var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(3));

        //Creating mocks to handle method
        var mockUiHandler = new Mock<IUiHandler>();
        var mockSubsystemController = new Mock<ISubsystemController>();
        var mockProcessInfoMonitor = new Mock<ProcessInfoMonitor>(NullLogger.Instance);
        var clientMock = new Mock<IClientConnection<SubsystemLauncherTests.DummyStartType>>();
        var processInfoAggregator = new ProcessInfoAggregator(
            mockProcessInfoMonitor.Object,
            mockUiHandler.Object,
            mockSubsystemController.Object,
            NullLogger<IProcessInfoAggregator>.Instance);

        //Run in the background
        var task = Task.Run(() => processInfoAggregator.RunSubsystemStateQueue(cancellationTokenSource.Token));

        //Schedule a modification
        var id = Guid.NewGuid();
        processInfoAggregator.ScheduleSubsystemStateChanged(id, SubsystemState.Started);

        //Add a mock ui connection to send to.
        processInfoAggregator.UiHandler.AddClientConnection(id, clientMock.Object);

        //Wait for the task to finish
        await task;

        cancellationTokenSource.IsCancellationRequested.Should().BeTrue();
    }

    [Fact]
    public void SetComposePid_will_set_the_main_id()
    {
        var dummyPid = 1;

        var processInfoAggregator = CreateProcessInfoAggregator();

        processInfoAggregator.SetMainProcessId(dummyPid);

        var result = processInfoAggregator.MainProcessId;

        result.Should().Be(dummyPid);
    }

    [Fact]
    public void SetDeadProcessRemovalDelay_will_set_the_delay_of_process_deleting_from_the_collection()
    {
        var processInfoAggregator = CreateProcessInfoAggregator();

        var dummyDelay = 0;
        processInfoAggregator.SetDeadProcessRemovalDelay(dummyDelay);

        var result = processInfoAggregator.TerminatingProcessDelay;

        result.Should().Be(dummyDelay);
    }

    [Fact]
    public async Task RemoveRuntimeInformation_will_remove_item_from_collection()
    {
        var processInfoAggregator = CreateProcessInfoAggregator();

        var dummyRuntimeInfo = new ProcessInfoCollectorData()
        {
            Id = 2,
            Connections = new() 
            { 
                new() { Id = Guid.NewGuid(), Name = "dummy" }, new() { Id = Guid.NewGuid(), Name = "dummy2" }, 
                new() { Id = Guid.NewGuid(), Name = "dummy3" } 
            },
            Registrations = new() 
            { 
                new() { ImplementationType = "dummyImpl", LifeTime = "dummyLT", ServiceType = "dummyST" }, 
                new() { ImplementationType = "dummyImpl", LifeTime = "dummyLT", ServiceType = "dummyST" }, 
                new() { ImplementationType = "dummyImpl", LifeTime = "dummyLT", ServiceType = "dummyST" }
            }
        };

        var id = "dummyId";
        var id2 = "dummyId2";

        await processInfoAggregator.AddRuntimeInformation(id, dummyRuntimeInfo);
        await processInfoAggregator.AddRuntimeInformation(id2, dummyRuntimeInfo);

        var collection = processInfoAggregator.GetRuntimeInformation();

        collection.Should().HaveCount(2);
        collection.Should().Contain(new KeyValuePair<string, ProcessInfoCollectorData>(id, dummyRuntimeInfo));
        collection.Should().Contain(new KeyValuePair<string, ProcessInfoCollectorData>(id2, dummyRuntimeInfo));

        processInfoAggregator.RemoveRuntimeInformation(id);
        collection.Should().HaveCount(1);
        collection.Should().NotContain(new KeyValuePair<string, ProcessInfoCollectorData>(id, dummyRuntimeInfo));
        collection.Should().Contain(new KeyValuePair<string, ProcessInfoCollectorData>(id2, dummyRuntimeInfo));
    }

    [Theory]
    [ClassData(typeof(ConnectionTheoryData))]
    public async Task AddConnectionCollection_will_add_a_new_connection_collection_information(string id, IEnumerable<ConnectionInfo> connections)
    {
        var processInfoAggregator = CreateProcessInfoAggregator();

        //Add dummy data
        await processInfoAggregator.AddRuntimeInformation(id, new ProcessInfoCollectorData());
        await processInfoAggregator.AddConnectionCollection(id, connections);

        var collection = processInfoAggregator.GetRuntimeInformation();

        collection.Should().HaveCount(1);
        
        var result = collection.First();

        result.Key.Should().Be(id);
        result.Value.Connections.Should().BeEquivalentTo(connections);
    }

    [Fact]
    public async Task UpdateConnectionInfo_will_update_a_connection_information()
    {
        var processInfoAggregator = CreateProcessInfoAggregator();

        var connectionId = Guid.NewGuid();
        var wrongConnectionInfo = new ConnectionInfo { Id = connectionId, Name = "dummyName", LocalEndpoint = "http://dummyLocalEndpontWrong.com" };
        var id = "dummyId";

        await processInfoAggregator.AddRuntimeInformation(id, new ProcessInfoCollectorData()
        {
            Connections = new() { wrongConnectionInfo }
        });

        var collection = processInfoAggregator.GetRuntimeInformation();
        collection.Should().HaveCount(1);
        
        var result = collection.First().Value;
        result.Connections.Should().HaveCount(1);
        result.Connections.Should().Contain(wrongConnectionInfo);

        //updating
        var dummyConnectionInfo = new ConnectionInfo { Id = connectionId, Name = "dummyName", LocalEndpoint = "https://dummyLocalEndpoint.com" };
        await processInfoAggregator.UpdateOrAddConnectionInfo(id, dummyConnectionInfo);

        collection = processInfoAggregator.GetRuntimeInformation();
        result = collection.First().Value;

        collection.Should().HaveCount(1);
        result.Connections.Should().HaveCount(1);
        result.Connections.Should().NotContain(wrongConnectionInfo);
        result.Connections.Should().Contain(dummyConnectionInfo);
    }

    [Fact]
    public async Task UpdateEnvironmentVariablesInfo_will_update_environment_variables()
    {
        var processInfoAggregator = CreateProcessInfoAggregator();

        var id = "dummyId";
        var envs = new ConcurrentDictionary<string, string>();

        envs.TryAdd("dummyKey", "dummyValue");
        envs.TryAdd("wrongEnv", "wrongValue");

        await processInfoAggregator.AddRuntimeInformation(id, new()
        {
            EnvironmentVariables = envs,
        });

        var collection = processInfoAggregator.GetRuntimeInformation();
        collection.Should().HaveCount(1);
        collection.Should().ContainKey(id);

        var result = collection.First().Value;
        result.EnvironmentVariables.Should().HaveCount(2);
        result.EnvironmentVariables.Should().BeEquivalentTo(envs);

        var updatedEnvs = new Dictionary<string, string>()
        {
            { "wrongEnv", "newValue" },
            { "newKey", "value" }
        };

        var expectedResult = new Dictionary<string, string>()
        {
            { "dummyKey", "dummyValue" },
            { "wrongEnv", "newValue" },
            { "newKey", "value" }
        };

        await processInfoAggregator.UpdateOrAddEnvironmentVariablesInfo(id, updatedEnvs);
        collection = processInfoAggregator.GetRuntimeInformation();
        collection.Should().HaveCount(1);
        collection.Should().ContainKey(id);

        result = collection.First().Value;
        result.EnvironmentVariables.Should().HaveCount(3);
        result.EnvironmentVariables.Should().BeEquivalentTo(expectedResult);
    }

    [Fact]
    public async Task UpdateRegistrationInfo_will_update_registrations()
    {
        var processInfoAggregator = CreateProcessInfoAggregator();

        var id = "dummyId";
        var registrations = new SynchronizedCollection<RegistrationInfo>()
        {
            new RegistrationInfo()
            {
                ImplementationType = "dummyImplementation",
                LifeTime = "dummyLifetime",
                ServiceType = "dummyServiceType"
            }
        };

        await processInfoAggregator.AddRuntimeInformation(id, new()
        {
            Registrations = registrations
        });

        var collection = processInfoAggregator.GetRuntimeInformation();
        collection.Should().HaveCount(1);
        collection.Should().ContainKey(id);

        var result = collection.First().Value;
        result.Registrations.Should().HaveCount(1);
        result.Registrations.Should().BeEquivalentTo(registrations);

        var update = new List<RegistrationInfo>()
        {
            new() { ServiceType = "dummyImplementation", ImplementationType = "dummyNewImplementationType", LifeTime = "dummyLifeTime" },
            new() { ServiceType = "dummyImplementation2", ImplementationType = "dummyImplementationType2", LifeTime = "dummyLifeTime2" }
        };

        await processInfoAggregator.UpdateRegistrations(id, update);

        collection = processInfoAggregator.GetRuntimeInformation();
        collection.Should().HaveCount(1);

        result = collection.First().Value;
        result.Registrations.Should().HaveCount(3);

        var expected = new List<RegistrationInfo>()
        {
            new() { ImplementationType = "dummyImplementation", LifeTime = "dummyLifetime", ServiceType = "dummyServiceType" },
            new() { ServiceType = "dummyImplementation", ImplementationType = "dummyNewImplementationType", LifeTime = "dummyLifeTime" },
            new() { ServiceType = "dummyImplementation2", ImplementationType = "dummyImplementationType2", LifeTime = "dummyLifeTime2" }
        };

        result.Registrations.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task UpdateModuleInfo_will_update_modules()
    {
        var processInfoAggregator = CreateProcessInfoAggregator();

        var id = "dummyId";

        var modules = new SynchronizedCollection<ModuleInfo>()
        {
            new() { Name = "dummyModule", Location = "dummyLocation" }
        };

        await processInfoAggregator.AddRuntimeInformation(id, new()
        {
            Modules = modules
        });

        var update = new List<ModuleInfo>() { new() { Name = "dummyModule", Location = "newDummyLocation" } };

        await processInfoAggregator.UpdateOrAddModuleInfo(id, update);

        var collection = processInfoAggregator.GetRuntimeInformation();
        collection.Should().HaveCount(1);
        collection.Should().ContainKey(id);

        var result = collection.First().Value;
        result.Modules.Should().HaveCount(1);
        result.Modules.Should().BeEquivalentTo(update);
    }


    [Fact]
    public void EnableWatchingSavedProcesses_will_begin_to_watch_processes()
    {
        var mockSubsystemController = new Mock<ISubsystemController>();
        var mockProcessInfoMonitor = new Mock<IProcessInfoMonitor>();
        var mockUiHandler = new Mock<IUiHandler>();
        var processInfoAggregator = new ProcessInfoAggregator(
            mockProcessInfoMonitor.Object,
            mockUiHandler.Object,
            mockSubsystemController.Object,
            NullLogger<IProcessInfoAggregator>.Instance);

        processInfoAggregator.EnableWatchingSavedProcesses();
        mockProcessInfoMonitor.Verify(x => x.WatchProcesses(processInfoAggregator.MainProcessId), Times.Once);
    }


    [Fact]
    public void ScheduleSubsystemStateChanged_will_put_items_to_the_queue()
    {
        var id = Guid.NewGuid();
        var state = SubsystemState.Running;

        var processInfoAggregator = CreateProcessInfoAggregator();
        processInfoAggregator.ScheduleSubsystemStateChanged(id, state);

        var field = typeof(ProcessInfoAggregator).GetField("_subsystemStateChanges", BindingFlags.NonPublic | BindingFlags.Instance);
        if (field == null) throw new ArgumentNullException(nameof(field));
        
        var queue = (ConcurrentQueue<KeyValuePair<Guid, string>>)field.GetValue(processInfoAggregator);
        if (queue == null) throw new ArgumentNullException(nameof(queue));

        var succeed = queue.TryDequeue(out var result);
        succeed.Should().BeTrue();

        result.Key.Should().Be(id);
        result.Value.Should().Be(state);
    }

    private IProcessInfoAggregator CreateProcessInfoAggregator()
    {
        var mockSubsystemController = new Mock<ISubsystemController>();
        var mockProcessInfoMonitor = new Mock<IProcessInfoMonitor>();
        var mockUiHandler = new Mock<IUiHandler>();
        var processInfoAggregator = new ProcessInfoAggregator(
            mockProcessInfoMonitor.Object,
            mockUiHandler.Object,
            mockSubsystemController.Object,
            NullLogger<IProcessInfoAggregator>.Instance);

        return processInfoAggregator;
    }

    [Theory]
    [ClassData(typeof(RuntimeInfoTheoryData))]
    public async Task AddRuntimeInformation_will_add_a_new_info(string id, ProcessInfoCollectorData data)
    {
        var processInfoAggregator = CreateProcessInfoAggregator();

        await processInfoAggregator.AddRuntimeInformation(id, data);

        var collection = processInfoAggregator.GetRuntimeInformation();

        collection.Should().HaveCount(1);
        collection.Should().Contain(new KeyValuePair<string, ProcessInfoCollectorData>(id, data));

        var result = collection.First().Value;

        data.Connections.Count.Should().Be(result.Connections.Count);
        data.Connections.Should().BeEquivalentTo(result.Connections);
        data.EnvironmentVariables.Count.Should().Be(result.EnvironmentVariables.Count);
        data.EnvironmentVariables.Should().BeEquivalentTo(result.EnvironmentVariables);
        data.Modules.Count.Should().Be(result.Modules.Count);
        data.Modules.Should().BeEquivalentTo(result.Modules);
        data.Registrations.Count.Should().Be(result.Registrations.Count);
        data.Registrations.Should().BeEquivalentTo(result.Registrations);
    }


    [Theory]
    [ClassData(typeof(RuntimeInfoTheoryData))]
    public async Task AddRuntimeInformation_will_update_an_info(string id, ProcessInfoCollectorData data)
    {
        var processInfoAggregator = CreateProcessInfoAggregator();

        var dummyRuntimeInfo = new ProcessInfoCollectorData()
        {
            Id = 2,
            Connections = new()
            {
                new() { Id = Guid.NewGuid(), Name = "dummy" },
                new() { Id = Guid.NewGuid(), Name = "dummy2" },
                new() { Id = Guid.NewGuid(), Name = "dummy3" }
            },
            Registrations = new()
            {
                new() { ImplementationType = "dummyImpl", LifeTime = "dummyLT", ServiceType = "dummyST" },
                new() { ImplementationType = "dummyImpl", LifeTime = "dummyLT", ServiceType = "dummyST" },
                new() { ImplementationType = "dummyImpl", LifeTime = "dummyLT", ServiceType = "dummyST" }
            }
        };

        await processInfoAggregator.AddRuntimeInformation(id, dummyRuntimeInfo);

        //modifying the existing one
        await processInfoAggregator.AddRuntimeInformation(id, data);

        var collection = processInfoAggregator.GetRuntimeInformation();

        collection.Should().HaveCount(1);
        collection.Should().Contain(new KeyValuePair<string, ProcessInfoCollectorData>(id, data));

        var result = collection.First().Value;

        data.Connections.Count.Should().Be(result.Connections.Count);
        data.Connections.Should().BeEquivalentTo(result.Connections);
        data.EnvironmentVariables.Count.Should().Be(result.EnvironmentVariables.Count);
        data.EnvironmentVariables.Should().BeEquivalentTo(result.EnvironmentVariables);
        data.Modules.Count.Should().Be(result.Modules.Count);
        data.Modules.Should().BeEquivalentTo(result.Modules);
        data.Registrations.Count.Should().Be(result.Registrations.Count);
        data.Registrations.Should().BeEquivalentTo(result.Registrations);
    }


    private class RuntimeInfoTheoryData : TheoryData
    {
        public RuntimeInfoTheoryData()
        {
            AddRow("dummyId", new ProcessInfoCollectorData()
            {
                Id = 1,
                Connections = new() { new() { Id = Guid.NewGuid(), Name = "dummyConnection" } },
                Registrations = new() { new() { ImplementationType = "dummyImplementation", LifeTime = "dummyLifeTime", ServiceType = "dummyServiceType" } }
            });
            AddRow("dummyId2", new ProcessInfoCollectorData()
            {
                Id = 1,
                Connections = new() { new() { Id = Guid.NewGuid(), Name = "dummyConnection2" } },
                Registrations = new() { new() { ImplementationType = "dummyImplementation2", LifeTime = "dummyLifeTime2", ServiceType = "dummyServiceType2" }, new() { ImplementationType = "dummyImplementation1", LifeTime = "dummyLifeTime1", ServiceType = "dummyServiceType1" } }
            });
        }
    }

    private class ConnectionTheoryData : TheoryData
    {
        public ConnectionTheoryData()
        {
            AddRow("dummyId", new List<ConnectionInfo>()
            {
                new() { Id = Guid.NewGuid(), Name = "dummyConnection", LocalEndpoint = "dummyEndpoint" }
            });
        }
    }
}
