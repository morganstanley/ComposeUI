/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using Microsoft.Extensions.Logging;

namespace ProcessExplorer.Processes.Logging
{
    internal static partial class SourceGeneratedLogMessages
    {
        // Disable the warning.
#pragma warning disable SYSLIB1006
        [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "The Process Explorer list is initialized", SkipEnabledCheck = true)]
        static partial void ProcessListIsInitialized(ILogger logger);

        [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "A process with PID: `{pid}` is terminated", SkipEnabledCheck = true)]
        static partial void ProcessTerminated(ILogger logger, int pid);

        [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "A process with PID: `{pid}` is created", SkipEnabledCheck = true)]
        static partial void ProcessCreated(ILogger logger, int pid);

        [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "A process with PID: `{pid}` is modified", SkipEnabledCheck = true)]
        static partial void ProcessModified(ILogger logger, int pid);

        [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "The process `{pid}` does not exist in the ProcessMonitor list", SkipEnabledCheck = true)]
        static partial void ProcessNotFound(ILogger logger, int pid);

        [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Couldn't find the PPID for the PID `{pid}`. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
        static partial void ManagementObjectPPID(ILogger logger, int pid, string exception);

        [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "ProcessMonitor UICommunicator is set.", SkipEnabledCheck = true)]
        static partial void ProcessMonitorCommunicatorIsSet(ILogger logger);

        [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Couldn't read the child process. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
        static partial void ManagementObjectChild(ILogger logger, string exception);

        [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Cannot find the process. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
        static partial void CannotFindProcess(ILogger logger, string exception);

        [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Couldn't watch the processes. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
        static partial void ManagementObjectWatch(ILogger logger, string exception);

        [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Cannot add the connections to collection. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
        static partial void ConnectionCollectionCannotBeAdded(ILogger logger, string exception);

        [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Connection cannot be updated. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
        static partial void ConnectionCannotBeUpdated(ILogger logger, string exception);

        [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Couldn't watch the processes. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
        static partial void EnvironmentVariablesCannotBeUpdated(ILogger logger, string exception);

        [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Registrations cannot be updated. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
        static partial void RegistrationsCannotBeUpdated(ILogger logger, string exception);

        [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Modules cannot be updated. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
        static partial void ModulesCannotBeUpdated(ILogger logger, string exception);

        [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Couldn't read the event of the process. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
        static partial void ManagementObjectWatchEvent(ILogger logger, string exception);

        [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "A UIHandler is removed from the collection", SkipEnabledCheck = true)]
        static partial void UICommunicatorIsRemoved(ILogger logger);

        [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Cannot find an element with PID `{pid}` in the current process list. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
        static partial void CannotFindElement(ILogger logger, int pid, string exception);

        [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Cannot find an element with PID `{pid}` or the communicator has been not set properly. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
        static partial void CouldNotFoundModifiableProcess(ILogger logger, int pid, string exception);

        [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Cannot convert object `{pid}` to Integer32. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
        static partial void CouldNotConvertToInt(ILogger logger, string pid, string exception);

        [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Nor PID does not exists: `{pid}` or the process list does not exists. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
        static partial void PIDNotExists(ILogger logger, int pid, string exception);

        [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Index does not exists for the PID: `{pid}` or the process list does not exists. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
        static partial void IndexDoesNotExists(ILogger logger, int pid, string exception);

        [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Cannot terminate process or the process does not exists in the current context. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
        static partial void CannotKillProcess(ILogger logger, string exception);

        [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Error while initializing list. The main process might be deleted. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
        static partial void CannotFillList(ILogger logger, string exception);

        [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Cannot add processthread to the list. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
        static partial void CannotAddProcessThread(ILogger logger, string exception);

        internal static void ManagementObjectPPID(this ILogger logger, int pid, Exception exception)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                ManagementObjectPPID(logger, pid, exception.Message);
            }
        }

        internal static void ManagementObjectWatchEventError(this ILogger logger, Exception exception)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                ManagementObjectWatchEvent(logger, exception.Message);
            }
        }

        internal static void ProcessListIsInitializedDebug(this ILogger logger)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                ProcessListIsInitialized(logger);
            }
        }

        internal static void ProcessCreatedInformation(this ILogger logger, int pid)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                ProcessCreated(logger, pid);
            }
        }

        internal static void ProcessModifiedDebug(this ILogger logger, int pid)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                ProcessModified(logger, pid);
            }
        }

        internal static void ManagementObjectChildError(this ILogger logger, Exception exception)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                ManagementObjectChild(logger, exception.Message);
            }
        }

        internal static void CannotFindProcessError(this ILogger logger, Exception exception)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                CannotFindProcess(logger, exception.Message);
            }
        }
        internal static void ManagementObjectWatchError(this ILogger logger, Exception exception)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                ManagementObjectChild(logger, exception.Message);
            }
        }

        internal static void ConnectionCollectionCannotBeAddedError(this ILogger logger, Exception exception)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                ConnectionCollectionCannotBeAdded(logger, exception.Message);
            }
        }

        internal static void ConnectionCannotBeUpdatedError(this ILogger logger, Exception exception)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                ConnectionCannotBeUpdated(logger, exception.Message);
            }
        }

        internal static void EnvironmentVariablesCannotBeUpdatedError(this ILogger logger, Exception exception)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                EnvironmentVariablesCannotBeUpdated(logger, exception.Message);
            }
        }

        internal static void RegistrationsCannotBeUpdatedError(this ILogger logger, Exception exception)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                RegistrationsCannotBeUpdated(logger, exception.Message);
            }
        }

        internal static void ModulesCannotBeUpdatedError(this ILogger logger, Exception exception)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                ModulesCannotBeUpdated(logger, exception.Message);
            }
        }

        internal static void CannotFindElementError(this ILogger logger, int pid, Exception exception)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                CannotFindElement(logger, pid, exception.Message);
            }
        }

        internal static void CouldNotFoundModifiableProcessError(this ILogger logger, int pid, Exception exception)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                CouldNotFoundModifiableProcess(logger, pid, exception.Message);
            }
        }

        internal static void CouldNotConvertToIntError(this ILogger logger, string pid, Exception exception)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                CouldNotConvertToInt(logger, pid, exception.Message);
            }
        }

        internal static void PIDNotExistsError(this ILogger logger, int pid, Exception exception)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                PIDNotExists(logger, pid, exception.Message);
            }
        }

        internal static void IndexDoesNotExistsError(this ILogger logger, int pid, Exception exception)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                IndexDoesNotExists(logger, pid, exception.Message);
            }
        }

        internal static void CannotKillProcessError(this ILogger logger, Exception exception)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                CannotKillProcess(logger, exception.Message);
            }
        }
        internal static void CannotFillListError(this ILogger logger, Exception exception)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                CannotFillList(logger, exception.Message);
            }
        }

        internal static void CannotAddProcessThreadError(this ILogger logger, Exception exception)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                CannotAddProcessThread(logger, exception.Message);
            }
        }

        internal static void ProcessTerminatedInformation(this ILogger logger, int pid)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                ProcessTerminated(logger, pid);
            }
        }

        internal static void UICommunicatorIsRemovedDebug(this ILogger logger)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                UICommunicatorIsRemoved(logger);
            }
        }

        internal static void ProcessNotFoundError(this ILogger logger, int pid)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                ProcessNotFound(logger, pid);
            }
        }

        internal static void ProcessCommunicatorIsSetDebug(this ILogger logger)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                ProcessMonitorCommunicatorIsSet(logger);
            }
        }
        // Re-enable the warning.
#pragma warning restore SYSLIB1006
    }
}
