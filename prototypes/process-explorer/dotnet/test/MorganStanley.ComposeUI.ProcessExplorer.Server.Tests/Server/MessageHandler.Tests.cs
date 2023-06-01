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

using Google.Protobuf;
using Google.Protobuf.Collections;
using Microsoft.Extensions.Logging;
using Moq;
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions;
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Entities;
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Infrastructure;
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Processes;
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Subsystems;
using MorganStanley.ComposeUI.ProcessExplorer.Core.Factories;
using MorganStanley.ComposeUI.ProcessExplorer.Server.Server;
using ProcessExplorer.Abstractions.Infrastructure.Protos;
using Module = ProcessExplorer.Abstractions.Infrastructure.Protos.Module;
using ProcessInfoCollectorData = ProcessExplorer.Abstractions.Infrastructure.Protos.ProcessInfoCollectorData;

namespace MorganStanley.ComposeUI.ProcessExplorer.Server.Tests.Server;

public class MessageHandlerTests
{
    [Fact]
    public void HandleIncomingGrpcMessages_will_handle_terminate_subsystems_command()
    {
        var message = new Message()
        {
            Action = ActionType.TerminateSubsystemsAction,
            Subsystems =
            {
                new MapField<string, Subsystem>
                {
                    { "dummySubsystemGuid1", new Subsystem() { Name = "dummySubsystem1", Url = "dummyUrl1", UiType = "dummyUiType1", State = "Started", StartupType = "dummyStartUpType1", Port = 1, Path = "dummyPath1", Description = "dummyDescription1", AutomatedStart = false, Arguments = { { "dummyArgument1"} } } },
                    { "dummySubsystemGuid2", new Subsystem() { Name = "dummySubsystem2", Url = "dummyUrl2", UiType = "dummyUiType2", State = "Started", StartupType = "dummyStartUpType2", Port = 1, Path = "dummyPath2", Description = "dummyDescription2", AutomatedStart = false, Arguments = { { "dummyArgument2"} } } }
                }
            }
        };

        var processMonitorMock = new Mock<IProcessInfoMonitor>();
        var uiHandlerMock = new Mock<IUiHandler>();
        var subsystemControllerMock = new Mock<ISubsystemController>();
        var loggerMock = CreateProcessInfoAggregatorLoggerMock();

        var processInfoAggregator = ProcessAggregatorFactory.CreateProcessInfoAggregator(
            processInfoMonitor: processMonitorMock.Object,
            handler: uiHandlerMock.Object,
            subsystemController: subsystemControllerMock.Object,
            logger: loggerMock.Object);

        MessageHandler.HandleIncomingGrpcMessages(
            message,
            processInfoAggregator,
            loggerMock.Object);

        subsystemControllerMock.Verify(x => x.ShutdownSubsystems(It.IsAny<IEnumerable<string>>()), Times.Once);

        loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unable to set ProcessMonitor's collection change events. Detailed exception:")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
    }

    [Fact]
    public void HandleIncomingGrpcMessages_will_handle_restart_subsystems_command()
    {
        var message = new Message()
        {
            Action = ActionType.RestartSubsystemsAction,
            Subsystems =
            {
                new MapField<string, Subsystem>
                {
                    { "dummySubsystemGuid1", new Subsystem() { Name = "dummySubsystem1", Url = "dummyUrl1", UiType = "dummyUiType1", State = "Started", StartupType = "dummyStartUpType1", Port = 1, Path = "dummyPath1", Description = "dummyDescription1", AutomatedStart = false, Arguments = { { "dummyArgument1"} } } },
                    { "dummySubsystemGuid2", new Subsystem() { Name = "dummySubsystem2", Url = "dummyUrl2", UiType = "dummyUiType2", State = "Started", StartupType = "dummyStartUpType2", Port = 1, Path = "dummyPath2", Description = "dummyDescription2", AutomatedStart = false, Arguments = { { "dummyArgument2"} } } }
                }
            }
        };

        var processMonitorMock = new Mock<IProcessInfoMonitor>();
        var uiHandlerMock = new Mock<IUiHandler>();
        var subsystemControllerMock = new Mock<ISubsystemController>();
        var loggerMock = CreateProcessInfoAggregatorLoggerMock();

        var processInfoAggregator = ProcessAggregatorFactory.CreateProcessInfoAggregator(
            processInfoMonitor: processMonitorMock.Object,
            handler: uiHandlerMock.Object,
            subsystemController: subsystemControllerMock.Object,
            logger: loggerMock.Object);

        MessageHandler.HandleIncomingGrpcMessages(
            message,
            processInfoAggregator,
            loggerMock.Object);

        subsystemControllerMock.Verify(x => x.RestartSubsystems(It.IsAny<IEnumerable<string>>()), Times.Once);

        loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unable to set ProcessMonitor's collection change events. Detailed exception:")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
    }

    [Fact]
    public void HandleIncomingGrpcMessages_will_handle_launch_subsystems_command()
    {
        var message = new Message()
        {
            Action = ActionType.LaunchSubsystemsAction,
            Subsystems =
            {
                new MapField<string, Subsystem>
                {
                    { "dummySubsystemGuid1", new Subsystem() { Name = "dummySubsystem1", Url = "dummyUrl1", UiType = "dummyUiType1", State = "Started", StartupType = "dummyStartUpType1", Port = 1, Path = "dummyPath1", Description = "dummyDescription1", AutomatedStart = false, Arguments = { { "dummyArgument1"} } } },
                    { "dummySubsystemGuid2", new Subsystem() { Name = "dummySubsystem2", Url = "dummyUrl2", UiType = "dummyUiType2", State = "Started", StartupType = "dummyStartUpType2", Port = 1, Path = "dummyPath2", Description = "dummyDescription2", AutomatedStart = false, Arguments = { { "dummyArgument2"} } } }
                }
            }
        };

        var processMonitorMock = new Mock<IProcessInfoMonitor>();
        var uiHandlerMock = new Mock<IUiHandler>();
        var subsystemControllerMock = new Mock<ISubsystemController>();
        var loggerMock = CreateProcessInfoAggregatorLoggerMock();

        var processInfoAggregator = ProcessAggregatorFactory.CreateProcessInfoAggregator(
            processInfoMonitor: processMonitorMock.Object,
            handler: uiHandlerMock.Object,
            subsystemController: subsystemControllerMock.Object,
            logger: loggerMock.Object);

        MessageHandler.HandleIncomingGrpcMessages(
            message,
            processInfoAggregator,
            loggerMock.Object);

        subsystemControllerMock.Verify(x => x.LaunchSubsystems(It.IsAny<IEnumerable<string>>()), Times.Once);

        loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unable to set ProcessMonitor's collection change events. Detailed exception:")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
    }

    [Fact]
    public void HandleIncomingGrpcMessages_will_handle_launch_subsystems_with_delay_command()
    {
        var id = Guid.NewGuid();

        var message = new Message()
        {
            Action = ActionType.LaunchSubsystemsWithDelayAction,
            Subsystems =
            {
                new MapField<string, Subsystem>
                {
                    { id.ToString(), new Subsystem() { Name = "dummySubsystem1" } }
                }
            },
            PeriodOfDelay = 15
        };

        var processMonitorMock = new Mock<IProcessInfoMonitor>();
        var uiHandlerMock = new Mock<IUiHandler>();
        var subsystemControllerMock = new Mock<ISubsystemController>();
        var loggerMock = CreateProcessInfoAggregatorLoggerMock();

        var processInfoAggregator = ProcessAggregatorFactory.CreateProcessInfoAggregator(
            processInfoMonitor: processMonitorMock.Object,
            handler: uiHandlerMock.Object,
            subsystemController: subsystemControllerMock.Object,
            logger: loggerMock.Object);

        MessageHandler.HandleIncomingGrpcMessages(
            message,
            processInfoAggregator,
            loggerMock.Object);

        subsystemControllerMock.Verify(x => x.LaunchSubsystemAfterTime(id, message.PeriodOfDelay), Times.Once);

        loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unable to set ProcessMonitor's collection change events. Detailed exception:")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
    }

    [Fact]
    public async Task HandleIncomingGrpcMessages_will_handle_add_runtimeInfo_command()
    {
        var message = new Message()
        {
            Action = ActionType.AddRuntimeInfoAction,
            AssemblyId = "dummyAssemblyId",
            RuntimeInfo = new ProcessInfoCollectorData()
            {
                Id = 1,
                Connections =
                {
                    new RepeatedField<Connection>()
                    {
                        {
                            new Connection()
                            {
                                Id = Guid.NewGuid().ToString(),
                                Name = "dummyConenctionName",
                                LocalEndpoint = "dummyLocalEndpoint",
                                RemoteApplication = "dummyRemoteApplication",
                                RemoteEndpoint = "dummyRemoteEndpoint",
                                Status = "dummyStatus",
                                RemoteHostname = "dummyRemoteHostname",
                                ConnectionInformation =
                                {
                                    { "dummyKey", "dummyValue" }
                                }
                            }
                        }
                    }
                },
                EnvironmentVariables =
                {
                    new MapField<string, string>()
                    {
                        { "dummyEnvKey", "dummyEnvValue" }
                    }
                },
                Modules =
                {
                    new RepeatedField<Module>()
                    {
                        new Module()
                        {
                            Name = "dummyModule",
                            Location = "dummyLocation",
                            Version = "dummyVersion",
                            VersionRedirectedFrom = "dummyVersionRedirectedFrom",
                            PublicKeyToken = ByteString.CopyFrom(new byte[]{1, 0})
                        }
                    }
                },
                Registrations =
                {
                    new RepeatedField<Registration>()
                    {
                        new Registration()
                        {
                            ImplementationType = "dummyImplementationType",
                            ServiceType = "dummyServiceTyoe",
                            LifeTime = "dummyLifeTime",
                        }
                    }
                }
            }
        };

        var loggerMock = CreateProcessInfoAggregatorLoggerMock();

        var processInfoAggregatorMock = new Mock<IProcessInfoAggregator>();

        MessageHandler.HandleIncomingGrpcMessages(
            message,
            processInfoAggregatorMock.Object,
            loggerMock.Object);

        processInfoAggregatorMock.Verify(x => x.AddRuntimeInformation(message.AssemblyId, It.IsAny<Abstractions.Entities.ProcessInfoCollectorData>()), Times.Once);
    }

    [Fact]
    public void HandleIncomingGrpcMessages_will_handle_add_connectionList_command()
    {
        var message = new Message()
        {
            Action = ActionType.AddConnectionListAction,
            AssemblyId = "dummyAssemblyId",
            Connections =
            {
                new RepeatedField<Connection>()
                {
                    new Connection()
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "dummyConenctionName",
                        LocalEndpoint = "dummyLocalEndpoint",
                        RemoteApplication = "dummyRemoteApplication",
                        RemoteEndpoint = "dummyRemoteEndpoint",
                        Status = "dummyStatus",
                        RemoteHostname = "dummyRemoteHostname",
                        ConnectionInformation =
                        {
                            { "dummyKey", "dummyValue" }
                        }
                    }
                }
            }
        };

        var loggerMock = CreateProcessInfoAggregatorLoggerMock();

        var processInfoAggregatorMock = new Mock<IProcessInfoAggregator>();

        MessageHandler.HandleIncomingGrpcMessages(
            message,
            processInfoAggregatorMock.Object,
            loggerMock.Object);

        processInfoAggregatorMock.Verify(x => x.AddConnectionCollection(message.AssemblyId, It.IsAny<IEnumerable<IConnectionInfo>>()), Times.Once);
    }

    [Fact]
    public void HandleIncomingGrpcMessages_will_handle_update_connection_command()
    {
        var message = new Message()
        {
            Action = ActionType.UpdateConnectionAction,
            AssemblyId = "dummyAssemblyId",
            Connections =
            {
                new RepeatedField<Connection>()
                {
                    new Connection()
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "dummyConenctionName",
                        LocalEndpoint = "dummyLocalEndpoint",
                        RemoteApplication = "dummyRemoteApplication",
                        RemoteEndpoint = "dummyRemoteEndpoint",
                        Status = "dummyStatus",
                        RemoteHostname = "dummyRemoteHostname",
                        ConnectionInformation =
                        {
                            { "dummyKey", "dummyValue" }
                        }
                    }
                }
            }
        };

        var loggerMock = CreateProcessInfoAggregatorLoggerMock();

        var processInfoAggregatorMock = new Mock<IProcessInfoAggregator>();

        MessageHandler.HandleIncomingGrpcMessages(
            message,
            processInfoAggregatorMock.Object,
            loggerMock.Object);

        processInfoAggregatorMock.Verify(x => x.UpdateOrAddConnectionInfo(message.AssemblyId, It.IsAny<IConnectionInfo>()), Times.Once);
    }

    [Fact]
    public void HandleIncomingGrpcMessages_will_handle_update_environment_variables_command()
    {
        var message = new Message()
        {
            Action = ActionType.UpdateEnvironmentVariablesAction,
            AssemblyId = "dummyAssemblyId",
            EnvironmentVariables =
            {
                new MapField<string, string>()
                {
                    { "dummyEnvKey", "dummyEnvValue" }
                }
            },
        };

        var loggerMock = CreateProcessInfoAggregatorLoggerMock();

        var processInfoAggregatorMock = new Mock<IProcessInfoAggregator>();

        MessageHandler.HandleIncomingGrpcMessages(
            message,
            processInfoAggregatorMock.Object,
            loggerMock.Object);

        processInfoAggregatorMock.Verify(x => x.UpdateOrAddEnvironmentVariablesInfo(message.AssemblyId, It.IsAny<IEnumerable<KeyValuePair<string, string>>>()), Times.Once);
    }

    [Fact]
    public void HandleIncomingGrpcMessages_will_handle_update_registrations_command()
    {
        var message = new Message()
        {
            Action = ActionType.UpdateRegistrationsAction,
            AssemblyId = "dummyAssemblyId",
            Registrations =
            {
                new RepeatedField<Registration>()
                {
                    new Registration()
                    {
                        ImplementationType = "dummyImplementationType",
                        ServiceType = "dummyServiceTyoe",
                        LifeTime = "dummyLifeTime",
                    }
                }
            }
        };

        var loggerMock = CreateProcessInfoAggregatorLoggerMock();

        var processInfoAggregatorMock = new Mock<IProcessInfoAggregator>();

        MessageHandler.HandleIncomingGrpcMessages(
            message,
            processInfoAggregatorMock.Object,
            loggerMock.Object);

        processInfoAggregatorMock.Verify(x => x.UpdateRegistrations(message.AssemblyId, It.IsAny<IEnumerable<RegistrationInfo>>()), Times.Once);
    }

    [Fact]
    public void HandleIncomingGrpcMessages_will_handle_update_modules_command()
    {
        var message = new Message()
        {
            Action = ActionType.UpdateModulesAction,
            AssemblyId = "dummyAssemblyId",
            Modules =
            {
                new RepeatedField<Module>()
                {
                    new Module()
                    {
                        Name = "dummyModule",
                        Location = "dummyLocation",
                        Version = "dummyVersion",
                        VersionRedirectedFrom = "dummyVersionRedirectedFrom",
                        PublicKeyToken = ByteString.CopyFrom(new byte[]{1, 0})
                    }
                }
            },
        };

        var loggerMock = CreateProcessInfoAggregatorLoggerMock();

        var processInfoAggregatorMock = new Mock<IProcessInfoAggregator>();

        MessageHandler.HandleIncomingGrpcMessages(
            message,
            processInfoAggregatorMock.Object,
            loggerMock.Object);

        processInfoAggregatorMock.Verify(x => x.UpdateOrAddModuleInfo(message.AssemblyId, It.IsAny<IEnumerable<ModuleInfo>>()), Times.Once);
    }

    [Fact]
    public void HandleIncomingGrpcMessages_will_handle_update_connection_status_command()
    {
        var message = new Message()
        {
            Action = ActionType.UpdateConnectionStatusAction,
            AssemblyId = "dummyAssemblyId",
            ConnectionStatusChanges =
            {
                new MapField<string, string>()
                {
                    { "dummyId", "dummyStatusChangeValue" }
                }
            }
        };

        var loggerMock = CreateProcessInfoAggregatorLoggerMock();

        var processInfoAggregatorMock = new Mock<IProcessInfoAggregator>();

        MessageHandler.HandleIncomingGrpcMessages(
            message,
            processInfoAggregatorMock.Object,
            loggerMock.Object);

        processInfoAggregatorMock.Verify(x => x.UpdateConnectionStatus(message.AssemblyId, "dummyId", "dummyStatusChangeValue"), Times.Once);
    }

    [Fact]
    public async Task HandleIncomingGrpcMessages_will_log_exception()
    {
        var message = new Message()
        {
            Action = ActionType.UpdateConnectionAction,
            AssemblyId = "dummyAssemblyId",
            Connections =
            {
                new RepeatedField<Connection>()
                {
                    new Connection()
                    {
                        Id = "BADGUIDID_Will_THROW_EXCEPTION",
                        Name = "dummyConenctionName",
                        LocalEndpoint = "dummyLocalEndpoint",
                        RemoteApplication = "dummyRemoteApplication",
                        RemoteEndpoint = "dummyRemoteEndpoint",
                        Status = "dummyStatus",
                        RemoteHostname = "dummyRemoteHostname",
                        ConnectionInformation =
                        {
                            { "dummyKey", "dummyValue" }
                        }
                    }
                }
            }
        };

        var loggerMock = CreateProcessInfoAggregatorLoggerMock();

        var processInfoAggregatorMock = new Mock<IProcessInfoAggregator>();

        MessageHandler.HandleIncomingGrpcMessages(
            message,
            processInfoAggregatorMock.Object,
            loggerMock.Object);

        processInfoAggregatorMock.Verify(x => x.UpdateOrAddConnectionInfo(message.AssemblyId, It.IsAny<IConnectionInfo>()), Times.Never);

        loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error while handling gRPC message for subsystem handling:")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
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
