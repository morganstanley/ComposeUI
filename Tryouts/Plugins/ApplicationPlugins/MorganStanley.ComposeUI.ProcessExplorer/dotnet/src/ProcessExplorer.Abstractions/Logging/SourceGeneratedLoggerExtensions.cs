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

namespace ProcessExplorer.Abstractions.Logging;

public static partial class SourceGeneratedLoggerExtensions
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Couldn't find the PPID for the PID `{pid}`. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
    private static partial void ManagementObjectPpid(this ILogger logger, int pid, Exception ex, Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "ProcessInfoManager is not initialized in the ProcessMonitor class", SkipEnabledCheck = false)]
    public static partial void ProcessInfoManagerNotInitializedDebug(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Starting a subsystem with id: {id}", SkipEnabledCheck = false)]
    public static partial void SubsystemStartedDebug(this ILogger logger, string id);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Subsystem with id: {id} is already started", SkipEnabledCheck = false)]
    public static partial void SubsystemAlreadyStartedDebug(this ILogger logger, string id);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Stopping a subsystem with id: {id}", SkipEnabledCheck = false)]
    public static partial void SubsystemStoppingDebug(this ILogger logger, string id);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Subsystem with id: {id} is already stopped", SkipEnabledCheck = false)]
    public static partial void SubsystemAlreadyStoppedDebug(this ILogger logger, string id);

    [LoggerMessage(Level = LogLevel.Debug, Message = "ProcessMonitor UICommunicator is set.", SkipEnabledCheck = false)]
    public static partial void ProcessMonitorCommunicatorIsSetDebug(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Cannot add the connections to collection. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
    private static partial void ConnectionCollectionCannotBeAdded(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Connection cannot be updated. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
    private static partial void ConnectionCannotBeUpdated(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Couldn't watch the processes. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
    private static partial void EnvironmentVariablesCannotBeUpdated(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Registrations cannot be updated. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
    private static partial void RegistrationsCannotBeUpdated(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Modules cannot be updated. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
    private static partial void ModulesCannotBeUpdated(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "A UIHandler is removed from the collection", SkipEnabledCheck = false)]
    public static partial void UiCommunicatorIsRemovedDebug(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Cannot modify subsystem's state with ID `{subsystemId}`. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
    private static partial void ModifyingSubsystemsState(this ILogger logger, string subsystemId, Exception ex, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error while terminating the process, ID: {pid}. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
    private static partial void CannotTerminateProcess(this ILogger logger, int pid, Exception ex, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error while terminating the process, ID: {pid}", SkipEnabledCheck = true)]
    public static partial void CannotTerminateProcessError(this ILogger logger, int pid);

    [LoggerMessage(Level = LogLevel.Error, Message = "Updating the information on UI cannot be completed. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
    private static partial void UiInformationCannotBeUpdated(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Cannot send launch request to the SubsystemLauncher. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
    private static partial void CannotSendLaunchRequest(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Cannot terminate a subsystem/subsystems. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
    private static partial void CannotTerminateSubsystem(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "The Process Explorer list is initialized", SkipEnabledCheck = false)]
    public static partial void ProcessListIsInitializedDebug(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "A process with PID: `{pid}` is terminated", SkipEnabledCheck = false)]
    public static partial void ProcessTerminatedInformation(this ILogger logger, int pid);

    [LoggerMessage(Level = LogLevel.Information, Message = "A process with PID: `{pid}` is created", SkipEnabledCheck = false)]
    public static partial void ProcessCreatedInformation(this ILogger logger, int pid);

    [LoggerMessage(Level = LogLevel.Debug, Message = "A process with PID: `{pid}` is modified", SkipEnabledCheck = false)]
    public static partial void ProcessModifiedDebug(this ILogger logger, int pid);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The process `{pid}` does not exist in the ProcessMonitor list", SkipEnabledCheck = false)]
    public static partial void ProcessNotFoundWarning(this ILogger logger, int pid);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The subsystem with `{id}` couldn't be terminated. Subsystem name: `{name}`", SkipEnabledCheck = false)]
    public static partial void SubsystemTerminationAddWarning(this ILogger logger, string id, string name);

    [LoggerMessage(Level = LogLevel.Error, Message = "Couldn't read the child process. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
    private static partial void ManagementObjectChild(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Cannot find the process. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
    private static partial void CannotFindProcess(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Couldn't read the event of the process. Probably the event is not set for this WMI handler or the ProcessMonitor is disabled. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
    private static partial void ManagementObjectWatchEvent(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Cannot find an element with PID `{pid}` in the current process list. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
    private static partial void CannotFindElement(this ILogger logger, int pid, Exception ex, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Cannot stop process watcher. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
    private static partial void CannotStopProcessWatcher(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Cannot find an element with PID `{pid}` or the communicator has been not set properly. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
    private static partial void CouldNotFoundModifiableProcess(this ILogger logger, int pid, Exception ex, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Nor PID does not exists: `{pid}` or the process list does not exists. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
    private static partial void PidNotExists(this ILogger logger, int pid, Exception ex, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Index does not exists for the PID: `{pid}` or the process list does not exists. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
    private static partial void IndexDoesNotExists(this ILogger logger, int pid, Exception ex, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Cannot terminate process or the process does not exists in the current context. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
    private static partial void CannotKillProcess(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error while initializing list. The main process might be deleted. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
    private static partial void CannotFillList(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error while starting a subsystem with id: {id}. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
    private static partial void SubsystemStartFailure(this ILogger logger, string id, Exception ex, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error while restarting a subsystem with id: {id}. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
    private static partial void SubsystemRestartFailure(this ILogger logger, string id, Exception ex, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error while restarting a subsystem with id: {id}.", SkipEnabledCheck = true)]
    private static partial void SubsystemRestartFailureWithoutException(this ILogger logger, string id);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error while stopping a subsystem with id: {id}. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
    private static partial void SubsystemStopFailure(this ILogger logger, string id, Exception ex, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error while adding a subsystem with id: {id}, name: `{name}`", SkipEnabledCheck = true)]
    private static partial void SubsystemAddFailure(this ILogger logger, string id, string name);

    public static void SubsystemAddError(this ILogger logger, string id, string name)
    {
        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.SubsystemAddFailure(id, name);
        }
    }

    public static void SubsystemStopError(this ILogger logger, string id, Exception exception)
    {
        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.SubsystemStopFailure(id, exception, exception);
        }
    }

    public static void SubsystemRestartError(this ILogger logger, string id, Exception exception)
    {
        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.SubsystemRestartFailure(id, exception, exception);
        }
    }

    public static void SubsystemRestartError(this ILogger logger, string id)
    {
        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.SubsystemRestartFailureWithoutException(id);
        }
    }

    public static void SubsystemStartError(this ILogger logger, string id, Exception exception)
    {
        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.SubsystemStartFailure(id, exception, exception);
        }
    }

    public static void ManagementObjectPpidExpected(this ILogger logger, int pid, Exception exception)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.ManagementObjectPpid(pid, exception, exception);
        }
    }

    public static void InstanceEventExpected(this ILogger logger, Exception exception)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.ManagementObjectWatchEvent(exception, exception);
        }
    }

    public static void ChildProcessExpected(this ILogger logger, Exception exception)
    {
        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.ManagementObjectChild(exception, exception);
        }
    }

    public static void ProcessExpected(this ILogger logger, Exception exception)
    {
        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.CannotFindProcess(exception, exception);
        }
    }

    public static void WatcherInitializationError(this ILogger logger, Exception exception)
    {
        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.ManagementObjectChild(exception, exception);
        }
    }

    public static void PpidExpected(this ILogger logger, int pid, Exception exception)
    {
        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.CannotFindElement(pid, exception, exception);
        }
    }

    public static void StoppingWatcherExpected(this ILogger logger, Exception exception)
    {
        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.CannotStopProcessWatcher(exception, exception);
        }
    }

    public static void ModifiableProcessExpected(this ILogger logger, int pid, Exception exception)
    {
        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.CouldNotFoundModifiableProcess(pid, exception, exception);
        }
    }

    public static void PidExpected(this ILogger logger, int pid, Exception exception)
    {
        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.PidNotExists(pid, exception, exception);
        }
    }

    public static void PidIndexExpected(this ILogger logger, int pid, Exception exception)
    {
        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.IndexDoesNotExists(pid, exception, exception);
        }
    }

    public static void KillProcessError(this ILogger logger, Exception exception)
    {
        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.CannotKillProcess(exception, exception);
        }
    }

    public static void ListInitializationError(this ILogger logger, Exception exception)
    {
        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.CannotFillList(exception, exception);
        }
    }

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
