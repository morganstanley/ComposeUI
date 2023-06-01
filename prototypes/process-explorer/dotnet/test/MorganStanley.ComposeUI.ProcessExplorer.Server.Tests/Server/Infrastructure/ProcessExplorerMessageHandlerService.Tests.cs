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

using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Core.Testing;
using Microsoft.Extensions.Logging;
using Moq;
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions;
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Infrastructure;
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Processes;
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Subsystems;
using MorganStanley.ComposeUI.ProcessExplorer.Core.Factories;
using MorganStanley.ComposeUI.ProcessExplorer.Server.Server.Infrastructure.Grpc;
using ProcessExplorer.Abstractions.Infrastructure.Protos;

namespace MorganStanley.ComposeUI.ProcessExplorer.Server.Tests.Server.Infrastructure;

public class ProcessExplorerMessageHandlerServiceTests
{
    private static readonly Empty _empty = new();

    [Fact]
    public async Task Subscribe_will_trigger_processInfoAggregator_and_throw_error()
    {
        var processMonitorMock = new Mock<IProcessInfoMonitor>();
        var uiHandlerMock = new Mock<IUiHandler>();
        var subsystemControllerMock = new Mock<ISubsystemController>();
        var loggerMock = CreateProcessInfoAggregatorLoggerMock();

        var processInfoAggregator = ProcessAggregatorFactory.CreateProcessInfoAggregator(
            processInfoMonitor: processMonitorMock.Object, //this will run to exception as no object has been declared for the process id collection
            handler: uiHandlerMock.Object,
            subsystemController: subsystemControllerMock.Object,
            logger: loggerMock.Object);

        var serverStreamWriterMock = new Mock<IServerStreamWriter<Message>>();

        var messageHandlerService = new ProcessExplorerMessageHandlerService(processInfoAggregator, loggerMock.Object);

        await messageHandlerService.Subscribe(
            _empty, 
            serverStreamWriterMock.Object, 
            TestServerCallContext.Create(
                method: "Subscribe",
                host: "dummyHost",
                deadline: DateTime.MaxValue,
                requestHeaders: Metadata.Empty,
                cancellationToken: CancellationToken.None,
                peer: "dummyPeer",
                authContext: new AuthContext(null, new()),
                contextPropagationToken: null,
                writeHeadersFunc: null,
                writeOptionsGetter: null,
                writeOptionsSetter: null));

        loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("A gRPC client subscribed with id:")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

        uiHandlerMock.Verify(x => x.AddClientConnection(It.IsAny<Guid>(), It.IsAny<IClientConnection<It.IsAnyType>>()), Times.Once);
        uiHandlerMock.Verify(x => x.SubscriptionIsAliveUpdate(), Times.Once);

        loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error occurred while subscribing as a gRPC client:")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

        uiHandlerMock.Verify(x => x.RemoveClientConnection(It.IsAny<Guid>()), Times.Once);
    }

    //TODO(Lilla): Based on OS
    [Fact]
    public async Task Subscribe_will_trigger_processInfoAggregator()
    {
        var processMonitorLoggerMock = new Mock<ILogger<ProcessInfoMonitor>>();
        var processMonitor = ProcessMonitorFactory.CreateProcessInfoMonitorWindows(processMonitorLoggerMock.Object);
        var uiHandlerMock = new Mock<IUiHandler>();
        var subsystemControllerMock = new Mock<ISubsystemController>();
        var processInfoAggregatorLoggerMock = CreateProcessInfoAggregatorLoggerMock();

        var processInfoAggregator = ProcessAggregatorFactory.CreateProcessInfoAggregator(
            processInfoMonitor: processMonitor,
            handler: uiHandlerMock.Object,
            subsystemController: subsystemControllerMock.Object,
            logger: processInfoAggregatorLoggerMock.Object);

        var serverStreamWriterMock = new Mock<IServerStreamWriter<Message>>();

        var messageHandlerService = new ProcessExplorerMessageHandlerService(processInfoAggregator, processInfoAggregatorLoggerMock.Object);

        await messageHandlerService.Subscribe(
            _empty,
            serverStreamWriterMock.Object,
            TestServerCallContext.Create(
                method: "Subscribe",
                host: "dummyHost",
                deadline: DateTime.MaxValue,
                requestHeaders: Metadata.Empty,
                cancellationToken: new CancellationTokenSource(TimeSpan.FromSeconds(1)).Token,
                peer: "dummyPeer",
                authContext: new AuthContext(null, new()),
                contextPropagationToken: null,
                writeHeadersFunc: null,
                writeOptionsGetter: null,
                writeOptionsSetter: null));

        processInfoAggregatorLoggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("A gRPC client subscribed with id:")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

        uiHandlerMock.Verify(x => x.AddClientConnection(It.IsAny<Guid>(), It.IsAny<IClientConnection<It.IsAnyType>>()), Times.Once);
        uiHandlerMock.Verify(x => x.SubscriptionIsAliveUpdate(), Times.Once);
        uiHandlerMock.Verify(x => x.AddProcesses(It.IsAny<IEnumerable<ProcessInfoData>>()), Times.Once);
        uiHandlerMock.Verify(x => x.AddRuntimeInfo(It.IsAny<IEnumerable<KeyValuePair<string, Abstractions.Entities.ProcessInfoCollectorData>>>()), Times.Once);
        uiHandlerMock.Verify(x => x.AddSubsystems(It.IsAny<IEnumerable<KeyValuePair<Guid, SubsystemInfo>>>()), Times.Once);
        uiHandlerMock.Verify(x => x.RemoveClientConnection(It.IsAny<Guid>()), Times.Once);
    }

    private static Mock<ILogger<IProcessInfoAggregator>> CreateProcessInfoAggregatorLoggerMock()
    {
        var loggerMock = new Mock<ILogger<IProcessInfoAggregator>>();

        var loggerFilterOptions = new LoggerFilterOptions();

        loggerFilterOptions.AddFilter("", LogLevel.Debug);

        loggerMock
            .Setup(x => x.IsEnabled(It.IsAny<LogLevel>()))
            .Returns<LogLevel>(level => loggerFilterOptions.MinLevel <= level);

        return loggerMock;
    }
}
