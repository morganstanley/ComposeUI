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

namespace ModuleProcessMonitor.Logging;

internal static partial class SourceGeneratedLoggerExtensions
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "The Process Explorer list is initialized", SkipEnabledCheck = false)]
    public static partial void ProcessListIsInitializedDebug(this ILogger logger);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "A process with PID: `{pid}` is terminated", SkipEnabledCheck = false)]
    public static partial void ProcessTerminatedInformation(this ILogger logger, int pid);

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "A process with PID: `{pid}` is created", SkipEnabledCheck = false)]
    public static partial void ProcessCreatedInformation(this ILogger logger, int pid);

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "A process with PID: `{pid}` is modified", SkipEnabledCheck = false)]
    public static partial void ProcessModifiedDebug(this ILogger logger, int pid);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "The process `{pid}` does not exist in the ProcessMonitor list", SkipEnabledCheck = false)]
    public static partial void ProcessNotFoundWarning(this ILogger logger, int pid);

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Couldn't find the PPID for the PID `{pid}`. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
    private static partial void ManagementObjectPpid(this ILogger logger, int pid, Exception ex, Exception exception);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Couldn't read the child process. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
    private static partial void ManagementObjectChild(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Cannot find the process. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
    private static partial void CannotFindProcess(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Couldn't read the event of the process. Probably the event is not set for this WMI handler or the ProcessMonitor is disabled. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
    private static partial void ManagementObjectWatchEvent(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Cannot find an element with PID `{pid}` in the current process list. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
    private static partial void CannotFindElement(this ILogger logger, int pid, Exception ex, Exception exception);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Cannot stop process watcher. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
    private static partial void CannotStopProcessWatcher(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Cannot find an element with PID `{pid}` or the communicator has been not set properly. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
    private static partial void CouldNotFoundModifiableProcess(this ILogger logger, int pid, Exception ex, Exception exception);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Nor PID does not exists: `{pid}` or the process list does not exists. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
    private static partial void PidNotExists(this ILogger logger, int pid, Exception ex, Exception exception);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Index does not exists for the PID: `{pid}` or the process list does not exists. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
    private static partial void IndexDoesNotExists(this ILogger logger, int pid, Exception ex, Exception exception);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Cannot terminate process or the process does not exists in the current context. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
    private static partial void CannotKillProcess(this ILogger logger, Exception ex, Exception exception);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Error while initializing list. The main process might be deleted. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
    private static partial void CannotFillList(this ILogger logger, Exception ex, Exception exception);

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
}

