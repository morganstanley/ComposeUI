/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using Microsoft.Extensions.Logging;

namespace ProcessExplorer.Processes.Logging
{
    internal static partial class SourceGeneratedLogMessages
    {
        // Disable the warning.
#pragma warning disable SYSLIB1006
        [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "The Process Explorer list is initalized", SkipEnabledCheck = true)]
        static partial void ProcessListIsInitalizedInformation(ILogger logger);

        [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "A process with PID: `{pid}` is terminated", SkipEnabledCheck = true)]
        static partial void ProcessTerminatedInformation(ILogger logger, int pid);

        [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "A process with PID: `{pid}` is created", SkipEnabledCheck = true)]
        static partial void ProcessCreatedInformation(ILogger logger, int pid);

        [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "A process with PID: `{pid}` is modified", SkipEnabledCheck = true)]
        static partial void ProcessModifiedDebug(ILogger logger, int pid);

        [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "The process `{pid}` is not exist in the ProcessMonitor list", SkipEnabledCheck = true)]
        static partial void ProcessNotFoundWarning(ILogger logger, int pid);

        [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "A process is renamed. Old value: `{oldValue}`, new value: {newValue}", SkipEnabledCheck = true)]
        static partial void ProcessRenamedWarning(ILogger logger, string oldValue, string newValue);

        [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Couldn't find the PPID for the PID `{pid}`. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
        static partial void ManagementObjectPPIDError(ILogger logger, int pid, string exception);

        [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "ProcessMonitor UICommunicator is set.", SkipEnabledCheck = true)]
        static partial void ProcessMonitorCommunicatorIsSet(ILogger logger);

        [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Couldn't read the child process. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
        static partial void ManagementObjectChildError(ILogger logger, string exception);

        [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Couldn't watch the processes. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
        static partial void ManagementObjectWatchError(ILogger logger, string exception);

        [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Cannot add the connections to collection. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
        static partial void ConnectionCollectionCannotBeAddedError(ILogger logger, string exception);

        [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Conenction cannot be updated. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
        static partial void ConnectionCannotBeUpdatedError(ILogger logger, string exception);

        [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Couldn't watch the processes. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
        static partial void EnvironmentVariablesCannotBeUpdatedError(ILogger logger, string exception);

        [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Registrations cannot be updated. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
        static partial void RegistrationsCannotBeUpdatedError(ILogger logger, string exception);

        [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Modules cannot be updated. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
        static partial void ModulesCannotBeUpdatedError(ILogger logger, string exception);

        [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Couldn't read the event of the process. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
        static partial void ManagementObjectWatchEventError(ILogger logger, string exception);

        [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Cannot find an element with PID `{pid}` in the current process list. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
        static partial void CannotFindElementError(ILogger logger, int pid, string exception);

        [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Cannot find an element with PID `{pid}` or the communicator has benn not set properly. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
        static partial void CouldNotFoundModifiableProcessError(ILogger logger, int pid, string exception);

        [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Cannot convert object `{pid}` to Integer32. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
        static partial void CouldNotConvertToIntError(ILogger logger, string pid, string exception);

        [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Nor PID is not exists: `{pid}` or the process list is not exists. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
        static partial void PIDNotExistsError(ILogger logger, int pid, string exception);

        [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Index does not exists for the PID: `{pid}` or the process list is not exists. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
        static partial void IndexDoesNotExistsError(ILogger logger, int pid, string exception);

        [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Cannot terminate process or the process is not exists in the current context. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
        static partial void CannotKillProcessError(ILogger logger, string exception);

        [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Detailed exception: `{exception}`, StackTrace: `{stackTrace}`", SkipEnabledCheck = true)]
        static partial void LinuxWatcherError(ILogger logger, string exception, string stackTrace);

        [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Error while initializing list. The main process might be deleted. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
        static partial void CannotFillListError(ILogger logger, string exception);

        [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Cannot add processthread to the list. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
        static partial void CannotAddProccessThreadError(ILogger logger, string exception);

        internal static void ManagementObjectPPIDError(this ILogger logger, int pid, Exception exception)
        {
            if (logger is not null && logger.IsEnabled(LogLevel.Debug))
            {
                ManagementObjectPPIDError(logger, pid, exception.Message);
            }
        }

        internal static void ManagementObjectWatchEventError(this ILogger logger, Exception exception)
        {
            if (logger is not null && logger.IsEnabled(LogLevel.Debug))
            {
                ManagementObjectWatchEventError(logger, exception.Message);
            }
        }

        internal static void ProcessListIsInitalized(this ILogger logger)
        {
            if (logger is not null && logger.IsEnabled(LogLevel.Debug))
            {
                ProcessListIsInitalizedInformation(logger);
            }
        }

        internal static void ProcessCreated(this ILogger logger, int pid)
        {
            if (logger is not null && logger.IsEnabled(LogLevel.Information))
            {
                ProcessCreatedInformation(logger, pid);
            }
        }

        internal static void ProcessModified(this ILogger logger, int pid)
        {
            if (logger is not null && logger.IsEnabled(LogLevel.Debug))
            {
                ProcessModifiedDebug(logger, pid);
            }
        }

        internal static void ManagementObjectChildError(this ILogger logger, Exception exception)
        {
            if (logger is not null && logger.IsEnabled(LogLevel.Error))
            {
                ManagementObjectChildError(logger, exception.Message);
            }
        }

        internal static void ManagementObjectWatchError(this ILogger logger, Exception exception)
        {
            if (logger is not null && logger.IsEnabled(LogLevel.Error))
            {
                ManagementObjectChildError(logger, exception.Message);
            }
        }

        internal static void ConnectionCollectionCannotBeAdded(this ILogger logger, Exception exception)
        {
            if (logger is not null && logger.IsEnabled(LogLevel.Error))
            {
                ConnectionCollectionCannotBeAddedError(logger, exception.Message);
            }
        }

        internal static void ConnectionCannotBeUpdated(this ILogger logger, Exception exception)
        {
            if (logger is not null && logger.IsEnabled(LogLevel.Error))
            {
                ConnectionCannotBeUpdatedError(logger, exception.Message);
            }
        }

        internal static void EnvironmentVariablesCannotBeUpdated(this ILogger logger, Exception exception)
        {
            if (logger is not null && logger.IsEnabled(LogLevel.Error))
            {
                EnvironmentVariablesCannotBeUpdatedError(logger, exception.Message);
            }
        }

        internal static void RegistrationsCannotBeUpdated(this ILogger logger, Exception exception)
        {
            if (logger is not null && logger.IsEnabled(LogLevel.Error))
            {
                RegistrationsCannotBeUpdatedError(logger, exception.Message);
            }
        }

        internal static void ModulesCannotBeUpdated(this ILogger logger, Exception exception)
        {
            if (logger is not null && logger.IsEnabled(LogLevel.Error))
            {
                ModulesCannotBeUpdatedError(logger, exception.Message);
            }
        }

        internal static void CannotFindElement(this ILogger logger, int pid, Exception exception)
        {
            if (logger is not null && logger.IsEnabled(LogLevel.Error))
            {
                CannotFindElementError(logger, pid, exception.Message);
            }
        }

        internal static void CouldNotFoundModifiableProcess(this ILogger logger, int pid, Exception exception)
        {
            if (logger is not null && logger.IsEnabled(LogLevel.Error))
            {
                CouldNotFoundModifiableProcessError(logger, pid, exception.Message);
            }
        }

        internal static void CouldNotConvertToInt(this ILogger logger, string pid, Exception exception)
        {
            if (logger is not null && logger.IsEnabled(LogLevel.Error))
            {
                CouldNotConvertToIntError(logger, pid, exception.Message);
            }
        }

        internal static void NotExists(this ILogger logger, int pid, Exception exception)
        {
            if (logger is not null && logger.IsEnabled(LogLevel.Error))
            {
                PIDNotExistsError(logger, pid, exception.Message);
            }
        }

        internal static void IndexDoesNotExists(this ILogger logger, int pid, Exception exception)
        {
            if (logger is not null && logger.IsEnabled(LogLevel.Error))
            {
                IndexDoesNotExistsError(logger, pid, exception.Message);
            }
        }

        internal static void CannotKillProcess(this ILogger logger, Exception exception)
        {
            if (logger is not null && logger.IsEnabled(LogLevel.Error))
            {
                CannotKillProcessError(logger, exception.Message);
            }
        }

        internal static void CannotSetWatcherLinux(this ILogger logger, Exception exception)
        {
            if (logger is not null && logger.IsEnabled(LogLevel.Error))
            {
                LinuxWatcherError(logger, exception.Message, exception.StackTrace is null ? "" : exception.StackTrace);
            }
        }

        internal static void CannotFillList(this ILogger logger, Exception exception)
        {
            if (logger is not null && logger.IsEnabled(LogLevel.Error))
            {
                CannotFillListError(logger, exception.Message);
            }
        }

        internal static void CannotAddProcessThread(this ILogger logger, Exception exception)
        {
            if (logger is not null && logger.IsEnabled(LogLevel.Error))
            {
                CannotAddProccessThreadError(logger, exception.Message);
            }
        }

        internal static void ProcessTerminated(this ILogger logger, int pid)
        {
            if (logger is not null && logger.IsEnabled(LogLevel.Information))
            {
                ProcessTerminatedInformation(logger, pid);
            }
        }

        internal static void ProcessNotFound(this ILogger logger, int pid)
        {
            if (logger is not null && logger.IsEnabled(LogLevel.Warning))
            {
                ProcessNotFoundWarning(logger, pid);
            }
        }

        internal static void ProcessRenamed(this ILogger logger, string oldValue, string newValue)
        {
            if (logger is not null && logger.IsEnabled(LogLevel.Warning))
            {
                ProcessRenamedWarning(logger, oldValue, newValue);
            }
        }

        internal static void ProcessCommunicatorIsSet(this ILogger logger)
        {
            if (logger is not null && logger.IsEnabled(LogLevel.Debug))
            {
                ProcessCommunicatorIsSet(logger);
            }
        }
        // Re-enable the warning.
#pragma warning restore SYSLIB1006
    }
}
