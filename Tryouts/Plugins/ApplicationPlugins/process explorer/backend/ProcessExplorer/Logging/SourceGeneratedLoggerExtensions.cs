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

namespace ProcessExplorer.Logging;
internal static partial class SourceGeneratedLoggerExtensions
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Couldn't find the PPID for the PID `{pid}`. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
    private static partial void ManagementObjectPpid(this ILogger logger, int pid, Exception ex, Exception exception);

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "ProcessMonitor UICommunicator is set.", SkipEnabledCheck = false)]
    public static partial void ProcessMonitorCommunicatorIsSetDebug(this ILogger logger);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Cannot add the connections to collection. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
    private static partial void ConnectionCollectionCannotBeAdded(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Connection cannot be updated. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
    private static partial void ConnectionCannotBeUpdated(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Couldn't watch the processes. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
    private static partial void EnvironmentVariablesCannotBeUpdated(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Registrations cannot be updated. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
    private static partial void RegistrationsCannotBeUpdated(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Modules cannot be updated. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
    private static partial void ModulesCannotBeUpdated(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "A UIHandler is removed from the collection", SkipEnabledCheck = false)]
    public static partial void UiCommunicatorIsRemovedDebug(this ILogger logger);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Cannot modify subsystem's state with ID `{subsystemId}`. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
    private static partial void ModifyingSubsystemsState(this ILogger logger, string subsystemId, Exception ex, Exception exception);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Error while terminating the process, ID: {pid}. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
    private static partial void CannotTerminateProcess(this ILogger logger, int pid, Exception ex, Exception exception);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Updating the information on UI cannot be completed. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
    private static partial void UiInformationCannotBeUpdated(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Cannot send launch request to the SubsystemLauncher. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
    private static partial void CannotSendLaunchRequest(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Cannot terminate a subsystem. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
    private static partial void CannotTerminateSubsystem(this ILogger logger, Exception ex, Exception exception);


    public static void UiInformationCannotBeUpdatedError(this ILogger logger, Exception exception)
    {
        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.UiInformationCannotBeUpdated(exception, exception);
        }
    }
    public static void CannotTerminateSubsystemError(this ILogger logger, Exception exception)
    {
        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.CannotTerminateSubsystem(exception, exception);
        }
    }

    public static void CannotSendLaunchRequestError(this ILogger logger, Exception exception)
    {
        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.CannotSendLaunchRequest(exception, exception);
        }
    }

    public static void ManagementObjectPPID(this ILogger logger, int pid, Exception exception)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.ManagementObjectPpid(pid, exception, exception);
        }
    }

    public static void ConnectionCollectionCannotBeAddedError(this ILogger logger, Exception exception)
    {
        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.ConnectionCollectionCannotBeAdded(exception, exception);
        }
    }

    public static void ConnectionCannotBeUpdatedError(this ILogger logger, Exception exception)
    {
        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.ConnectionCannotBeUpdated(exception, exception);
        }
    }

    public static void EnvironmentVariablesCannotBeUpdatedError(this ILogger logger, Exception exception)
    {
        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.EnvironmentVariablesCannotBeUpdated(exception, exception);
        }
    }

    public static void RegistrationsCannotBeUpdatedError(this ILogger logger, Exception exception)
    {
        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.RegistrationsCannotBeUpdated(exception, exception);
        }
    }

    public static void ModulesCannotBeUpdatedError(this ILogger logger, Exception exception)
    {
        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.ModulesCannotBeUpdated(exception, exception);
        }
    }

    public static void CannotTerminateProcessError(this ILogger logger, int pid, Exception exception)
    {
        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.CannotTerminateProcess(pid, exception, exception);
        }
    }

    public static void ModifyingSubsystemsStateError(this ILogger logger, Guid subsystemId, Exception exception)
    {
        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.ModifyingSubsystemsState(subsystemId.ToString(), exception, exception);
        }
    }
}
