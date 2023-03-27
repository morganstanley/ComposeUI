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
using Microsoft.Extensions.Logging.Abstractions;
using ModuleProcessMonitor.Subsystems;
using MorganStanley.ComposeUI.Messaging;
using MorganStanley.ComposeUI.Tryouts.Core.Abstractions.Modules;
using ProcessExplorerMessageRouterTopics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ModulesPrototype.Infrastructure;

internal class SubsystemLauncher : ISubsystemLauncher
{
    private readonly ILogger<SubsystemLauncher> _logger;
    private readonly Dictionary<Guid, SubsystemInfo> _subsystems;
    private readonly IModuleLoader _moduleLoader;
    private readonly IMessageRouter? _messageRouter;
    private readonly object _subsystemLocker = new();
    private delegate ValueTask<string> RequestSubsystemAction(Guid subsystemId);

    public SubsystemLauncher(ILogger<SubsystemLauncher>? logger,
        IMessageRouter? messageRouter,
        IModuleLoader moduleLoader)
    {
        _logger = logger ?? NullLogger<SubsystemLauncher>.Instance;
        _moduleLoader = moduleLoader;
        _messageRouter = messageRouter;
        _subsystems = new Dictionary<Guid, SubsystemInfo>();
    }

    public void SetSubsystems(string serializedSubsystems)
    {
        try
        {
            var subsystems = JsonSerializer.Deserialize<Dictionary<Guid, SubsystemInfo>>(serializedSubsystems);

            if (subsystems == null)
            {
                _logger.LogWarning($"Cannot initialize subsystems. Null argumentum: {nameof(subsystems)}.");
                return;
            }

            lock (_subsystemLocker)
            {
                if (subsystems.Any())
                {
                    foreach (var subsystem in subsystems)
                    {
                        _subsystems[subsystem.Key] = subsystem.Value;
                    }
                }
            }
        }
        catch (Exception exception)
        {
            _logger.LogError($@"Some error(s) occurred while initializing subsystems for Process Explorer backend. {exception}");
        }
    }

    public async ValueTask InitSubsystems()
    {
        if (_messageRouter == null)
        {
            throw new ArgumentNullException($"Argument : {nameof(_messageRouter)} is null.");
        }

        _logger.LogInformation($"Sending the collection of subsystems to Process Explorer to show on the UI.");

        try
        {
            if (_subsystems.Any())
            {
                var payload = JsonSerializer.Serialize(_subsystems);
                await _messageRouter.PublishAsync(Topics.initilaizingSubsystems, payload);
            }
        }
        catch (Exception exception)
        {
            _logger.LogInformation($"Some error(s) occurred while publishing to topic: {Topics.initilaizingSubsystems}. {exception}");
        }
    }

    public async ValueTask AddSubsystem(Guid subsystemId, SubsystemInfo subsystem)
    {
        if (_messageRouter == null)
        {
            throw new ArgumentNullException($"Argument : {nameof(_messageRouter)} is null.");
        }

        _logger.LogInformation($"Adding subsystem to the collection with ID : {subsystemId}.");

        var subsystemExists = false;

        lock (_subsystemLocker)
        {
            subsystemExists = _subsystems.Any(sub => sub.Key == subsystemId);
        }

        if (subsystemExists)
        {
            _moduleLoader.RequestStartProcess(new LaunchRequest { instanceId = subsystemId, name = subsystem.Name });

            try
            {
                var serializableSubsystem = new KeyValuePair<Guid, SubsystemInfo>(subsystemId, subsystem);
                var payload = JsonSerializer.Serialize(serializableSubsystem);
                await _messageRouter.PublishAsync(Topics.addingSubsystem, payload);
            }
            catch (Exception exception)
            {
                _logger.LogInformation($"Some error(s) occurred while publishing to topic: {Topics.addingSubsystem}. {exception}");
            }
        }
    }

    public async ValueTask ModifySubsystemState(Guid subsystemId, string state)
    {
        if (_messageRouter == null)
        {
            throw new ArgumentNullException($"Argument : {nameof(_messageRouter)} is null.");
        }

        _logger.LogInformation($"Modifying the state of subsystem with Id: {subsystemId} and to state: {state}.");

        try
        {
            var payload = JsonSerializer.Serialize(new KeyValuePair<Guid, string>(subsystemId, state));
            await _messageRouter.PublishAsync(Topics.modifyingSubsystem, payload);
        }
        catch (Exception exception)
        {
            _logger.LogInformation($"Some error(s) occurred while publishing to topic: {Topics.modifyingSubsystem}. {exception}");
        }
    }

    public async ValueTask RemoveSubsystem(Guid subsystemId)
    {
        if (_messageRouter == null)
        {
            throw new ArgumentNullException($"Argument : {nameof(_messageRouter)} is null.");
        }

        _moduleLoader.RequestStopProcess(new StopRequest { instanceId = subsystemId });

        try
        {
            var payload = JsonSerializer.Serialize(subsystemId);
            await _messageRouter.PublishAsync(Topics.removingSubsystem, payload);
        }
        catch (Exception exception)
        {
            _logger.LogInformation($"Some error(s) occurred while publishing to topic: {Topics.removingSubsystem}. {exception}");
        }
    }

    private async ValueTask<IEnumerable<KeyValuePair<Guid, string>>> HandleSubsystemAction(IEnumerable<Guid> subsystems, RequestSubsystemAction action)
    {
        var result = new Dictionary<Guid, string>();

        foreach (var subsystem in subsystems)
        {
            KeyValuePair<Guid, SubsystemInfo> existedSubsystem;

            lock (_subsystemLocker)
            {
                existedSubsystem = _subsystems
                    .FirstOrDefault(sub =>
                        sub.Key == subsystem);
            }

            var resultSubsystemState = await action(existedSubsystem.Key);

            result.Add(existedSubsystem.Key, resultSubsystemState);
        }

        return result;
    }

    public async ValueTask<string> LaunchSubsystem(Guid subsystemId)
    {
        try
        {
            KeyValuePair<Guid, SubsystemInfo> subsystem;

            lock (_subsystemLocker)
            {
                subsystem = _subsystems
                    .First(sub => sub.Key == subsystemId);
            }

            _logger.LogInformation($"Starting subsystem with Id: {subsystem.Key}.");

            if (subsystem.Value.State != SubsystemState.Started)
            {
                //TODO(Lilla): Send to the PE backend the given name in the manifest.
                _moduleLoader.RequestStartProcess(new LaunchRequest { instanceId = subsystem.Key, name = subsystem.Value.Name });
            }
            else
            {
                _logger.LogInformation($"Subsystem with id: {subsystem.Key} is already started.");
            }

            return SubsystemState.Started;
        }
        catch (Exception exception)
        {
            _logger.LogError($"Failed to launch subsystem with Id: {subsystemId}. {exception}");
        }

        return SubsystemState.Stopped;
    }

    public ValueTask<string> LaunchSubsystemAfterTime(Guid subsystemId, int periodOfTime)
    {
        Thread.Sleep(periodOfTime);
        return LaunchSubsystem(subsystemId);
    }

    public ValueTask<IEnumerable<KeyValuePair<Guid, string>>> LaunchSubsystems(IEnumerable<Guid> subsystems)
    {
        return HandleSubsystemAction(subsystems, LaunchSubsystem);
    }

    public async ValueTask<string> RestartSubsystem(Guid subsystemId)
    {
        //Sending stop and launch request
        await ShutdownSubsystem(subsystemId);

        var startedStateResult = await LaunchSubsystem(subsystemId);
        if (startedStateResult == SubsystemState.Stopped)
        {
            _logger.LogError(
                $"FAILED to launch subsystem with Id: {subsystemId} after stopping it... (Restart failed)..");

        }

        return startedStateResult;
    }

    public ValueTask<IEnumerable<KeyValuePair<Guid, string>>> RestartSubsystems(IEnumerable<Guid> subsystems)
    {
        return HandleSubsystemAction(subsystems, RestartSubsystem);
    }

    public async ValueTask<string> ShutdownSubsystem(Guid subsystemId)
    {
        try
        {
            KeyValuePair<Guid, SubsystemInfo> subsystem;
            lock (_subsystemLocker)
            {
                subsystem = _subsystems.First(sub => sub.Key == subsystemId);
            }

            _logger.LogInformation($"Stopping subsystem with Id: {subsystem.Key}.");

            if (subsystem.Value.State != SubsystemState.Stopped)
            {
                _moduleLoader.RequestStopProcess(new StopRequest { instanceId = subsystem.Key });
            }
            else
            {
                _logger.LogInformation($"Subsystem with id: {subsystem.Key} is already stopped.");
            }

            return SubsystemState.Stopped;
        }
        catch (Exception exception)
        {
            _logger.LogError($"Failed to stop subsystem with Id: {subsystemId}. {exception}");
            throw new Exception($"Some error(s) occurred while shutdowning the subsystem with Id :{subsystemId}, {exception}.");
        }
    }

    public ValueTask<IEnumerable<KeyValuePair<Guid, string>>> ShutdownSubsystems(IEnumerable<Guid> subsystems)
    {
        return HandleSubsystemAction(subsystems, ShutdownSubsystem);
    }
}
