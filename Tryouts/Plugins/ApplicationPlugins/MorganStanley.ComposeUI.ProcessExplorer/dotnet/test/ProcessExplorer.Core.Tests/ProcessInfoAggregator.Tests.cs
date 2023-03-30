using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LocalCollector;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ProcessExplorer.Abstractions;
using ProcessExplorer.Abstractions.Infrastructure;
using ProcessExplorer.Abstractions.Processes;
using ProcessExplorer.Abstractions.Subsystems;
using ProcessExplorer.Core.Processes;
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
        var mockUiHandler = new Mock<IUIHandler>();
        var mockSubsystemController = new Mock<ISubsystemController>();
        var mockProcessInfoManager = new Mock<ProcessInfoManager>(NullLogger.Instance);

        var processInfoAggregator = new ProcessInfoAggregator(
            NullLogger<IProcessInfoAggregator>.Instance,
            mockProcessInfoManager.Object,
            mockSubsystemController.Object);

        //Run in the background
        var task = Task.Run(() => processInfoAggregator.RunSubsystemStateQueue(cancellationTokenSource.Token));

        //Schedule a modification
        var id = Guid.NewGuid();
        processInfoAggregator.ScheduleSubsystemStateChanged(id, SubsystemState.Started);

        //Add a mock ui connection to send to.
        processInfoAggregator.AddUiConnection(Guid.NewGuid(), mockUiHandler.Object);

        //Wait for the task to finish
        await task;

        Assert.True(cancellationTokenSource.IsCancellationRequested);
    }

    [Fact(Skip = "Run in Windows environment")]
    public void SetComposePid_will_set_the_main_id()
    {
        //Creating mocks to handle method
        var mockSubsystemController = new Mock<ISubsystemController>();

        //due it is an abstarct class we should create an instance of it, to test the set will be called.
#pragma warning disable CA1416 // Validate platform compatibility
        var processInfoManager = new WindowsProcessInfoManager(NullLogger<ProcessInfoManager>.Instance);
#pragma warning restore CA1416 // Validate platform compatibility

        var dummyPid = 1;

        var processInfoAggregator = new ProcessInfoAggregator(
            NullLogger<IProcessInfoAggregator>.Instance,
            processInfoManager,
            mockSubsystemController.Object);

        processInfoAggregator.SetComposePid(dummyPid);
        //using reflection to compare values
        var field = typeof(ProcessInfoManager).GetField("_composePid", BindingFlags.NonPublic | BindingFlags.Instance);

        var value = (int)field?.GetValue(processInfoManager);

        Assert.Equal(dummyPid, value);
    }

    [Fact(Skip = "Run in Windows environment")]
    public void SetDeadProcessRemovalDelay_will_set_the_delay_of_process_deleting_from_the_collection()
    {
        var mockSubsystemController = new Mock<ISubsystemController>();
#pragma warning disable CA1416 // Validate platform compatibility
        var processInfoManager = new WindowsProcessInfoManager(NullLogger<ProcessInfoManager>.Instance);
#pragma warning restore CA1416 // Validate platform compatibility


        var processInfoAggregator = new ProcessInfoAggregator(
            NullLogger<IProcessInfoAggregator>.Instance,
            processInfoManager,
            mockSubsystemController.Object);

        var dummyDelay = 0;
        processInfoAggregator.SetDeadProcessRemovalDelay(dummyDelay);

        var delayField = typeof(ProcessInfoManager).GetField("_delayTime", BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (int)delayField.GetValue(processInfoManager);

        Assert.Equal(dummyDelay, result);
    }

    [Fact]
    public void AddUiConnection_will_add_a_connection_information()
    {
        var mockUiHandler = new Mock<IUIHandler>();
        var mockSubsystemController = new Mock<ISubsystemController>();
        var mockProcessInfoManager = new Mock<ProcessInfoManager>(NullLogger.Instance);

        var processInfoAggregator = new ProcessInfoAggregator(
            NullLogger<IProcessInfoAggregator>.Instance,
            mockProcessInfoManager.Object,
            mockSubsystemController.Object);

        var id = Guid.NewGuid();

        processInfoAggregator.AddUiConnection(id, mockUiHandler.Object);

        var collectionOfUiHandlers = typeof(ProcessInfoAggregator).GetField("_uiClients", BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (ConcurrentDictionary<Guid, IUIHandler>)collectionOfUiHandlers?.GetValue(processInfoAggregator);

        Assert.Single(result);
        Assert.Equal(mockUiHandler.Object, result[id]);
    }

    [Fact]
    public void RemoveUiConnection_will_add_a_connection_information()
    {
        var mockUiHandler = new Mock<IUIHandler>();
        var mockSubsystemController = new Mock<ISubsystemController>();
        var mockProcessInfoManager = new Mock<ProcessInfoManager>(NullLogger.Instance);

        var processInfoAggregator = new ProcessInfoAggregator(
            NullLogger<IProcessInfoAggregator>.Instance,
            mockProcessInfoManager.Object,
            mockSubsystemController.Object);

        var id = Guid.NewGuid();

        processInfoAggregator.AddUiConnection(id, mockUiHandler.Object);

        var collectionOfUiHandlers = typeof(ProcessInfoAggregator).GetField("_uiClients", BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (ConcurrentDictionary<Guid, IUIHandler>)collectionOfUiHandlers?.GetValue(processInfoAggregator);

        Assert.Single(result);
        Assert.Equal(mockUiHandler.Object, result[id]);

        var otherId = Guid.NewGuid();
        processInfoAggregator.AddUiConnection(otherId, mockUiHandler.Object);
        Assert.Equal(2, result.Count);

        processInfoAggregator.RemoveUiConnection(new(id, mockUiHandler.Object));
        Assert.True(result.ContainsKey(otherId));
        Assert.Single(result);
    }

    [Theory]
    [ClassData(typeof(RuntimeInfoTheoryData))]
    public async Task AddRuntimeInformation_will_add_a_new_info(string id, ProcessInfoCollectorData data)
    {
        var mockSubsystemController = new Mock<ISubsystemController>();
        var mockProcessInfoManager = new Mock<ProcessInfoManager>(NullLogger.Instance);

        var processInfoAggregator = new ProcessInfoAggregator(
            NullLogger<IProcessInfoAggregator>.Instance,
            mockProcessInfoManager.Object,
            mockSubsystemController.Object);

        await processInfoAggregator.AddRuntimeInformation(id, data);

        var field = typeof(ProcessInfoAggregator).GetField("_processInformation", BindingFlags.NonPublic | BindingFlags.Instance);
        var collection = (ConcurrentDictionary<string, ProcessInfoCollectorData>)field?.GetValue(processInfoAggregator);

        var succeed = collection.TryGetValue(id, out ProcessInfoCollectorData? result);

        if (!succeed || result == null) throw new ArgumentNullException(nameof(result));

        Assert.Single(collection);
        Assert.True(collection?.ContainsKey(id));
        Assert.Equal(data.Connections.Count, result.Connections.Count);
        Assert.Equal(data.EnvironmentVariables.Count, result.EnvironmentVariables.Count);
        Assert.Equal(data.Modules.Count, result.Modules.Count);
        Assert.Equal(data.Registrations.Count, result.Registrations.Count);
    }

    [Theory]
    [ClassData(typeof(RuntimeInfoTheoryData))]
    public async Task AddRuntimeInformation_will_update_an_info(string id, ProcessInfoCollectorData data)
    {
        var mockSubsystemController = new Mock<ISubsystemController>();
        var mockProcessInfoManager = new Mock<ProcessInfoManager>(NullLogger.Instance);

        var processInfoAggregator = new ProcessInfoAggregator(
            NullLogger<IProcessInfoAggregator>.Instance,
            mockProcessInfoManager.Object,
            mockSubsystemController.Object);

        var dummyRuntimeInfo = new ProcessInfoCollectorData()
        {
            Id = 2,
            Connections = new() { new() { Id = Guid.NewGuid(), Name = "dummy" }, new() { Id = Guid.NewGuid(), Name = "dummy2" }, new() { Id = Guid.NewGuid(), Name = "dummy3" } },
            Registrations = new() { new() { ImplementationType = "dummyImpl", LifeTime = "dummyLT", ServiceType = "dummyST" }, new() { ImplementationType = "dummyImpl", LifeTime = "dummyLT", ServiceType = "dummyST" }, new() { ImplementationType = "dummyImpl", LifeTime = "dummyLT", ServiceType = "dummyST" } }
        };

        await processInfoAggregator.AddRuntimeInformation(id, dummyRuntimeInfo);

        //modifying the existing one
        await processInfoAggregator.AddRuntimeInformation(id, data);

        var field = typeof(ProcessInfoAggregator).GetField("_processInformation", BindingFlags.NonPublic | BindingFlags.Instance);
        var collection = (ConcurrentDictionary<string, ProcessInfoCollectorData>)field?.GetValue(processInfoAggregator);

        var succeed = collection.TryGetValue(id, out ProcessInfoCollectorData? result);

        if (!succeed || result == null) throw new ArgumentNullException(nameof(result));

        Assert.Single(collection);
        Assert.True(collection?.ContainsKey(id));
        Assert.Equal(data.Connections.Count, result.Connections.Count);
        Assert.Equal(data.EnvironmentVariables.Count, result.EnvironmentVariables.Count);
        Assert.Equal(data.Modules.Count, result.Modules.Count);
        Assert.Equal(data.Registrations.Count, result.Registrations.Count);
    }

    [Fact]
    public async Task RemoveRuntimeInformation_will_remove_item_from_collection()
    {
        var mockSubsystemController = new Mock<ISubsystemController>();
        var mockProcessInfoManager = new Mock<ProcessInfoManager>(NullLogger.Instance);

        var processInfoAggregator = new ProcessInfoAggregator(
            NullLogger<IProcessInfoAggregator>.Instance,
            mockProcessInfoManager.Object,
            mockSubsystemController.Object);

        var dummyRuntimeInfo = new ProcessInfoCollectorData()
        {
            Id = 2,
            Connections = new() { new() { Id = Guid.NewGuid(), Name = "dummy" }, new() { Id = Guid.NewGuid(), Name = "dummy2" }, new() { Id = Guid.NewGuid(), Name = "dummy3" } },
            Registrations = new() { new() { ImplementationType = "dummyImpl", LifeTime = "dummyLT", ServiceType = "dummyST" }, new() { ImplementationType = "dummyImpl", LifeTime = "dummyLT", ServiceType = "dummyST" }, new() { ImplementationType = "dummyImpl", LifeTime = "dummyLT", ServiceType = "dummyST" } }
        };

        var id = "dummyId";
        var id2 = "dummyId2";

        await processInfoAggregator.AddRuntimeInformation(id, dummyRuntimeInfo);
        await processInfoAggregator.AddRuntimeInformation(id2, dummyRuntimeInfo);

        var field = typeof(ProcessInfoAggregator).GetField("_processInformation", BindingFlags.NonPublic | BindingFlags.Instance);
        var collection = (ConcurrentDictionary<string, ProcessInfoCollectorData>)field?.GetValue(processInfoAggregator);

        Assert.Equal(2, collection?.Count);
        Assert.True(collection?.ContainsKey(id));

        processInfoAggregator.RemoveRuntimeInformation(id);
        Assert.False(collection?.ContainsKey(id));
        Assert.Single(collection);
    }

    //[Theory]
    //[ClassData(typeof(ConnectionTheoryData))]
    //public async Task AddConnectionCollection_will_add_a_new_connection_collection_information(string id, IEnumerable<ConnectionInfo> connections)
    //{

    //}

    //[Fact]
    //public async Task AddConnectionCollection_will_update_connection_collection_information(string id, IEnumerable<ConnectionInfo> connections)
    //{

    //}

    //[Fact]
    //public async Task UpdateConnectionInfo_will_update_a_connection_information(string id, ConnectionInfo connection)
    //{

    //}

    //[Fact]
    //public async Task UpdateEnvironmentVariablesInfo_will_update_environment_variables(string assemblyId, IEnumerable<KeyValuePair<string, string>> environmentVariables)
    //{

    //}

    //[Fact]
    //public async Task UpdateRegistrationInfo_will_update_registrations(string assemblyId, IEnumerable<RegistrationInfo> registrations)
    //{

    //}

    //[Fact]
    //public async Task UpdateModuleInfo_will_update_modules(string assemblyId, IEnumerable<ModuleInfo> modules)
    //{

    //}


    //[Fact]
    //public void EnableWatchingSavedProcesses_will_begin_to_watch_processes()
    //{

    //}

    //[Fact]
    //public void DisableWatchingProcesses_will_dispose_processmonitor()
    //{

    //}

    //[Fact]
    //public async Task ShutdownSubsystems_will_send_shutdown_command(IEnumerable<string> subsystemIds)
    //{

    //}

    //[Fact]
    //public async Task RestartSubsystems_will_send_restart_command(IEnumerable<string> subsystemIds)
    //{

    //}

    //[Fact]
    //public async Task LaunchSubsystems_will_send_launch_command(IEnumerable<string> subsystemIds)
    //{

    //}

    //[Fact]
    //public async Task LaunchSubsystemsWithDelay_will_send_launch_with_delay_command(IEnumerable<string> subsystemIds)
    //{

    //}

    //[Fact]
    //public async Task InitializeSubsystems_will_set_subsystems(IEnumerable<string> subsystemIds)
    //{

    //}

    //[Fact]
    //public async Task ModifySubsystemState_will_modify_the_state_of_the_item_in_the_collection(Guid subsystemId, string state)
    //{

    //}

    //[Fact]
    //public void SetSubsystemController_will_set_subsystemcontroller()
    //{

    //}

    //[Fact]
    //public void ScheduleSubsystemStateChanged_will_put_items_to_the_queue(Guid instanceId, string state)
    //{

    //}

    //[Fact]
    //public async Task InitializeProcesses_will_set_the_process_ids_to_watch()
    //{

    //}

    //[Fact]
    //public async Task AddProcesses_will_add_the_processes_to_the_existing_list_without_duplication()
    //{

    //}

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
            AddRow();
        }
    }
}


