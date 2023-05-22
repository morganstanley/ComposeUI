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
using ProcessExplorer.Abstractions;
using ProcessExplorer.Abstractions.Infrastructure.Protos;
using ProcessExplorer.Server.Logging;

namespace ProcessExplorer.Server.Server;

internal static class MessageHandler
{
    public static async void HandleIncomingGrpcMessages(
        Message message,
        IProcessInfoAggregator processInfoAggregator,
        CancellationToken cancellationToken,
        ILogger? logger = null)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var ids = message.Subsystems.Select(subsystem => subsystem.Key);

                switch (message.Action)
                {
                    case ActionType.TerminateSubsystemsAction:
                        await processInfoAggregator.SubsystemController.ShutdownSubsystems(ids);

                        break;

                    case ActionType.RestartSubsystemsAction:
                        await processInfoAggregator.SubsystemController.RestartSubsystems(ids);

                        break;

                    case ActionType.LaunchSubsystemsAction:
                        await processInfoAggregator.SubsystemController.LaunchSubsystems(ids);

                        break;

                    case ActionType.LaunchSubsystemsWithDelayAction:
                        try
                        {
                            if (ids.ElementAt(0) == null) continue;

                            var id = Guid.Parse(ids.ElementAt(0));
                            await processInfoAggregator.SubsystemController.LaunchSubsystemAfterTime(id, message.PeriodOfDelay);
                        }
                        catch(Exception exception)
                        {
                            logger?.GrpcMessageReadingError(exception, exception);
                        }

                        break;
                }

                if (cancellationToken.IsCancellationRequested) 
                    break;
            }
        }
        catch(Exception exception)
        {
            logger?.GrpcMessageHandlingError(exception, exception);
        }
    }
}
