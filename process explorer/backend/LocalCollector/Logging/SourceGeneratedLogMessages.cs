﻿/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using Microsoft.Extensions.Logging;

namespace ProcessExplorer.LocalCollector.Logging
{
    internal static partial class SourceGeneratedLogMessages
    {
        [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Cannot send connection status changed message to the server. Detailed exception: `{exception}`", SkipEnabledCheck = true)]
        static partial void ConnectionStatusSendError(ILogger logger, string exception);

        internal static void ConnectionStatusChangedError(this ILogger logger, Exception exception)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                ConnectionStatusSendError(logger, exception.Message);
            }
        }

    }
}
