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
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions;
using MorganStanley.ComposeUI.ProcessExplorer.Server.Extensions;
using MorganStanley.ComposeUI.ProcessExplorer.Server.Logging;
using ProcessExplorer.Abstractions.Infrastructure.Protos;

namespace MorganStanley.ComposeUI.ProcessExplorer.Server.Server;

internal static class MessageHandler
{
    public static async void HandleIncomingGrpcMessages(
        Message message,
        IProcessInfoAggregator processInfoAggregator,
        ILogger? logger = null)
    {
        try
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
                        if (ids.ElementAt(0) == null) break;

                        var id = Guid.Parse(ids.ElementAt(0)); // or foreach? 
                        await processInfoAggregator.SubsystemController.LaunchSubsystemAfterTime(id, message.PeriodOfDelay);
                    }
                    catch (Exception exception)
                    {
                        logger?.GrpcMessageReadingError(exception, exception);
                    }

                    break;

                case ActionType.AddRuntimeInfoAction:
                    await processInfoAggregator.AddRuntimeInformation(
                        message.AssemblyId,
                        message.RuntimeInfo.DeriveProcessInfoCollectorData());

                    break;

                case ActionType.AddConnectionListAction:
                    await processInfoAggregator.AddConnectionCollection(
                        message.AssemblyId,
                        message.Connections.Select(connection => connection.DeriveConnectionInfo()));

                    break;

                case ActionType.UpdateConnectionAction:
                    await processInfoAggregator.UpdateOrAddConnectionInfo(
                        message.AssemblyId,
                        message.Connections.First().DeriveConnectionInfo());

                    break;

                case ActionType.UpdateEnvironmentVariablesAction:
                    await processInfoAggregator.UpdateOrAddEnvironmentVariablesInfo(
                        message.AssemblyId,
                        message.EnvironmentVariables);

                    break;

                case ActionType.UpdateRegistrationsAction:
                    await processInfoAggregator.UpdateRegistrations(
                        message.AssemblyId,
                        message.Registrations.Select(registration => registration.DeriveRegistration()));

                    break;

                case ActionType.UpdateModulesAction:
                    await processInfoAggregator.UpdateOrAddModuleInfo(
                        message.AssemblyId,
                        message.Modules.Select(module => module.DeriveModule()));

                    break;

                case ActionType.UpdateConnectionStatusAction:
                    await processInfoAggregator.UpdateConnectionStatus(
                        message.AssemblyId,
                        message.ConnectionStatusChanges.First().Key,
                        message.ConnectionStatusChanges.First().Value);

                    break;
            }
        }
        catch (Exception exception)
        {
            logger?.GrpcMessageHandlingError(exception, exception);
        }
    }
}
