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
            createDummyStartType: launchRequestMock.Object,
            dummyStartRequest: createLaunchRequestMock.Object);

        // Act
        var result = await subsystemLauncher.LaunchSubsystem(testInformation.RequestId, testInformation.RequestSubsystemName);

        // Assert
        result.Should().Be(testInformation.ExpectedStateResult);

        var element = testInformation.Subsystems.FirstOrDefault(x => x.Key == testInformation.RequestId);

        if (element.Value != null && element.Value.State != SubsystemState.Started && element.Value.State != SubsystemState.Running)
            launchRequestMock.Verify(x => x.Invoke(It.IsAny<DummyStartType>()), Times.Once);

        if(testInformation.ExpectedLogMessage != string.Empty)
            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(testInformation.RequestId.ToString()) && v.ToString().Contains(testInformation.ExpectedLogMessage)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
    }

    [Theory]
    [ClassData(typeof(SubsystemLauncherTheoryData))]
    public async Task LauchSubsystem_will_return_stopped_when_no_launchType_added(TestInformation testInformation)
    {
        var loggerMock = CreateLoggerMock();

        var subsystemLauncher = CreateSubsystemLauncher(
            logger: loggerMock.Object);

        // Act
        var result = await subsystemLauncher.LaunchSubsystem(testInformation.RequestId, testInformation.RequestSubsystemName);

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
        await subsystemLauncher.LaunchSubsystemAfterTime(testInformation.RequestId, testInformation.RequestSubsystemName, periodOfTime);
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
            createDummyStartType: launchRequestMock.Object,
            dummyStartRequest: createLaunchRequestMock.Object);

        // Act
        var result = await subsystemLauncher.LaunchSubsystems(testInformation.LaunchRequestIds);

        // Assert
        result.Should().BeEquivalentTo(testInformation.ExpectedStateResults);

        launchRequestMock.Verify(x => x.Invoke(It.IsAny<DummyStartType>()), Times.Exactly(testInformation.ExpectedStateResults.Count()));
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
        Action<DummyStartType>? createDummyStartType = null,
        Action<DummyStopType>? createDummyStopType = null,
        Func<Guid, string, DummyStartType>? dummyStartRequest = null,
        Func<Guid, DummyStopType>? dummyStopRequest = null)
    {
        var subsystemLauncher = new SubsystemLauncher<DummyStartType, DummyStopType>(
            logger,
            new SubsystemLauncherOptions<DummyStartType, DummyStopType>()
            {
                CreateLaunchRequest = dummyStartRequest,
                CreateStopRequest = dummyStopRequest,
                LaunchRequest = createDummyStartType,
                StopRequest = createDummyStopType,
            });

        return subsystemLauncher;
    }

    private class SubsystemLauncherTheoryData : TheoryData
    {
        private readonly KeyValuePair<Guid, SubsystemInfo>[] _subsystems = new KeyValuePair<Guid, SubsystemInfo>[] {
            new KeyValuePair<Guid, SubsystemInfo>(Guid.NewGuid(), new SubsystemInfo { State = SubsystemState.Started, Name = "TestSubsystem1" }),
            new KeyValuePair<Guid, SubsystemInfo>(Guid.NewGuid(), new SubsystemInfo { State = SubsystemState.Stopped, Name = "TestSubsystem2" }),
            new KeyValuePair<Guid, SubsystemInfo>(Guid.NewGuid(), new SubsystemInfo { State = SubsystemState.Stopped, Name = "TestSubsystem3" }),
            new KeyValuePair<Guid, SubsystemInfo>(Guid.NewGuid(), new SubsystemInfo { State = SubsystemState.Running, Name = "TestSubsystem4" }),
            new KeyValuePair<Guid, SubsystemInfo>(Guid.NewGuid(), new SubsystemInfo { State = SubsystemState.Stopped, Name = "TestSubsystem5" }),
            new KeyValuePair<Guid, SubsystemInfo>(Guid.NewGuid(), new SubsystemInfo { State = SubsystemState.Stopped, Name = "TestSubsystem6" })
        };

        public SubsystemLauncherTheoryData()
        {
            AddRow(CreateTestInformation());
        }

        private TestInformation CreateTestInformation()
        {
            var randomElement = GetRandomSubsystem();
            var expectedState = randomElement.Value.State == SubsystemState.Stopped ? SubsystemState.Started : randomElement.Value.State;
            var expectedLogMessage = randomElement.Value.State == SubsystemState.Stopped ? "Starting" : "";

            var random = new Random().Next(1, _subsystems.Length);
            var requestIds = new Dictionary<Guid, string>();
            var expectedStateResults = new Dictionary<Guid, string>();

            for (int i = 0; i < random; i++)
            {
                var subsystem = GetRandomSubsystemAndItsExpectedResult();
                if (requestIds.ContainsKey(subsystem.Key)) continue;
                requestIds.Add(subsystem.Key, subsystem.Value.Key);
                expectedStateResults.Add(subsystem.Key, subsystem.Value.Value);
            }

            var info = new TestInformation()
            {
                Subsystems = _subsystems,

                RequestSubsystemName = randomElement.Value.Name,
                RequestId = randomElement.Key,
                ExpectedStateResult = expectedState,
                ExpectedLogMessage = expectedLogMessage,
                LaunchRequestIds = requestIds,
                ExpectedStateResults = expectedStateResults
            };

            return info;
        }

        private KeyValuePair<Guid, SubsystemInfo> GetRandomSubsystem()
        {
            var random = new Random().Next(0, _subsystems.Length);
            return _subsystems.ElementAt(random);
        }

        private KeyValuePair<Guid, KeyValuePair<string, string>> GetRandomSubsystemAndItsExpectedResult()
        {
            // Assuming that no Launch invocation will be sent from the UI if the requested subsystem has been already started.
            var subsystem = GetRandomSubsystem();
            if (subsystem.Value.State != SubsystemState.Stopped) return GetRandomSubsystemAndItsExpectedResult();
            return new(subsystem.Key, new(subsystem.Value.Name, SubsystemState.Started));
        }
    }

    public class TestInformation
    {
        public Guid RequestId { get; set; }
        public string RequestSubsystemName { get; set; }
        public IEnumerable<KeyValuePair<Guid, string>> LaunchRequestIds { get; set; } = Enumerable.Empty<KeyValuePair<Guid, string>>();
        public KeyValuePair<Guid, SubsystemInfo>[] Subsystems { get; set; }
        public string ExpectedStateResult { get; set; }
        public IEnumerable<KeyValuePair<Guid, string>> ExpectedStateResults { get; set; } = Enumerable.Empty<KeyValuePair<Guid, string>>();
        public string ExpectedLogMessage { get; set; }
    }

    public static DummyStartType CreateDummyStartType(Guid id, string name)
    {
        return new DummyStartType(id, name);
    }

    public static DummyStopType CreateDummyStopType(Guid id)
    {
        return new DummyStopType(id);
    }

    public static void Start(DummyStartType dummy) { }
    public static void Stop(DummyStopType dummy) { }

    public record DummyStartType(Guid id, string name);
    public record DummyStopType(Guid id);
}
