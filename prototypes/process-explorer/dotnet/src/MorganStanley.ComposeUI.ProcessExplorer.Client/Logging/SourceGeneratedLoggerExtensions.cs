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

namespace MorganStanley.ComposeUI.ProcessExplorer.Client.Logging;

internal static partial class SourceGeneratedLoggerExtensions
{
    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Cannot send connection status changed message to the server. Detailed exception: `{exception}`", SkipEnabledCheck = false)]
    public static partial void ConnectionStatusSendError(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "LocalCollector could not send message about runtimeinformation to the server. Detailed exception: `{exception}`", SkipEnabledCheck = false)]
    public static partial void AddRuntimeInfoError(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "LocalCollector could not send message about connection collection to the server. Detailed exception: `{exception}`", SkipEnabledCheck = false)]
    public static partial void AddConnectionCollectionError(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "LocalCollector could not send message about updating a connection to the server. Detailed exception: `{exception}`", SkipEnabledCheck = false)]
    public static partial void UpdateConnectionInformationError(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "LocalCollector could not send message about updating environment variables to the server. Detailed exception: `{exception}`", SkipEnabledCheck = false)]
    public static partial void UpdateEnvironmentVariableInformationError(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "LocalCollector could not send message about updating registrations to the server. Detailed exception: `{exception}`", SkipEnabledCheck = false)]
    public static partial void UpdateRegistrationInformationError(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "LocalCollector could not send message about updating modules to the server. Detailed exception: `{exception}`", SkipEnabledCheck = false)]
    public static partial void UpdateModuleInformationError(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "LocalCollector could not send message about updating connection status to the server. Id: `{connectionId}`. Detailed exception: `{exception}`", SkipEnabledCheck = false)]
    public static partial void UpdateConnectionStatusError(this ILogger logger, string connectionId, Exception ex, Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Connection with Id: `{connectionId}` has been updated to status: `{connectionStatus}`", SkipEnabledCheck = false)]
    public static partial void ConnectionUpdatedDebug(this ILogger logger, string connectionId, string connectionStatus);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Sending runtime information collected by LocalCollector.", SkipEnabledCheck = false)]
    public static partial void SendingLocalCollectorRuntimeInformationDebug(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Sending runtime information collected by LocalCollector. Id: `{id}`", SkipEnabledCheck = false)]
    public static partial void SendingLocalCollectorRuntimeInformationWithIdDebug(this ILogger logger, string id);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Sending connection collection collected by LocalCollector.", SkipEnabledCheck = false)]
    public static partial void SendingLocalCollectorConnectionCollectionDebug(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Sending connection collection collected by LocalCollector. Id: `{id}`", SkipEnabledCheck = false)]
    public static partial void SendingLocalCollectorConnectionCollectionWithIdDebug(this ILogger logger, string id);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Sending a connection collected by LocalCollector. Id: `{id}`", SkipEnabledCheck = false)]
    public static partial void SendingLocalCollectorConnectionDebug(this ILogger logger, string id);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Sending environment variables update collected by LocalCollector. Id: `{id}`", SkipEnabledCheck = false)]
    public static partial void SendingLocalCollectorEnvironmentVariablesDebug(this ILogger logger, string id);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Sending registrations update collected by LocalCollector. Id: `{id}`", SkipEnabledCheck = false)]
    public static partial void SendingLocalCollectorRegistrationsDebug(this ILogger logger, string id);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Sending modules update collected by LocalCollector. Id: `{id}`", SkipEnabledCheck = false)]
    public static partial void SendingLocalCollectorModulesDebug(this ILogger logger, string id);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Sending connection status update collected by LocalCollector. Id: `{id}`", SkipEnabledCheck = false)]
    public static partial void SendingLocalCollectorConnectionStatusDebug(this ILogger logger, string id);

    [LoggerMessage(Level = LogLevel.Debug, Message = "EnvironmentVariable couldn't be parsed : {key}, {value}", SkipEnabledCheck = false)]
    public static partial void EnvironmentVariableParingErrorDebug(this ILogger logger, string key, string value);

    [LoggerMessage(Level = LogLevel.Debug, Message = "EnvironmentVariable couldn't be added : {key}, {value}", SkipEnabledCheck = false)]
    public static partial void EnvironmentVariableAddErrorDebug(this ILogger logger, string key, string value);
}
