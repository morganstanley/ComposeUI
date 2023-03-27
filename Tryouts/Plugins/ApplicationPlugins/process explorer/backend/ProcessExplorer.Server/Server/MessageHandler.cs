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
using ProcessExplorer.Server.Logging;
using ProcessExplorer.Server.Server.Abstractions;
using ProcessExplorer.Server.Server.Infrastructure.Protos;
using ProcessExplorer.Server.Server.Infrastructure.WebSocket;

namespace ProcessExplorer.Server.Server;

internal static class MessageHandler
{
    public static async void HandleIncomingWebSocketMessages(
        IWebSocketConnection connection,
        IProcessInfoAggregator processInfoAggregator,
        CancellationTokenSource cancellationTokenSource,
        CancellationToken cancellationToken,
        ILogger? logger = null)
    {
        try
        {
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                var message = await connection.ReceiveAsync(cancellationToken);

                logger?.WebSocketClientMessageReceivedDebug(message.Topic ?? string.Empty);

                //Here we need to handle just the requests from the UI and also if the process directly stopped?
                switch (message.Topic)
                {
                    case Topics.RestartingSubsystems:
                        await processInfoAggregator.RestartSubsystems(message.Payload as IEnumerable<string> ?? Enumerable.Empty<string>());

                        break;

                    case Topics.TerminatingSubsystems:
                        await processInfoAggregator.ShutdownSubsystems(message.Payload as IEnumerable<string> ?? Enumerable.Empty<string>());

                        break;

                    case Topics.LaunchingSubsystems:
                        await processInfoAggregator.LaunchSubsystems(message.Payload as IEnumerable<string> ?? Enumerable.Empty<string>());

                        break;

                    case Topics.LaunchingSubsystemWithDelay:
                        if (message.Payload == null) break;
                        var kvp = (KeyValuePair<Guid, int>)message.Payload;
                        
                        await processInfoAggregator.LaunchSubsystemWithDelay(kvp.Key, kvp.Value);

                        break;
                }
            }
        }
        catch (Exception exception)
        {
            logger?.WebSocketMessageHandlingError(exception, exception);
        }
    }

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
                        await processInfoAggregator.ShutdownSubsystems(ids);

                        break;

                    case ActionType.RestartSubsystemsAction:
                        await processInfoAggregator.RestartSubsystems(ids);

                        break;

                    case ActionType.LaunchSubsystemsAction:
                        await processInfoAggregator.LaunchSubsystems(ids);

                        break;

                    case ActionType.LaunchSubsystemsWithDelayAction:
                        try
                        {
                            if (ids.ElementAt(0) == null) continue;

                            var id = Guid.Parse(ids.ElementAt(0));
                            await processInfoAggregator.LaunchSubsystemWithDelay(id, message.PeriodOfDelay);
                        }
                        catch(Exception exception)
                        {
                            logger?.GrpcMessageReadingError(exception, exception);
                        }

                        break;
                }
            }
        }
        catch(Exception exception)
        {
            logger?.GrpcMessageHandlingError(exception, exception);
        }
    }
}
