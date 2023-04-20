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
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProcessExplorer.Abstractions.Infrastructure;
using ProcessExplorer.Abstractions.Subsystems;
using ProcessExplorer.Core.Subsystems;
using Xunit;

namespace ProcessExplorer.Core.Tests.Subsystems;

public class SubsystemControllerTests
{
    [Theory]
    [ClassData(typeof(SubsystemControllerTheoryData))]
    public async Task InitializeSubsystems_will_add_elements_trigger_automated_start_and_update_to_the_ui(IEnumerable<KeyValuePair<Guid, SubsystemInfo>> subsystems)
    {
        var loggerMock = CreateLoggerMock();
        var subsystemLauncherMock = new Mock<ISubsystemLauncher>();
        var subsystemController = CreateSubsystemController(
            logger: loggerMock.Object,
            dummySubsystemLauncher: subsystemLauncherMock.Object);
        var uiDelegateMock = new Mock<Func<Func<IUiHandler, Task>, Task>>();

        subsystemController.SetUiDelegate(uiDelegateMock.Object);

        await subsystemController.InitializeSubsystems(subsystems);

        subsystemLauncherMock.Verify(x => x.LaunchSubsystems(It.IsAny<Dictionary<Guid, string>>()), Times.Once);

        uiDelegateMock.Verify(x => x.Invoke(It.IsAny<Func<IUiHandler, Task>>()));
    }

    [Theory]
    [ClassData(typeof(SubsystemControllerTheoryData))]
    public async Task LaunchAllRegisteredSubsystem_will_use_subsystemLauncher_to_launch_subsystems(IEnumerable<KeyValuePair<Guid, SubsystemInfo>> subsystems)
    {
        var loggerMock = CreateLoggerMock();
        var subsystemLauncherMock = new Mock<ISubsystemLauncher>();
        var subsystemController = CreateSubsystemController(
            logger: loggerMock.Object,
            dummySubsystemLauncher: subsystemLauncherMock.Object);
        var uiDelegateMock = new Mock<Func<Func<IUiHandler, Task>, Task>>();

        subsystemController.SetUiDelegate(uiDelegateMock.Object);

        await subsystemController.InitializeSubsystems(subsystems);

        await subsystemController.LaunchAllRegisteredSubsystem();

        subsystemLauncherMock.Verify(x => x.LaunchSubsystems(It.IsAny<Dictionary<Guid, string>>()), Times.Once);
    }

    [Theory]
    [ClassData(typeof(SubsystemControllerTheoryData))]
    public async Task LaunchAllRegisteredSubsystem_will_use_subsystemCommunicator_to_launch_subsystems(IEnumerable<KeyValuePair<Guid, SubsystemInfo>> subsystems)
    {
        var loggerMock = CreateLoggerMock();
        var subsystemLauncherMock = new Mock<ISubsystemLauncher>();
        var subsystemController = CreateSubsystemController(
            logger: loggerMock.Object,
            dummySubsystemLauncher: subsystemLauncherMock.Object);
        var uiDelegateMock = new Mock<Func<Func<IUiHandler, Task>, Task>>();

        subsystemController.SetUiDelegate(uiDelegateMock.Object);

        await subsystemController.InitializeSubsystems(subsystems);

        await subsystemController.LaunchAllRegisteredSubsystem();

        subsystemLauncherMock.Verify(x => x.LaunchSubsystems(It.IsAny<Dictionary<Guid, string>>()), Times.Once);
    }

    [Theory]
    [ClassData(typeof(SubsystemControllerTheoryData))]
    public async Task LaunchSubsystemAfterTime_will_use_subsystemLauncher_to_launch_subsystems(IEnumerable<KeyValuePair<Guid, SubsystemInfo>> subsystems)
    {
        var loggerMock = CreateLoggerMock();
        var subsystemLauncherMock = new Mock<ISubsystemLauncher>();
        var subsystemController = CreateSubsystemController(
            logger: loggerMock.Object,
            dummySubsystemLauncher: subsystemLauncherMock.Object);
        var uiDelegateMock = new Mock<Func<Func<IUiHandler, Task>, Task>>();

        subsystemController.SetUiDelegate(uiDelegateMock.Object);

        await subsystemController.InitializeSubsystems(subsystems);

        var stoppedSubsystems = subsystems.Where(x => x.Value.State == SubsystemState.Stopped && !x.Value.AutomatedStart);
        var random = new Random().Next(0, stoppedSubsystems.Count());

        var subsystem = stoppedSubsystems.ElementAt(random);
        var time = 100;

        await subsystemController.LaunchSubsystemAfterTime(subsystem.Key, time);

        subsystemLauncherMock.Verify(x => x.LaunchSubsystemAfterTime(It.IsAny<Guid>(), It.IsAny<string>(), time), Times.Once);
    }

    [Theory]
    [ClassData(typeof(SubsystemControllerTheoryData))]
    public async Task LaunchSubsystemAfterTime_will_use_subsystemCommunicator_to_launch_subsystems(IEnumerable<KeyValuePair<Guid, SubsystemInfo>> subsystems)
    {
        var loggerMock = CreateLoggerMock();
        var subsystemLauncherCommunicatorMock = new Mock<ISubsystemLauncherCommunicator>();
        var subsystemController = CreateSubsystemController(
            logger: loggerMock.Object,
            dummySubsystemLauncherCommunicator: subsystemLauncherCommunicatorMock.Object);
        var uiDelegateMock = new Mock<Func<Func<IUiHandler, Task>, Task>>();

        subsystemController.SetUiDelegate(uiDelegateMock.Object);

        await subsystemController.InitializeSubsystems(subsystems);

        var stoppedSubsystems = subsystems.Where(x => x.Value.State == SubsystemState.Stopped && !x.Value.AutomatedStart);
        var random = new Random().Next(0, stoppedSubsystems.Count());

        var subsystem = stoppedSubsystems.ElementAt(random);
        var time = 100;

        await subsystemController.LaunchSubsystemAfterTime(subsystem.Key, time);

        subsystemLauncherCommunicatorMock.Verify(x => x.SendLaunchSubsystemAfterTimeRequest(It.IsAny<Guid>(), It.IsAny<string>(), time), Times.Once);
    }

    [Fact]
    public async Task LaunchSubsystemAfterTime_will_return_without_triggering_anything()
    {
        var loggerMock = CreateLoggerMock();
        var subsystemLauncherCommunicatorMock = new Mock<ISubsystemLauncherCommunicator>();
        var subsystemController = CreateSubsystemController(
            logger: loggerMock.Object,
            dummySubsystemLauncherCommunicator: subsystemLauncherCommunicatorMock.Object);
        var uiDelegateMock = new Mock<Func<Func<IUiHandler, Task>, Task>>();

        subsystemController.SetUiDelegate(uiDelegateMock.Object);

        var subsystems = new Dictionary<Guid, SubsystemInfo>();
        await subsystemController.InitializeSubsystems(subsystems);

        var time = 100;

        await subsystemController.LaunchSubsystemAfterTime(Guid.NewGuid(), time);

        subsystemLauncherCommunicatorMock.Verify(x => x.SendLaunchSubsystemAfterTimeRequest(It.IsAny<Guid>(), It.IsAny<string>(), time), Times.Never);
    }

    [Theory]
    [ClassData(typeof(SubsystemControllerTheoryData))]
    public async Task LaunchSubsystemAutomatically_will_use_subsystemLauncher_to_launch_subsystems(IEnumerable<KeyValuePair<Guid, SubsystemInfo>> subsystems)
    {
        var loggerMock = CreateLoggerMock();
        var subsystemLauncherMock = new Mock<ISubsystemLauncher>();
        var subsystemController = CreateSubsystemController(
            logger: loggerMock.Object,
            dummySubsystemLauncher: subsystemLauncherMock.Object);
        var uiDelegateMock = new Mock<Func<Func<IUiHandler, Task>, Task>>();

        subsystemController.SetUiDelegate(uiDelegateMock.Object);

        await subsystemController.InitializeSubsystems(subsystems);

        var subsystem = subsystems.First(x => x.Value.State == SubsystemState.Stopped && !x.Value.AutomatedStart);

        await subsystemController.LaunchSubsystemAutomatically(subsystem.Key);

        subsystem.Value.AutomatedStart.Should().BeTrue();

        subsystemLauncherMock.Verify(x => x.LaunchSubsystem(It.IsAny<Guid>(), It.IsAny<string>()));
    }

    [Theory]
    [ClassData(typeof(SubsystemControllerTheoryData))]
    public async Task LaunchSubsystemAutomatically_will_use_subsystemCommunicator_to_launch_subsystems(IEnumerable<KeyValuePair<Guid, SubsystemInfo>> subsystems)
    {
        var loggerMock = CreateLoggerMock();
        var subsystemLauncherCommunicatorMock = new Mock<ISubsystemLauncherCommunicator>();
        var subsystemController = CreateSubsystemController(
            logger: loggerMock.Object,
            dummySubsystemLauncherCommunicator: subsystemLauncherCommunicatorMock.Object);
        var uiDelegateMock = new Mock<Func<Func<IUiHandler, Task>, Task>>();

        subsystemController.SetUiDelegate(uiDelegateMock.Object);

        await subsystemController.InitializeSubsystems(subsystems);

        var subsystem = subsystems.First(x => x.Value.State == SubsystemState.Stopped && !x.Value.AutomatedStart);

        await subsystemController.LaunchSubsystemAutomatically(subsystem.Key);

        subsystem.Value.AutomatedStart.Should().BeTrue();

        subsystemLauncherCommunicatorMock.Verify(x => x.SendLaunchSubsystemsRequest(It.IsAny<Dictionary<Guid, string>>()), Times.Exactly(2));
    }

    [Fact]
    public async Task LaunchSubsystemAutomatically_will_return_without_triggering_anything()
    {
        var loggerMock = CreateLoggerMock();
        var subsystemLauncherCommunicatorMock = new Mock<ISubsystemLauncherCommunicator>();
        var subsystemController = CreateSubsystemController(
            logger: loggerMock.Object,
            dummySubsystemLauncherCommunicator: subsystemLauncherCommunicatorMock.Object);
        var uiDelegateMock = new Mock<Func<Func<IUiHandler, Task>, Task>>();

        subsystemController.SetUiDelegate(uiDelegateMock.Object);
        
        await subsystemController.LaunchSubsystemAutomatically(Guid.Empty);

        subsystemLauncherCommunicatorMock.Verify(x => x.SendLaunchSubsystemsRequest(It.IsAny<Dictionary<Guid, string>>()), Times.Never);
    }

    [Theory]
    [ClassData(typeof(SubsystemControllerTheoryData))]
    public async Task LaunchSubsystemsAutomatically_will_use_subsystemLauncher_to_launch_subsystems(IEnumerable<KeyValuePair<Guid, SubsystemInfo>> subsystems)
    {
        var loggerMock = CreateLoggerMock();
        var subsystemLauncherMock = new Mock<ISubsystemLauncher>();
        var subsystemController = CreateSubsystemController(
            logger: loggerMock.Object,
            dummySubsystemLauncher: subsystemLauncherMock.Object);

        await subsystemController.InitializeSubsystems(subsystems);

        await subsystemController.LaunchSubsystemsAutomatically();

        subsystemLauncherMock.Verify(x => x.LaunchSubsystems(It.IsAny<Dictionary<Guid, string>>()), Times.Exactly(2)); // due the initialization method would actually trigger this method
    }

    [Theory]
    [ClassData(typeof(SubsystemControllerTheoryData))]
    public async Task LaunchSubsystemsAutomatically_will_use_subsystemCommunicator_to_launch_subsystems(IEnumerable<KeyValuePair<Guid, SubsystemInfo>> subsystems)
    {
        var loggerMock = CreateLoggerMock();
        var subsystemLauncherCommunicatorMock = new Mock<ISubsystemLauncherCommunicator>();
        var subsystemController = CreateSubsystemController(
            logger: loggerMock.Object,
            dummySubsystemLauncherCommunicator: subsystemLauncherCommunicatorMock.Object);

        await subsystemController.InitializeSubsystems(subsystems);

        await subsystemController.LaunchSubsystemsAutomatically();

        subsystemLauncherCommunicatorMock.Verify(x => x.SendLaunchSubsystemsRequest(It.IsAny<Dictionary<Guid, string>>()), Times.Exactly(2));
    }

    [Theory]
    [ClassData(typeof(SubsystemControllerTheoryData))]
    public async Task LaunchSubsystem_will_use_subsystemLauncher_to_launch_subsystems(IEnumerable<KeyValuePair<Guid, SubsystemInfo>> subsystems)
    {
        var loggerMock = CreateLoggerMock();
        var subsystemLauncherMock = new Mock<ISubsystemLauncher>();
        var subsystemController = CreateSubsystemController(
            logger: loggerMock.Object,
            dummySubsystemLauncher: subsystemLauncherMock.Object);

        await subsystemController.InitializeSubsystems(subsystems);

        var stoppedSubsystems = subsystems.Where(x => x.Value.State == SubsystemState.Stopped && !x.Value.AutomatedStart);
        var subsystem = stoppedSubsystems.ElementAt(new Random().Next(0, stoppedSubsystems.Count()));

        await subsystemController.LaunchSubsystem(subsystem.Key.ToString());

        subsystemLauncherMock.Verify(x => x.LaunchSubsystem(subsystem.Key, subsystem.Value.Name));
    }

    [Theory]
    [ClassData(typeof(SubsystemControllerTheoryData))]
    public async Task LaunchSubsystem_will_use_subsystemCommunicator_to_launch_subsystems(IEnumerable<KeyValuePair<Guid, SubsystemInfo>> subsystems)
    {
        var loggerMock = CreateLoggerMock();
        var subsystemLauncherCommunicatorMock = new Mock<ISubsystemLauncherCommunicator>();
        var subsystemController = CreateSubsystemController(
            logger: loggerMock.Object,
            dummySubsystemLauncherCommunicator: subsystemLauncherCommunicatorMock.Object);

        await subsystemController.InitializeSubsystems(subsystems);

        var stoppedSubsystems = subsystems.Where(x => x.Value.State == SubsystemState.Stopped && !x.Value.AutomatedStart);
        var subsystem = stoppedSubsystems.ElementAt(new Random().Next(0, stoppedSubsystems.Count()));

        await subsystemController.LaunchSubsystem(subsystem.Key.ToString());

        subsystemLauncherCommunicatorMock.Verify(x => x.SendLaunchSubsystemsRequest(It.IsAny<Dictionary<Guid, string>>()));
    }

    [Theory]
    [ClassData(typeof(SubsystemControllerTheoryData))]
    public async Task LaunchSubsystems_will_use_subsystemLauncher_to_launch_subsystems(IEnumerable<KeyValuePair<Guid, SubsystemInfo>> subsystems)
    {
        var loggerMock = CreateLoggerMock();
        var subsystemLauncherMock = new Mock<ISubsystemLauncher>();
        var subsystemController = CreateSubsystemController(
            logger: loggerMock.Object,
            dummySubsystemLauncher: subsystemLauncherMock.Object);

        await subsystemController.InitializeSubsystems(subsystems);

        var subsToLaunch = subsystems
            .Where(x => x.Value.State == SubsystemState.Stopped && !x.Value.AutomatedStart)
            .Select(x => x.Key.ToString());

        await subsystemController.LaunchSubsystems(subsToLaunch);

        subsystemLauncherMock.Verify(x => x.LaunchSubsystems(It.IsAny<Dictionary<Guid, string>>()), Times.Exactly(2));
    }

    [Theory]
    [ClassData(typeof(SubsystemControllerTheoryData))]
    public async Task LaunchSubsystems_will_use_subsystemCommunicator_to_launch_subsystems(IEnumerable<KeyValuePair<Guid, SubsystemInfo>> subsystems)
    {
        var loggerMock = CreateLoggerMock();
        var subsystemLauncherCommunicatorMock = new Mock<ISubsystemLauncherCommunicator>();
        var subsystemController = CreateSubsystemController(
            logger: loggerMock.Object,
            dummySubsystemLauncherCommunicator: subsystemLauncherCommunicatorMock.Object);

        await subsystemController.InitializeSubsystems(subsystems);

        var subsToLaunch = subsystems
            .Where(x => x.Value.State == SubsystemState.Stopped && !x.Value.AutomatedStart)
            .Select(x => x.Key.ToString());

        await subsystemController.LaunchSubsystems(subsToLaunch);

        subsystemLauncherCommunicatorMock.Verify(x => x.SendLaunchSubsystemsRequest(It.IsAny<Dictionary<Guid, string>>()), Times.Exactly(2));
    }

    [Theory]
    [ClassData(typeof(SubsystemControllerTheoryData))]
    public async Task RestartSubsystem_will_use_subsystemLauncher_to_restart_subsystems(IEnumerable<KeyValuePair<Guid, SubsystemInfo>> subsystems)
    {
        var loggerMock = CreateLoggerMock();
        var subsystemLauncherMock = new Mock<ISubsystemLauncher>();
        var subsystemController = CreateSubsystemController(
            logger: loggerMock.Object,
            dummySubsystemLauncher: subsystemLauncherMock.Object);

        await subsystemController.InitializeSubsystems(subsystems);

        var subsToRestart = subsystems.Where(x => x.Value.State == SubsystemState.Started || x.Value.State == SubsystemState.Running);
        var subsystem = subsToRestart.ElementAt(new Random().Next(0, subsToRestart.Count()));

        await subsystemController.RestartSubsystem(subsystem.Key.ToString());

        subsystemLauncherMock.Verify(x => x.RestartSubsystem(subsystem.Key, subsystem.Value.Name), Times.Once);
    }

    [Theory]
    [ClassData(typeof(SubsystemControllerTheoryData))]
    public async Task RestartSubsystem_will_use_subsystemCommunicator_to_restart_subsystems(IEnumerable<KeyValuePair<Guid, SubsystemInfo>> subsystems)
    {
        var loggerMock = CreateLoggerMock();
        var subsystemLauncherCommunicatorMock = new Mock<ISubsystemLauncherCommunicator>();
        var subsystemController = CreateSubsystemController(
            logger: loggerMock.Object,
            dummySubsystemLauncherCommunicator: subsystemLauncherCommunicatorMock.Object);

        await subsystemController.InitializeSubsystems(subsystems);
        
        var subsToRestart = subsystems.Where(x => x.Value.State == SubsystemState.Started || x.Value.State == SubsystemState.Running);
        var subsystem = subsToRestart.ElementAt(new Random().Next(0, subsToRestart.Count()));

        await subsystemController.RestartSubsystem(subsystem.Key.ToString());

        subsystemLauncherCommunicatorMock.Verify(x => x.SendRestartSubsystemsRequest(It.IsAny<Dictionary<Guid, string>>()), Times.Once);
    }

    [Theory]
    [ClassData(typeof(SubsystemControllerTheoryData))]
    public async Task RestartSubsystems_will_use_subsystemLauncher_to_restart_subsystems(IEnumerable<KeyValuePair<Guid, SubsystemInfo>> subsystems)
    {
        var loggerMock = CreateLoggerMock();
        var subsystemLauncherMock = new Mock<ISubsystemLauncher>();
        var subsystemController = CreateSubsystemController(
            logger: loggerMock.Object,
            dummySubsystemLauncher: subsystemLauncherMock.Object);

        await subsystemController.InitializeSubsystems(subsystems);

        var subsToRestart = subsystems
                .Where(x => x.Value.State == SubsystemState.Started || x.Value.State == SubsystemState.Running)
                .Select(x => x.Key.ToString());

        await subsystemController.RestartSubsystems(subsToRestart);

        subsystemLauncherMock.Verify(x => x.RestartSubsystems(It.IsAny<Dictionary<Guid, string>>()), Times.Once);
    }

    [Theory]
    [ClassData(typeof(SubsystemControllerTheoryData))]
    public async Task RestartSubsystems_will_use_subsystemCommunicator_to_restart_subsystems(IEnumerable<KeyValuePair<Guid, SubsystemInfo>> subsystems)
    {
        var loggerMock = CreateLoggerMock();
        var subsystemLauncherCommunicatorMock = new Mock<ISubsystemLauncherCommunicator>();
        var subsystemController = CreateSubsystemController(
            logger: loggerMock.Object,
            dummySubsystemLauncherCommunicator: subsystemLauncherCommunicatorMock.Object);

        await subsystemController.InitializeSubsystems(subsystems);

        var subsToRestart = subsystems
            .Where(x => x.Value.State == SubsystemState.Started || x.Value.State == SubsystemState.Running)
            .Select(x => x.Key.ToString());

        await subsystemController.RestartSubsystems(subsToRestart);

        subsystemLauncherCommunicatorMock.Verify(x => x.SendRestartSubsystemsRequest(It.IsAny<Dictionary<Guid, string>>()), Times.Once);
    }

    [Theory]
    [ClassData(typeof(SubsystemControllerTheoryData))]
    public async Task ShutdownAllRegisteredSubsystem_will_use_subsystemLauncher_to_shutdown_subsystems(IEnumerable<KeyValuePair<Guid, SubsystemInfo>> subsystems)
    {
        var loggerMock = CreateLoggerMock();
        var subsystemLauncherMock = new Mock<ISubsystemLauncher>();
        var subsystemController = CreateSubsystemController(
            logger: loggerMock.Object,
            dummySubsystemLauncher: subsystemLauncherMock.Object);

        await subsystemController.InitializeSubsystems(subsystems);

        await subsystemController.ShutdownAllRegisteredSubsystem();

        subsystemLauncherMock.Verify(x => x.ShutdownSubsystems(It.IsAny<IEnumerable<KeyValuePair<Guid, string>>>()), Times.Once);
    }

    [Theory]
    [ClassData(typeof(SubsystemControllerTheoryData))]
    public async Task ShutdownAllRegisteredSubsystem_will_use_subsystemCommunicator_to_shutdown_subsystems(IEnumerable<KeyValuePair<Guid, SubsystemInfo>> subsystems)
    {
        var loggerMock = CreateLoggerMock();
        var subsystemLauncherCommunicatorMock = new Mock<ISubsystemLauncherCommunicator>();
        var subsystemController = CreateSubsystemController(
            logger: loggerMock.Object,
            dummySubsystemLauncherCommunicator: subsystemLauncherCommunicatorMock.Object);

        await subsystemController.InitializeSubsystems(subsystems);

        await subsystemController.ShutdownAllRegisteredSubsystem();

        subsystemLauncherCommunicatorMock.Verify(x => x.SendShutdownSubsystemsRequest(It.IsAny<IEnumerable<KeyValuePair<Guid, string>>>()), Times.Once);
    }

    [Theory]
    [ClassData(typeof(SubsystemControllerTheoryData))]
    public async Task ShutdownSubsystem_will_use_subsystemLauncher_to_shutdown_subsystem(IEnumerable<KeyValuePair<Guid, SubsystemInfo>> subsystems)
    {
        var loggerMock = CreateLoggerMock();
        var subsystemLauncherMock = new Mock<ISubsystemLauncher>();
        var subsystemController = CreateSubsystemController(
            logger: loggerMock.Object,
            dummySubsystemLauncher: subsystemLauncherMock.Object);

        await subsystemController.InitializeSubsystems(subsystems);

        var stoppableSubsystems = subsystems.Where(x =>
            x.Value.State == SubsystemState.Running || x.Value.State == SubsystemState.Started);

        var subsystem = stoppableSubsystems.ElementAt(new Random().Next(0, stoppableSubsystems.Count()));
        await subsystemController.ShutdownSubsystem(subsystem.Key.ToString());

        subsystemLauncherMock.Verify(x => x.ShutdownSubsystem(subsystem.Key, subsystem.Value.Name), Times.Once);
    }

    [Theory]
    [ClassData(typeof(SubsystemControllerTheoryData))]
    public async Task ShutdownSubsystem_will_use_subsystemCommunicator_to_shutdown_subsystem(IEnumerable<KeyValuePair<Guid, SubsystemInfo>> subsystems)
    {
        var loggerMock = CreateLoggerMock();
        var subsystemLauncherCommunicatorMock = new Mock<ISubsystemLauncherCommunicator>();
        var subsystemController = CreateSubsystemController(
            logger: loggerMock.Object,
            dummySubsystemLauncherCommunicator: subsystemLauncherCommunicatorMock.Object);

        await subsystemController.InitializeSubsystems(subsystems);

        var stoppableSubsystems = subsystems.Where(x =>
            x.Value.State == SubsystemState.Running || x.Value.State == SubsystemState.Started);

        var subsystem = stoppableSubsystems.ElementAt(new Random().Next(0, stoppableSubsystems.Count()));
        await subsystemController.ShutdownSubsystem(subsystem.Key.ToString());

        subsystemLauncherCommunicatorMock.Verify(x => x.SendShutdownSubsystemsRequest(It.IsAny<Dictionary<Guid, string>>()), Times.Once);
    }

    [Fact]
    public async Task ShutdownSubsystem_will_do_nothing()
    {
        var loggerMock = CreateLoggerMock();
        var subsystemLauncherCommunicatorMock = new Mock<ISubsystemLauncherCommunicator>();
        var subsystemController = CreateSubsystemController(
            logger: loggerMock.Object,
            dummySubsystemLauncherCommunicator: subsystemLauncherCommunicatorMock.Object);

        await subsystemController.InitializeSubsystems(new List<KeyValuePair<Guid, SubsystemInfo>>());

        await subsystemController.ShutdownSubsystem(Guid.NewGuid().ToString());

        subsystemLauncherCommunicatorMock.Verify(x => x.SendShutdownSubsystemsRequest(It.IsAny<Dictionary<Guid, string>>()), Times.Never);
    }

    [Theory]
    [ClassData(typeof(SubsystemControllerTheoryData))]
    public async Task ShutdownSubsystems_will_use_subsystemLauncher_to_shutdown_subsystems(IEnumerable<KeyValuePair<Guid, SubsystemInfo>> subsystems)
    {
        var loggerMock = CreateLoggerMock();
        var subsystemLauncherMock = new Mock<ISubsystemLauncher>();
        var subsystemController = CreateSubsystemController(
            logger: loggerMock.Object,
            dummySubsystemLauncher: subsystemLauncherMock.Object);

        await subsystemController.InitializeSubsystems(subsystems);

        var stoppableSubsystems = subsystems
            .Where(x => x.Value.State == SubsystemState.Running || x.Value.State == SubsystemState.Started && x.Value.AutomatedStart);

        await subsystemController.ShutdownSubsystems(stoppableSubsystems.Select(x => x.Key.ToString()));

        subsystemLauncherMock.Verify(x => x.ShutdownSubsystems(It.IsAny<IEnumerable<KeyValuePair<Guid, string>>>()), Times.Once);
    }

    [Theory]
    [ClassData(typeof(SubsystemControllerTheoryData))]
    public async Task ShutdownSubsystems_will_use_subsystemCommunicator_to_launch_subsystems(IEnumerable<KeyValuePair<Guid, SubsystemInfo>> subsystems)
    {
        var loggerMock = CreateLoggerMock();
        var subsystemLauncherCommunicatorMock = new Mock<ISubsystemLauncherCommunicator>();
        var subsystemController = CreateSubsystemController(
            logger: loggerMock.Object,
            dummySubsystemLauncherCommunicator: subsystemLauncherCommunicatorMock.Object);

        await subsystemController.InitializeSubsystems(subsystems);

        var stoppableSubsystems = subsystems
            .Where(x => x.Value.State == SubsystemState.Running || x.Value.State == SubsystemState.Started && x.Value.AutomatedStart);

        await subsystemController.ShutdownSubsystems(stoppableSubsystems.Select(x => x.Key.ToString()));

        subsystemLauncherCommunicatorMock.Verify(x => x.SendShutdownSubsystemsRequest(It.IsAny<IEnumerable<KeyValuePair<Guid, string>>>()), Times.Once);
    }

    [Fact]
    public async Task ShutdownSubsystems_will_return_without_triggering_anything()
    {
        var loggerMock = CreateLoggerMock();
        var subsystemLauncherCommunicatorMock = new Mock<ISubsystemLauncherCommunicator>();
        var subsystemController = CreateSubsystemController(
            logger: loggerMock.Object,
            dummySubsystemLauncherCommunicator: subsystemLauncherCommunicatorMock.Object);

        await subsystemController.InitializeSubsystems(new List<KeyValuePair<Guid, SubsystemInfo>>());

        await subsystemController.ShutdownSubsystems(new List<string>());

        subsystemLauncherCommunicatorMock.Verify(x => x.SendShutdownSubsystemsRequest(Enumerable.Empty<KeyValuePair<Guid, string>>()), Times.Once);
    }

    [Fact]
    public async Task ShutdownSubsystems_will_log_exception()
    {
        var loggerMock = CreateLoggerMock();
        var subsystemLauncherCommunicatorMock = new Mock<ISubsystemLauncherCommunicator>();
        var subsystemController = CreateSubsystemController(
            logger: loggerMock.Object,
            dummySubsystemLauncherCommunicator: subsystemLauncherCommunicatorMock.Object);

        await subsystemController.InitializeSubsystems(new List<KeyValuePair<Guid, SubsystemInfo>>());

        await subsystemController.ShutdownSubsystems(null);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Cannot terminate a subsystem/subsystems.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [ClassData(typeof(SubsystemControllerTheoryData))]
    public async Task ModifySubsystemState_will_modify_the_state_and_trigger_UI_update(IEnumerable<KeyValuePair<Guid, SubsystemInfo>> subsystems)
    {
        var loggerMock = CreateLoggerMock();
        var subsystemLauncherMock = new Mock<ISubsystemLauncher>();
        var subsystemController = CreateSubsystemController(
            logger: loggerMock.Object,
            dummySubsystemLauncher: subsystemLauncherMock.Object);
        var uiDelegateMock = new Mock<Func<Func<IUiHandler, Task>, Task>>();

        subsystemController.SetUiDelegate(uiDelegateMock.Object);

        await subsystemController.InitializeSubsystems(subsystems);

        var stoppableSubsystems = subsystems.Where(x => x.Value.State == SubsystemState.Started);
        var subsystem = stoppableSubsystems.ElementAt(new Random().Next(0, stoppableSubsystems.Count()));

        await subsystemController.ModifySubsystemState(subsystem.Key, "DummyState");

        subsystem.Value.State.Should().Be("DummyState");
        uiDelegateMock.Verify(x => x.Invoke(It.IsAny<Func<IUiHandler, Task>>()), Times.Exactly(2)); // due IUiHandler function will be called twice as per after initialization of the subsystems we are pushing data to the uis.
    }

    [Fact]
    public async Task ModifySubsystemState_will_return_and_wont_trigger_update()
    {
        var loggerMock = CreateLoggerMock();
        var subsystemLauncherMock = new Mock<ISubsystemLauncher>();
        var subsystemController = CreateSubsystemController(
            logger: loggerMock.Object,
            dummySubsystemLauncher: subsystemLauncherMock.Object);
        var uiDelegateMock = new Mock<Func<Func<IUiHandler, Task>, Task>>();

        subsystemController.SetUiDelegate(uiDelegateMock.Object);

        await subsystemController.InitializeSubsystems(null);
        await subsystemController.ModifySubsystemState(Guid.NewGuid(), "DummyState");
        uiDelegateMock.Verify(x => x.Invoke(It.IsAny<Func<IUiHandler, Task>>()), Times.Never);
    }

    [Theory]
    [ClassData(typeof(SubsystemControllerTheoryData))]
    public async Task AddSubsystems_will_add_elements_and_update_to_the_ui(IEnumerable<KeyValuePair<Guid, SubsystemInfo>> subsystems)
    {
        var loggerMock = CreateLoggerMock();
        var subsystemLauncherMock = new Mock<ISubsystemLauncher>();
        var subsystemController = CreateSubsystemController(
            logger: loggerMock.Object,
            dummySubsystemLauncher: subsystemLauncherMock.Object);
        var uiDelegateMock = new Mock<Func<Func<IUiHandler, Task>, Task>>();

        subsystemController.SetUiDelegate(uiDelegateMock.Object);

        await subsystemController.InitializeSubsystems(subsystems);

        var newElements = new List<KeyValuePair<Guid, SubsystemInfo>>();
        newElements.Add(new KeyValuePair<Guid, SubsystemInfo>(
            Guid.NewGuid(), new SubsystemInfo()
            {
                State = SubsystemState.Stopped,
                Name = "DummySubsystemNew"
            }));

        await subsystemController.AddSubsystems(newElements);

        var result = subsystemController.GetSubsystems();
        result.Should().Contain(subsystems);
        result.Should().Contain(newElements);

        uiDelegateMock.Verify(x => x.Invoke(It.IsAny<Func<IUiHandler, Task>>()), Times.Exactly(2));
    }

    [Theory]
    [ClassData(typeof(SubsystemControllerTheoryData))]
    public async Task RemoveSubsystem_will_remove_element_without_triggering_shutdown(IEnumerable<KeyValuePair<Guid, SubsystemInfo>> subsystems)
    {
        var loggerMock = CreateLoggerMock();
        var subsystemLauncherMock = new Mock<ISubsystemLauncher>();
        var subsystemController = CreateSubsystemController(
            logger: loggerMock.Object,
            dummySubsystemLauncher: subsystemLauncherMock.Object);
        var uiDelegateMock = new Mock<Func<Func<IUiHandler, Task>, Task>>();

        subsystemController.SetUiDelegate(uiDelegateMock.Object);

        await subsystemController.InitializeSubsystems(subsystems);

        var subsystemtoDelete =
            subsystems.First(x => x.Value.State == SubsystemState.Stopped && !x.Value.AutomatedStart);

        subsystemController.RemoveSubsystem(subsystemtoDelete.Key);

        var result = subsystemController.GetSubsystems();

        result.Should().NotContain(subsystemtoDelete);

        subsystemLauncherMock.Verify(x => x.ShutdownSubsystem(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [ClassData(typeof(SubsystemControllerTheoryData))]
    public async Task RemoveSubsystem_will_remove_element_wit_triggering_shutdown(IEnumerable<KeyValuePair<Guid, SubsystemInfo>> subsystems)
    {
        var loggerMock = CreateLoggerMock();
        var subsystemLauncherMock = new Mock<ISubsystemLauncher>();
        var subsystemController = CreateSubsystemController(
            logger: loggerMock.Object,
            dummySubsystemLauncher: subsystemLauncherMock.Object);
        var uiDelegateMock = new Mock<Func<Func<IUiHandler, Task>, Task>>();

        subsystemController.SetUiDelegate(uiDelegateMock.Object);

        await subsystemController.InitializeSubsystems(subsystems);

        var subsystemtoDelete =
            subsystems.First(x => x.Value.State == SubsystemState.Started);

        subsystemController.RemoveSubsystem(subsystemtoDelete.Key);

        var result = subsystemController.GetSubsystems();

        result.Should().NotContain(subsystemtoDelete);

        subsystemLauncherMock.Verify(x => x.ShutdownSubsystem(It.IsAny<Guid>(), It.IsAny<string>()), Times.Once);
    }

    private Mock<ILogger> CreateLoggerMock()
    {
        var loggerMock = new Mock<ILogger>();

        var loggerFilterOptions = new LoggerFilterOptions();

        loggerFilterOptions.AddFilter("", LogLevel.Debug);

        loggerMock
            .Setup(x => x.IsEnabled(It.IsAny<LogLevel>()))
            .Returns<LogLevel>(level => loggerFilterOptions.MinLevel <= level);

        return loggerMock;
    }

    private ISubsystemController CreateSubsystemController(
        ILogger logger,
        ISubsystemLauncher? dummySubsystemLauncher = null,
        ISubsystemLauncherCommunicator? dummySubsystemLauncherCommunicator = null)
    {
        ISubsystemController subsystemController = null;
        if (dummySubsystemLauncher != null)
            subsystemController = new SubsystemController(
                subsystemLauncher: dummySubsystemLauncher,
                logger: logger);
        else if (dummySubsystemLauncherCommunicator != null)
            subsystemController = new SubsystemController(
                subsystemLauncherCommunicator: dummySubsystemLauncherCommunicator,
                logger: logger);

        return subsystemController;
    }

    private class SubsystemControllerTheoryData : TheoryData
    {
        private readonly Dictionary<Guid, SubsystemInfo> _subsystems = new()
        {
            { Guid.NewGuid(), new(){ State = SubsystemState.Started, Name = "DummySubsystem1" }},
            { Guid.NewGuid(), new(){ State = SubsystemState.Started, Name = "DummySubsystem2" }},
            { Guid.NewGuid(), new(){ State = SubsystemState.Running, Name = "DummySubsystem3" }},
            { Guid.NewGuid(), new(){ State = SubsystemState.Running, Name = "DummySubsystem4" }},
            { Guid.NewGuid(), new(){ State = SubsystemState.Stopped, Name = "DummySubsystem5" }},
            { Guid.NewGuid(), new(){ State = SubsystemState.Stopped, Name = "DummySubsystem6" , AutomatedStart = true }},
            { Guid.NewGuid(), new(){ State = SubsystemState.Stopped, Name = "DummySubsystem7" }},
            { Guid.NewGuid(), new(){ State = SubsystemState.Stopped, Name = "DummySubsystem8" }},
            { Guid.NewGuid(), new(){ State = SubsystemState.Stopped, Name = "DummySubsystem9" }},
            { Guid.NewGuid(), new(){ State = SubsystemState.Stopped, Name = "DummySubsystem10" }},
        };

        public SubsystemControllerTheoryData()
        {
            AddRow(_subsystems);
        }
    }
}
