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
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProcessExplorer.Abstractions.Subsystems;
using ProcessExplorer.Core.DependencyInjection;
using ProcessExplorer.Core.Subsystems;
using Xunit;

namespace ProcessExplorer.Core.Tests.Subsystems;

public class SubsystemLauncherTests
{
    [Theory]
    [ClassData(typeof(SubsystemLauncherTheoryData))]
    public async Task LaunchSubsystem_will_invoke_dummy_launch_request(TestInformation testInformation)
    {
        // Arrange
        var launchRequestMock = new Mock<Action<DummyStartType>>();
        var createLaunchRequestMock = new Mock<Func<Guid, string, DummyStartType>>();
        var loggerMock = CreateLoggerMock();

        var subsystemLauncher = CreateSubsystemLauncher(
            logger: loggerMock.Object,
            createDummyStartType: createLaunchRequestMock.Object,
            dummyStartRequest: launchRequestMock.Object);

        // Act
        var result = await subsystemLauncher.LaunchSubsystem(testInformation.LaunchRequestId, testInformation.LaunchRequestSubsystemName);

        // Assert
        result.Should().Be(testInformation.ExpectedLaunchStateResult);

        var element = testInformation.Subsystems.FirstOrDefault(x => x.Key == testInformation.LaunchRequestId);

        if (element.Value.State != SubsystemState.Started && element.Value.State != SubsystemState.Running)
            launchRequestMock.Verify(x => x.Invoke(It.IsAny<DummyStartType>()), Times.Once);

        if(testInformation.ExpectedLogMessage != string.Empty)
            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(testInformation.LaunchRequestId.ToString()) && v.ToString()!.Contains(testInformation.ExpectedLogMessage)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
    }

    [Theory]
    [ClassData(typeof(SubsystemLauncherTheoryData))]
    public async Task LauchSubsystem_will_return_stopped_when_no_launchType_added(TestInformation testInformation)
    {
        //Arrange
        var loggerMock = CreateLoggerMock();

        var subsystemLauncher = CreateSubsystemLauncher(
            logger: loggerMock.Object);

        // Act
        var result = await subsystemLauncher.LaunchSubsystem(testInformation.LaunchRequestId, testInformation.LaunchRequestSubsystemName);

        //Assert
        result.Should().Be(SubsystemState.Stopped); //That is the default value
    }

    [Theory]
    [ClassData(typeof(SubsystemLauncherTheoryData))]
    public async Task LaunchSubsystemAfterTime_will_wait_before_launching_a_subsystem(TestInformation testInformation)
    {
        // Arrange
        var loggerMock = CreateLoggerMock();
        var periodOfTime = 150;

        var subsystemLauncher = CreateSubsystemLauncher(
            logger: loggerMock.Object);

        // Act
        var stopwatch = Stopwatch.StartNew();
        await subsystemLauncher.LaunchSubsystemAfterTime(testInformation.LaunchRequestId, testInformation.LaunchRequestSubsystemName, periodOfTime);
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(periodOfTime);
    }

    [Theory]
    [ClassData(typeof(SubsystemLauncherTheoryData))]
    public async Task LaunchSubsystems_will_launch_subsystems(TestInformation testInformation)
    {
        // Arrange
        var launchRequestMock = new Mock<Action<DummyStartType>>();
        var createLaunchRequestMock = new Mock<Func<Guid, string, DummyStartType>>();
        var loggerMock = CreateLoggerMock();

        var subsystemLauncher = CreateSubsystemLauncher(
            logger: loggerMock.Object,
            createDummyStartType: createLaunchRequestMock.Object,
            dummyStartRequest: launchRequestMock.Object);

        // Act
        var result = await subsystemLauncher.LaunchSubsystems(testInformation.LaunchRequestIds);

        // Assert
        result.Should().BeEquivalentTo(testInformation.ExpectedLaunchStateResults);

        launchRequestMock.Verify(x => x.Invoke(It.IsAny<DummyStartType>()), Times.Exactly(testInformation.ExpectedLaunchStateResults.Count()));
    }

    [Theory]
    [ClassData(typeof(SubsystemLauncherTheoryData))]
    public async Task ShutdownSubsytem_will_trigger_the_shutdown_command(TestInformation testInformation)
    {
        // Arrange
        var stopRequestMock = new Mock<Action<DummyStopType>>();
        var createStopRequest = new Mock<Func<Guid, DummyStopType>>();
        var loggerMock = CreateLoggerMock();

        var subsystemLauncher = CreateSubsystemLauncher(
            logger: loggerMock.Object,
            createDummyStopType: createStopRequest.Object,
            dummyStopRequest: stopRequestMock.Object);

        // Act
        var result = await subsystemLauncher.ShutdownSubsystem(testInformation.StopRequestId, testInformation.StopRequestSubsystemName);

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Stopping a subsystem with id:") && v.ToString()!.Contains(testInformation.StopRequestId.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        result.Should().Be(SubsystemState.Stopped);
    }

    [Theory]
    [ClassData(typeof(SubsystemLauncherTheoryData))]
    public async Task ShutdownSubsytem_will_return_running_state_if_no_stop_request_behavior_is_added(TestInformation testInformation)
    {
        // Arrange
        var loggerMock = CreateLoggerMock();

        var subsystemLauncher = CreateSubsystemLauncher(
            logger: loggerMock.Object);

        // Act
        var result = await subsystemLauncher.ShutdownSubsystem(testInformation.StopRequestId, testInformation.StopRequestSubsystemName);

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Stopping a subsystem with id:") && v.ToString()!.Contains(testInformation.StopRequestId.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        result.Should().Be(SubsystemState.Running);
    }

    [Theory]
    [ClassData(typeof(SubsystemLauncherTheoryData))]
    public async Task StopSubsystems_will_trigger_stop_request(TestInformation testInformation)
    {
        // Arrange
        var stopRequestMock = new Mock<Action<DummyStopType>>();
        var createStopRequest = new Mock<Func<Guid, DummyStopType>>();
        var loggerMock = CreateLoggerMock();

        var subsystemLauncher = CreateSubsystemLauncher(
            logger: loggerMock.Object,
            createDummyStopType: createStopRequest.Object,
            dummyStopRequest: stopRequestMock.Object);

        // Act
        var result = await subsystemLauncher.ShutdownSubsystems(testInformation.StopRequestIds);

        // Assert
        result.Should().BeEquivalentTo(testInformation.ExpectedStopStateResults);

        stopRequestMock.Verify(x => x.Invoke(It.IsAny<DummyStopType>()), Times.Exactly(testInformation.ExpectedStopStateResults.Count()));
    }

    [Theory]
    [ClassData(typeof(SubsystemLauncherTheoryData))]
    public async Task RestartSubsystem_will_return_started_state_and_trigger_launch_and_stop_request(TestInformation testInformation)
    {
        // Arrange
        var launchRequestMock = new Mock<Action<DummyStartType>>();
        var createLaunchRequestMock = new Mock<Func<Guid, string, DummyStartType>>();
        var stopRequestMock = new Mock<Action<DummyStopType>>();
        var createStopRequest = new Mock<Func<Guid, DummyStopType>>();
        var loggerMock = CreateLoggerMock();

        var subsystemLauncher = CreateSubsystemLauncher(
            logger: loggerMock.Object,
            createDummyStartType: createLaunchRequestMock.Object,
            dummyStartRequest: launchRequestMock.Object,
            createDummyStopType: createStopRequest.Object,
            dummyStopRequest: stopRequestMock.Object);

        // Act
        var result = await subsystemLauncher.RestartSubsystem(testInformation.StopRequestId, testInformation.StopRequestSubsystemName);

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Stopping a subsystem with id:") && v.ToString()!.Contains(testInformation.StopRequestId.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        
        stopRequestMock.Verify(x => x.Invoke(It.IsAny<DummyStopType>()), Times.Once);

        launchRequestMock.Verify(x => x.Invoke(It.IsAny<DummyStartType>()), Times.Once);

        result.Should().Be(SubsystemState.Started);
    }

    [Theory]
    [ClassData(typeof(SubsystemLauncherTheoryData))]
    public async Task RestartSubsystem_will_log_subsystem_stop_debugerror_if_no_stop_request_is_defined(TestInformation testInformation)
    {
        // Arrange
        var launchRequestMock = new Mock<Action<DummyStartType>>();
        var createLaunchRequestMock = new Mock<Func<Guid, string, DummyStartType>>();

        var loggerMock = CreateLoggerMock();

        var subsystemLauncher = CreateSubsystemLauncher(
            logger: loggerMock.Object,
            createDummyStartType: createLaunchRequestMock.Object,
            dummyStartRequest: launchRequestMock.Object);

        // Act
        var result = await subsystemLauncher.RestartSubsystem(testInformation.StopRequestId, testInformation.StopRequestSubsystemName);

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Stopping a subsystem with id:") && v.ToString()!.Contains(testInformation.StopRequestId.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error while restarting a subsystem with id:") && v.ToString()!.Contains(testInformation.StopRequestId.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        launchRequestMock.Verify(x => x.Invoke(It.IsAny<DummyStartType>()), Times.Never);

        result.Should().Be(SubsystemState.Running);
    }

    [Theory]
    [ClassData(typeof(SubsystemLauncherTheoryData))]
    public async Task RestartSubsytem_will_log_subsystem_restart_error_if_no_launch_request_is_defined(TestInformation testInformation)
    {
        // Arrange
        var stopRequestMock = new Mock<Action<DummyStopType>>();
        var createStopRequest = new Mock<Func<Guid, DummyStopType>>();
        var loggerMock = CreateLoggerMock();

        var subsystemLauncher = CreateSubsystemLauncher(
            logger: loggerMock.Object,
            createDummyStopType: createStopRequest.Object,
            dummyStopRequest: stopRequestMock.Object);

        // Act
        var result = await subsystemLauncher.RestartSubsystem(testInformation.StopRequestId, testInformation.StopRequestSubsystemName);

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Stopping a subsystem with id:") && v.ToString()!.Contains(testInformation.StopRequestId.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error while restarting a subsystem with id:") && v.ToString()!.Contains(testInformation.StopRequestId.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        stopRequestMock.Verify(x => x.Invoke(It.IsAny<DummyStopType>()), Times.Once);

        result.Should().Be(SubsystemState.Stopped);
    }

    [Theory]
    [ClassData(typeof(SubsystemLauncherTheoryData))]
    public async Task RestartSubsystems_will_return_started_state_and_trigger_launch_and_stop_request(TestInformation testInformation)
    {
        // Arrange
        var launchRequestMock = new Mock<Action<DummyStartType>>();
        var createLaunchRequestMock = new Mock<Func<Guid, string, DummyStartType>>();
        var stopRequestMock = new Mock<Action<DummyStopType>>();
        var createStopRequest = new Mock<Func<Guid, DummyStopType>>();
        var loggerMock = CreateLoggerMock();

        var subsystemLauncher = CreateSubsystemLauncher(
            logger: loggerMock.Object,
            createDummyStartType: createLaunchRequestMock.Object,
            dummyStartRequest: launchRequestMock.Object,
            createDummyStopType: createStopRequest.Object,
            dummyStopRequest: stopRequestMock.Object);

        // Act
        var result = await subsystemLauncher.RestartSubsystems(testInformation.StopRequestIds);

        // Assert
        stopRequestMock.Verify(x => x.Invoke(It.IsAny<DummyStopType>()), Times.Exactly(testInformation.ExpectedStopStateResults.Count()));

        launchRequestMock.Verify(x => x.Invoke(It.IsAny<DummyStartType>()), Times.Exactly(testInformation.ExpectedStopStateResults.Count()));
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

    private ISubsystemLauncher CreateSubsystemLauncher(
        ILogger logger,
        Action<DummyStartType>? dummyStartRequest = null,
        Action<DummyStopType>? dummyStopRequest = null,
        Func<Guid, string, DummyStartType>? createDummyStartType = null, 
        Func<Guid, DummyStopType>? createDummyStopType = null)
    {
        var subsystemLauncher = new SubsystemLauncher<DummyStartType, DummyStopType>(
            logger,
            new SubsystemLauncherOptions<DummyStartType, DummyStopType>()
            {
                CreateLaunchRequest = createDummyStartType,
                CreateStopRequest = createDummyStopType,
                LaunchRequest = dummyStartRequest,
                StopRequest = dummyStopRequest,
            });

        return subsystemLauncher;
    }

    private class SubsystemLauncherTheoryData : TheoryData
    {
        private readonly KeyValuePair<Guid, SubsystemInfo>[] _subsystems = 
        {
            new(Guid.NewGuid(), new SubsystemInfo { State = SubsystemState.Started, Name = "TestSubsystem1" }),
            new(Guid.NewGuid(), new SubsystemInfo { State = SubsystemState.Stopped, Name = "TestSubsystem2" }),
            new(Guid.NewGuid(), new SubsystemInfo { State = SubsystemState.Stopped, Name = "TestSubsystem3" }),
            new(Guid.NewGuid(), new SubsystemInfo { State = SubsystemState.Running, Name = "TestSubsystem3" }),
            new(Guid.NewGuid(), new SubsystemInfo { State = SubsystemState.Stopped, Name = "TestSubsystem5" }),
            new(Guid.NewGuid(), new SubsystemInfo { State = SubsystemState.Stopped, Name = "TestSubsystem6" }),
            new(Guid.NewGuid(), new SubsystemInfo { State = SubsystemState.Started, Name = "TestSubsystem7" }),
            new(Guid.NewGuid(), new SubsystemInfo { State = SubsystemState.Started, Name = "TestSubsystem8" }),
            new(Guid.NewGuid(), new SubsystemInfo { State = SubsystemState.Started, Name = "TestSubsystem9" }),
        };

        public SubsystemLauncherTheoryData()
        {
            AddRow(CreateTestInformation());
        }

        //Generating random data and its preferably expected value
        private TestInformation CreateTestInformation()
        {
            var stopppableSubsystems = 
                _subsystems
                    .Where(subsystem => subsystem.Value.State != SubsystemState.Stopped);

            var launchableSubsystems = 
                _subsystems
                    .Where(subsystem => subsystem.Value.State == SubsystemState.Stopped);

            var launchedSubsystemsCount = stopppableSubsystems.Count();
            var stoppedSubsystemsCount = _subsystems.Length - launchedSubsystemsCount;

            var randomLaunchableSubsystem = GetRandomSubsystem(launchableSubsystems);
            var randomStoppableSubsystem = GetRandomSubsystem(stopppableSubsystems);

            //Because the SubsystemLauncher does not search for the expectedState its just executing the gotten command from the UI.
            var expectedLaunchState = 
                randomLaunchableSubsystem.Value.State == SubsystemState.Stopped || randomLaunchableSubsystem.Value.State == SubsystemState.Running 
                    ? SubsystemState.Started 
                    : randomLaunchableSubsystem.Value.State;

            var expectedLaunchLogMessage = randomLaunchableSubsystem.Value.State == SubsystemState.Stopped 
                ? "Starting" 
                : "";

            var expectedStopState =
                randomLaunchableSubsystem.Value.State == SubsystemState.Started || randomLaunchableSubsystem.Value.State == SubsystemState.Running
                    ? SubsystemState.Stopped
                    : randomLaunchableSubsystem.Value.State;

            var launchRequestIds = new Dictionary<Guid, string>();
            var expectedLaunchStateResults = new Dictionary<Guid, string>();

            // Fill LaunchRequestIds and ExpectedLaunchStateResults when LaunchSubsystems will be tested
            if (_subsystems.Length > launchedSubsystemsCount)
            {
                var random = new Random().Next(1, stoppedSubsystemsCount);
                for (int i = 0; i < random; i++)
                {
                    var subsystem = GetRandomSubsystemAndItsExpectedResult(launchableSubsystems, SubsystemState.Started);
                    if (launchRequestIds.ContainsKey(subsystem.Key)) continue;
                    launchRequestIds.Add(subsystem.Key, subsystem.Value.Key);
                    expectedLaunchStateResults.Add(subsystem.Key, subsystem.Value.Value);
                }
            }

            var stopRequestIds = new Dictionary<Guid, string>();
            var expectedStopStateResults = new Dictionary<Guid, string>();

            // Checking if there is subsystems which can be stopped
            if (_subsystems.Length > stoppedSubsystemsCount)
            {
                var random = new Random().Next(1, launchedSubsystemsCount);
                for (int i = 0; i < random; i++)
                {
                    var subsystem = GetRandomSubsystemAndItsExpectedResult(stopppableSubsystems, SubsystemState.Stopped);
                    if (stopRequestIds.ContainsKey(subsystem.Key)) continue;
                    stopRequestIds.Add(subsystem.Key, subsystem.Value.Key);
                    expectedStopStateResults.Add(subsystem.Key, subsystem.Value.Value);
                }
            }

            var info = new TestInformation
            {
                Subsystems = _subsystems,
                LaunchRequestSubsystemName = randomLaunchableSubsystem.Value.Name,
                LaunchRequestId = randomLaunchableSubsystem.Key,
                StopRequestId = randomStoppableSubsystem.Key,
                StopRequestSubsystemName = randomStoppableSubsystem.Value.Name,
                ExpectedLaunchStateResult = expectedLaunchState,
                ExpectedStopStateResult = expectedStopState,
                ExpectedLogMessage = expectedLaunchLogMessage,
                LaunchRequestIds = launchRequestIds,
                StopRequestIds = stopRequestIds,
                ExpectedLaunchStateResults = expectedLaunchStateResults,
                ExpectedStopStateResults = expectedStopStateResults
            };

            return info;
        }

        private static KeyValuePair<Guid, SubsystemInfo> GetRandomSubsystem(IEnumerable<KeyValuePair<Guid, SubsystemInfo>> subsystems)
        {
            var random = new Random().Next(0, subsystems.Count());
            return subsystems.ElementAt(random);
        }

        private KeyValuePair<Guid, KeyValuePair<string, string>> GetRandomSubsystemAndItsExpectedResult(IEnumerable<KeyValuePair<Guid, SubsystemInfo>> subsystems, string expectedStateResult)
        {
            var subsystem = GetRandomSubsystem(subsystems);
            return  new(subsystem.Key, new(subsystem.Value.Name, expectedStateResult));
        }
    }

    public class TestInformation
    {
        public Guid LaunchRequestId { get; set; }
        public Guid StopRequestId { get; set; }
        public string LaunchRequestSubsystemName { get; set; }
        public string StopRequestSubsystemName { get; set; }
        public IEnumerable<KeyValuePair<Guid, string>> LaunchRequestIds { get; set; } = Enumerable.Empty<KeyValuePair<Guid, string>>();
        public IEnumerable<KeyValuePair<Guid, string>> StopRequestIds { get; set; } = Enumerable.Empty<KeyValuePair<Guid, string>>();
        public KeyValuePair<Guid, SubsystemInfo>[] Subsystems { get; set; }
        public string ExpectedLaunchStateResult { get; set; }
        public string ExpectedStopStateResult { get; set; }
        public IEnumerable<KeyValuePair<Guid, string>> ExpectedLaunchStateResults { get; set; } = Enumerable.Empty<KeyValuePair<Guid, string>>();
        public IEnumerable<KeyValuePair<Guid, string>> ExpectedStopStateResults { get; set; } = Enumerable.Empty<KeyValuePair<Guid, string>>();
        public string ExpectedLogMessage { get; set; }
    }
    
    public record DummyStartType(Guid id, string name);
    public record DummyStopType(Guid id);
}
