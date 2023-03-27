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

using Microsoft.Extensions.Logging;

namespace ProcessExplorer.Server.Logging;

internal static partial class SourceGeneratedLoggerExtensions
{
    //Debugs
    [LoggerMessage(Level = LogLevel.Debug, Message = "The Process Explorer list is initialized", SkipEnabledCheck = false)]
    public static partial void ProcessListIsInitializedDebug(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "The Process Explorer gRPC server has been started...", SkipEnabledCheck = false)]
    public static partial void GrpcServerStartedDebug(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "The Process Explorer WebSocket server has been started...", SkipEnabledCheck = false)]
    public static partial void WebSocketServerStartedDebug(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "The Process Explorer gRPC server has been stopped...", SkipEnabledCheck = false)]
    public static partial void GrpcServerStoppedDebug(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "The Process Explorer WebSocket server has been stopped by client as a close message was received...", SkipEnabledCheck = false)]
    public static partial void WebSocketServerClosedDebug(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "The Process Explorer WebSocket server has been stopped...", SkipEnabledCheck = false)]
    public static partial void WebSocketServerStoppedDebug(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "A gRPC client subscribed with id: {id}.", SkipEnabledCheck = false)]
    public static partial void GrpcClientSubscribedDebug(this ILogger logger, string id);

    [LoggerMessage(Level = LogLevel.Debug, Message = "A WebSocket client subscribed with id: {id}...", SkipEnabledCheck = false)]
    public static partial void WebSocketClientSubscribedDebug(this ILogger logger, string id);

    [LoggerMessage(Level = LogLevel.Debug, Message = "A WebSocket connection handler have been added...", SkipEnabledCheck = false)]
    public static partial void WebSocketConnectionHandlerAddedDebug(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "A gRPC client sent a message with topic: {topic}.", SkipEnabledCheck = false)]
    public static partial void GrpcClientMessageReceivedDebug(this ILogger logger, string topic);

    [LoggerMessage(Level = LogLevel.Debug, Message = "A WebSocket client sent a message with topic: {topic}.", SkipEnabledCheck = false)]
    public static partial void WebSocketClientMessageReceivedDebug(this ILogger logger, string topic);

    //Errors
    [LoggerMessage(Level = LogLevel.Error, Message = "Sending connection information to the UI(s) was unsuccessful. Detailed axception: `{exception}`", SkipEnabledCheck = false)]
    public static partial void AddConnectionsError(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Sending a process information to the UI(s) was unsuccessful. Detailed axception: `{exception}`", SkipEnabledCheck = false)]
    public static partial void AddProcessError(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Sending process information to the UI(s) was unsuccessful. Detailed axception: `{exception}`", SkipEnabledCheck = false)]
    public static partial void AddProcessesError(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Sending a runtime information to the UI(s) was unsuccessful. Detailed axception: `{exception}`", SkipEnabledCheck = false)]
    public static partial void AddRuntimeError(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Sending a process information to the UI(s) was unsuccessful. Detailed axception: `{exception}`", SkipEnabledCheck = false)]
    public static partial void AddRuntimesError(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Removing a process by id command on the UI(s) was unsuccessful. Detailed axception: `{exception}`", SkipEnabledCheck = false)]
    public static partial void RemoveProcessByIdError(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Updating a connection information on the UI(s) was unsuccessful. Detailed axception: `{exception}`", SkipEnabledCheck = false)]
    public static partial void UpdateConnectionError(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Updating a environment variables on the UI(s) was unsuccessful. Detailed axception: `{exception}`", SkipEnabledCheck = false)]
    public static partial void UpdateEnvironmentVariablesError(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Updating modules information on the UI(s) was unsuccessful. Detailed axception: `{exception}`", SkipEnabledCheck = false)]
    public static partial void UpdateModulesError(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Updating a process information on the UI(s) was unsuccessful. Detailed axception: `{exception}`", SkipEnabledCheck = false)]
    public static partial void UpdateProcessError(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Updating registrations information on the UI(s) was unsuccessful. Detailed axception: `{exception}`", SkipEnabledCheck = false)]
    public static partial void UpdateRegistrationsError(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Adding subsystems on the UI(s) was unsuccessful. Detailed axception: `{exception}`", SkipEnabledCheck = false)]
    public static partial void AddSubsystemsError(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Updating subsystem information on the UI(s) was unsuccessful. Detailed axception: `{exception}`", SkipEnabledCheck = false)]
    public static partial void UpdateSubsystemError(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Updating the status of the process: `{pid}` on the UI(s) was unsuccessful. Detailed axception: `{exception}`", SkipEnabledCheck = false)]
    public static partial void UpdateProcessStatusError(this ILogger logger, int pid, Exception ex, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error occurred while subscribing as a gRPC client: {id}. Detailed axception: `{exception}`", SkipEnabledCheck = false)]
    public static partial void GrpcSubscribeError(this ILogger logger, string id, Exception ex, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error occurred while subscribing as a websocket client. Detailed axception: `{exception}`", SkipEnabledCheck = false)]
    public static partial void WebSocketSubscribeError(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error occurred while starting a subsystem with id: {id}.", SkipEnabledCheck = false)]
    public static partial void StartSubsystemError(this ILogger logger, string id);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error occurred while restarting a subsystem with id: {id}.", SkipEnabledCheck = false)]
    public static partial void RestartSubsystemError(this ILogger logger, string id);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error occurred while termianting a subsystem with id: {id}.", SkipEnabledCheck = false)]
    public static partial void ShutdownSubsystemError(this ILogger logger, string id);

    [LoggerMessage(Level = LogLevel.Error, Message = "The Process Explorer WebSocket server has been stopped unexpectedly: {exception}...", SkipEnabledCheck = false)]
    public static partial void WebSocketServerStoppedError(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error while reading, deserializing gRPC message: {exception}...", SkipEnabledCheck = false)]
    public static partial void GrpcMessageReadingError(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error while reading, deserializing WebSocket message: {exception}...", SkipEnabledCheck = false)]
    public static partial void WebSocketMessageReadingError(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error while handling WebSocket message for subsystem handling: {exception}...", SkipEnabledCheck = false)]
    public static partial void WebSocketMessageHandlingError(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error while handling WebSocket message for subsystem handling: {exception}...", SkipEnabledCheck = false)]
    public static partial void GrpcMessageHandlingError(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error while setting up process explorer server. Detailed exception: {exception}...", SkipEnabledCheck = false)]
    public static partial void ProcessExplorerSetupError(this ILogger logger, Exception ex, Exception exception);

    //Warnings
    [LoggerMessage(Level = LogLevel.Warning, Message = "No timeout was declared while using CancellationToken for gRPC server...", SkipEnabledCheck = false)]
    public static partial void GrpcCancellationTokenWarning(this ILogger logger);
}

