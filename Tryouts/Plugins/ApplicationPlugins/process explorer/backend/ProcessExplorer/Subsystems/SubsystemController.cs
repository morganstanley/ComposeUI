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

using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ModuleProcessMonitor.Subsystems;
using ProcessExplorer.Infrastructure;
using ProcessExplorer.Logging;

namespace ProcessExplorer.Subsystems;

/// <summary>
/// Gets the messages from the UI and sends command to the subsystemHandler (proxy object)
/// </summary>
internal class SubsystemController : ISubsystemController
{
    private ObservableCollection<KeyValuePair<Guid, SubsystemInfo>> _subsystems = new();
    private readonly object _subsystemLock = new();
    private readonly ILogger<ISubsystemController> _logger;
    public ISubsystemLauncherCommunicator SubsystemLauncherCommunicator { get; }

    public SubsystemController(ISubsystemLauncherCommunicator subsystemLauncherCommunicator,
        ILogger<ISubsystemController>? logger)
    {
        SubsystemLauncherCommunicator = subsystemLauncherCommunicator;
        _logger = logger ?? NullLogger<ISubsystemController>.Instance;
    }

    public async Task InitializeSubsystems(IEnumerable<KeyValuePair<Guid, SubsystemInfo>> subsystems)
    {
        UpdateOrAddElements(subsystems);

        SetCollectionChangedEvent();

        await LaunchSubsystemsAutomatically();
        await SendInitializedSubsystemInfoToUis();
    }

    private void SetCollectionChangedEvent()
    {
        lock (_subsystemLock)
        {
            _subsystems.CollectionChanged += async (sender, args) =>
            {
                List<KeyValuePair<Guid, SubsystemInfo>> subsystemsCopy;
                lock (_subsystemLock)
                {
                    subsystemsCopy = _subsystems.ToList();
                }

                await UpdateInfoOnUI(handler => handler.AddSubsystems(subsystemsCopy));
            };
        }
    }

    public async Task LaunchAllRegisteredSubsystem()
    {
        try
        {
            var guids = Enumerable.Empty<Guid>();

            lock (_subsystemLock)
            {
                if (_subsystems.Any())
                {
                    //Must have key
                    guids = _subsystems
                        .Select(subsystem => subsystem.Key);
                }

                if (!guids.Any()) return;
            }

            await SubsystemLauncherCommunicator.SendLaunchSubsystemsRequest(guids);
        }
        catch (Exception exception)
        {
            _logger.CannotSendLaunchRequestError(exception);
        }
    }

    public async Task LaunchSubsystemAfterTime(Guid subsystemId, int periodOfTime)
    {
        await SubsystemLauncherCommunicator.SendLaunchSubsystemAfterTimeRequest(subsystemId, periodOfTime);
    }

    public async Task LaunchSubsystemAutomatically(Guid subsystemId)
    {
        var subsystem = GetSubsystem(subsystemId);

        if (subsystem != null)
        {
            await LaunchSubsystem(subsystem.Value.Key.ToString());
            subsystem.Value.Value.AutomatedStart = true;
        }
    }

    public async Task LaunchSubsystemsAutomatically()
    {
        var subsystemIds = new SynchronizedCollection<string>();
        lock (_subsystemLock)
        {
            if (_subsystems.Any())
            {
                foreach (var subsystem in _subsystems)
                {
                    if (subsystem.Value.AutomatedStart == true && subsystem.Value.State == SubsystemState.Stopped)
                    {
                        subsystemIds.Add(subsystem.Key.ToString());
                    }
                }
            }
        }

        await LaunchSubsystems(subsystemIds);
    }

    public async Task LaunchSubsystems(IEnumerable<string> subsystems)
    {
        try
        {
            if (subsystems.Any())
            {
                var subsystemIds = subsystems
                    .Select(id => new Guid(id));

                await SubsystemLauncherCommunicator.SendLaunchSubsystemsRequest(subsystemIds);
            }
        }
        catch (Exception exception)
        {
            _logger.CannotSendLaunchRequestError(exception);
        }
    }

    public async Task LaunchSubsystem(string subsystemId)
    {
        await SubsystemLauncherCommunicator.SendLaunchSubsystemsRequest(new List<Guid> { new(subsystemId) });
    }

    public async Task RestartSubsystems(IEnumerable<string> subsystems)
    {
        foreach (var subsystemId in subsystems)
        {
            var registeredSubsystem = GetSubsystem(new Guid(subsystemId));

            if (registeredSubsystem == null)
            {
                continue;
            }

            await SubsystemLauncherCommunicator.SendRestartSubsystemsRequest(new List<Guid> { new(subsystemId) });
        }
    }

    public async Task RestartSubsystem(string subsystemId)
    {
        await SubsystemLauncherCommunicator.SendRestartSubsystemsRequest(new List<Guid> { new(subsystemId) });
    }

    public async Task ShutdownAllRegisteredSubsystem()
    {
        try
        {
            IEnumerable<KeyValuePair<Guid, SubsystemInfo>> copySubsystems;

            lock (_subsystemLock)
            {
                copySubsystems = _subsystems.ToList();
            }

            if (copySubsystems.Any())
            {
                await SubsystemLauncherCommunicator
                    .SendShutdownSubsystemsRequest(copySubsystems
                        .Select(element => element.Key));
            }
        }
        catch (Exception exception)
        {
            _logger.CannotTerminateSubsystemError(exception);
        }
    }

    public async Task ShutdownSubsystem(string subsystemId)
    {
        await SubsystemLauncherCommunicator.SendShutdownSubsystemsRequest(new List<Guid>() { new Guid(subsystemId) });
    }

    public async Task ShutdownSubsystems(IEnumerable<string> subsystems)
    {
        try
        {
            var subsystemIds = subsystems
                .Select(id => new Guid(id));

            await SubsystemLauncherCommunicator.SendShutdownSubsystemsRequest(subsystemIds);
        }
        catch (Exception exception)
        {
            _logger.CannotTerminateSubsystemError(exception);
        }
    }


    public async Task ModifySubsystemState(Guid subsystemId, string state)
    {
        KeyValuePair<Guid, SubsystemInfo> subsystem;

        lock (_subsystemLock)
        {
            subsystem = _subsystems
                .FirstOrDefault(sub =>
                    sub.Key.Equals(subsystemId));

            try
            {

                subsystem.Value.State = state;

            }
            catch (Exception exception)
            {
                _logger.ModifyingSubsystemsStateError(subsystemId, exception);
            }
        }

        await UpdateInfoOnUI(handler => handler.UpdateSubsystemInfo(subsystemId, subsystem.Value));
    }

    public async Task AddUIConnection(IUIHandler uiHandler)
    {
        UiClientsStore.AddUiConnection(uiHandler);
        await SendInitializedSubsystemInfoToUis();
    }

    public Task RemoveUIConnection(IUIHandler uiHandler)
    {
        return Task.Factory.StartNew(() =>
        {
            UiClientsStore.RemoveUiConnection(uiHandler);
        });
    }

    public async Task AddSubsystems(IEnumerable<KeyValuePair<Guid, SubsystemInfo>> subsystems)
    {
        UpdateOrAddElements(subsystems);
        await UpdateInfoOnUI(handler => handler.AddSubsystems(subsystems));
    }

    public void AddSubsystem(Guid subsystemId, SubsystemInfo subsystem)
    {
        lock (_subsystemLock)
        {
            _subsystems.Add(new(subsystemId, subsystem));
        }
    }

    public void RemoveSubsystem(Guid subsystemId)
    {
        var subsystem = GetSubsystem(subsystemId);

        lock (_subsystemLock)
        {
            if (subsystem == null ||
                !_subsystems.Contains(subsystem.Value))
            {
                return;
            }

            var subsystems = _subsystems
                .Where(subsystemKeyValuePair =>
                    subsystemKeyValuePair.Key != subsystemId);

            _subsystems = new ObservableCollection<KeyValuePair<Guid, SubsystemInfo>>(subsystems);
            SetCollectionChangedEvent();
        }
    }

    private KeyValuePair<Guid, SubsystemInfo>? GetSubsystem(Guid subsystemId)
    {
        KeyValuePair<Guid, SubsystemInfo> subsystem;

        lock (_subsystemLock)
        {
            subsystem = _subsystems
                .FirstOrDefault(s =>
                    s.Key == subsystemId);
        }

        return subsystem;
    }

    private async Task SendInitializedSubsystemInfoToUis()
    {
        var handlers = new List<IUIHandler>();

        lock (UiClientsStore._uiClientLocker)
        {
            if (UiClientsStore._uiClients.Count > 0)
            {
                handlers.AddRange(UiClientsStore._uiClients);
            }
        }

        if (handlers.Count > 0)
        {
            await SendInfoToInitializedUIs(handlers);
        }
    }

    private async Task SendInfoToInitializedUIs(List<IUIHandler> handlers)
    {
        var subsystemCopy = new Dictionary<Guid, SubsystemInfo>();

        lock (_subsystemLock)
        {
            if (_subsystems.Any())
            {
                foreach (var subsystem in _subsystems)
                {
                    subsystemCopy
                        .Add(subsystem.Key, subsystem.Value);
                }
            }
        }

        if (subsystemCopy.Count > 0)
        {
            foreach (var uiHandler in handlers)
            {
                await uiHandler.AddSubsystems(subsystemCopy);
            }
        }
    }

    private async Task SetState(Guid subsystemId, string state)
    {
        var subsystem = GetSubsystem(subsystemId);

        if (subsystem == null) return;

        lock (_subsystemLock)
        {
            if (subsystem.Value.Value.State != state)
            {
                subsystem.Value.Value.State = state;
            }
        }

        await UpdateInfoOnUI(handler => handler.UpdateSubsystemInfo(subsystemId, subsystem.Value.Value));
    }

    private async Task SetStates(IEnumerable<KeyValuePair<Guid, string>> states)
    {
        foreach (var state in states)
        {
            await SetState(state.Key, state.Value);
        }
    }

    private Task UpdateInfoOnUI(Func<IUIHandler, Task> handlerAction)
    {
        try
        {
            return Task.WhenAll(CreateCopyOfClients().Select(handlerAction));
        }
        catch (Exception exception)
        {
            _logger.UiInformationCannotBeUpdatedError(exception);
            return Task.CompletedTask;
        }
    }

    private IEnumerable<IUIHandler> CreateCopyOfClients()
    {
        lock (UiClientsStore._uiClientLocker)
        {
            return UiClientsStore._uiClients.ToArray();
        }
    }

    private void UpdateOrAddElements(IEnumerable<KeyValuePair<Guid, SubsystemInfo>> subsystems)
    {
        lock (_subsystemLock)
        {
            foreach (var subsystem in subsystems)
            {
                var element = _subsystems
                    .FirstOrDefault(sub =>
                        subsystem.Key == sub.Key);

                if (element.Value != null)
                {
                    element.Value.Name = subsystem.Value.Name;
                    element.Value.Path = subsystem.Value.Path;
                    element.Value.State = subsystem.Value.State;
                    element.Value.Description = subsystem.Value.Description;
                    element.Value.AutomatedStart = subsystem.Value.AutomatedStart;
                }
                else
                {
                    _subsystems.Add(subsystem);
                }
            }
        }
    }
}
