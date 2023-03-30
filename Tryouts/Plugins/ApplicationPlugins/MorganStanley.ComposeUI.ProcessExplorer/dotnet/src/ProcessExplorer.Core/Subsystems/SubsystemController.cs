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

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ProcessExplorer.Abstractions.Infrastructure;
using ProcessExplorer.Abstractions.Logging;
using ProcessExplorer.Abstractions.Subsystems;

namespace ProcessExplorer.Core.Subsystems;

/// <summary>
/// Gets the messages from the UI and sends command to the subsystemHandler (proxy object)
/// </summary>
internal class SubsystemController : ISubsystemController
{
    private ConcurrentDictionary<Guid, SubsystemInfo> _subsystems = new();
    private Func<Func<IUIHandler, Task>, Task>? _updateInfoOnUi;
    private readonly object _subsystemLock = new();
    private readonly ILogger _logger;

    //We are allowing that to users to create their own backend server or their own SubsystemLauncher, without depending on certain types and without circular dependency.
    //We are using the _subsystemLauncher in the Server project.
    private readonly ISubsystemLauncherCommunicator? _subsystemLauncherCommunicator;
    private readonly ISubsystemLauncher? _subsystemLauncher;

    public SubsystemController(
        ISubsystemLauncher subsystemLauncher,
        ILogger? logger)
    {
        _subsystemLauncher = subsystemLauncher;
        _logger = logger ?? NullLogger<SubsystemController>.Instance;
    }

    public SubsystemController(
        ISubsystemLauncherCommunicator subsystemLauncherCommunicator,
        ILogger? logger = null)
    {
        _subsystemLauncherCommunicator = subsystemLauncherCommunicator;
        _logger = logger ?? NullLogger<SubsystemController>.Instance;
    }

    public async Task InitializeSubsystems(IEnumerable<KeyValuePair<Guid, SubsystemInfo>> subsystems)
    {
        UpdateOrAddElements(subsystems);
        await SendModifiedSubsystems();
        await LaunchSubsystemsAutomatically();
    }

    public async Task LaunchAllRegisteredSubsystem()
    {
        try
        {
            var subsystemIds = Enumerable.Empty<KeyValuePair<Guid, string>>();

            lock (_subsystemLock)
            {
                if (_subsystems.Any())
                {
                    subsystemIds = _subsystems
                        .Select(subsystem => new KeyValuePair<Guid, string>(subsystem.Key, subsystem.Value.Name));
                }

                if (!subsystemIds.Any()) return;
            }

            await SendRequest(
                _subsystemLauncher.LaunchSubsystems(subsystemIds),
                _subsystemLauncherCommunicator.SendLaunchSubsystemsRequest(subsystemIds));

        }
        catch (Exception exception)
        {
            _logger.CannotSendLaunchRequestError(exception);
        }
    }

    public async Task LaunchSubsystemAfterTime(Guid subsystemId, int periodOfTime)
    {
        SubsystemInfo? subsystem;

        lock (_subsystemLock)
            if(!_subsystems.TryGetValue(subsystemId, out subsystem)) return;

        if (subsystem == null) return;

        await SendRequest(
            _subsystemLauncherCommunicator.SendLaunchSubsystemAfterTimeRequest(subsystemId, subsystem.Name, periodOfTime),
            _subsystemLauncher.LaunchSubsystemAfterTime(subsystemId, subsystem.Name, periodOfTime));
    }

    private Task SendRequest(
        Task communicatorTask,
        Task launcherTask)
    {
        if (_subsystemLauncherCommunicator == null && _subsystemLauncher == null) return Task.CompletedTask;

        return _subsystemLauncherCommunicator == null ?
            launcherTask
            : communicatorTask;
    }

    public async Task LaunchSubsystemAutomatically(Guid subsystemId)
    {
        lock (_subsystemLock)
        {
            var succeed = _subsystems.TryGetValue(subsystemId, out var subsystem);
            if (!succeed || subsystem == null) return;
            subsystem.AutomatedStart = true;
        }

        await LaunchSubsystem(subsystemId.ToString());
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
            var ids = new Dictionary<Guid, string>();

            if (!subsystems.Any()) return;

            lock (_subsystemLock)
            {
                foreach (var id in subsystems)
                {
                    var guidId = Guid.Parse(id);
                    if(!_subsystems.TryGetValue(guidId, out var subsystem)) continue;
                    ids.Add(guidId, subsystem.Name);
                }
            }

            await SendRequest(
                    _subsystemLauncherCommunicator.SendLaunchSubsystemsRequest(ids),
                    _subsystemLauncher.LaunchSubsystems(ids));
        }
        catch (Exception exception)
        {
            _logger.CannotSendLaunchRequestError(exception);
        }
    }

    private KeyValuePair<Guid, string?> GetSubsystem(string subsystemId)
    {
        SubsystemInfo? subsystem;
        var guidId = Guid.Parse(subsystemId);

        lock (_subsystemLock)
        {
            if (!_subsystems.TryGetValue(guidId, out subsystem)) return default;
        }

        return new(guidId, subsystem.Name);
    }

    public async Task LaunchSubsystem(string subsystemId)
    {
        var subsystem = GetSubsystem(subsystemId);
        if(subsystem.Value == null) return;

        await SendRequest(
            _subsystemLauncherCommunicator.SendLaunchSubsystemsRequest(new Dictionary<Guid, string> { { subsystem.Key, subsystem.Value } }),
            _subsystemLauncher.LaunchSubsystem(subsystem.Key, subsystem.Value));
    }

    public async Task RestartSubsystems(IEnumerable<string> subsystems)
    {
        foreach (var subsystemId in subsystems)
        {
            var subsystem = GetSubsystem(subsystemId);
            if (subsystem.Value == null) return;

            await SendRequest(
                _subsystemLauncherCommunicator.SendRestartSubsystemsRequest(new Dictionary<Guid, string> { { subsystem.Key, subsystem.Value } }),
                _subsystemLauncher.RestartSubsystem(subsystem.Key, subsystem.Value));
        }
    }

    public async Task RestartSubsystem(string subsystemId)
    {
        var subsystem = GetSubsystem(subsystemId);
        if (subsystem.Value == null) return;

        await SendRequest(
            _subsystemLauncherCommunicator.SendRestartSubsystemsRequest(new Dictionary<Guid, string> { { subsystem.Key, subsystem.Value } }),
            _subsystemLauncher.RestartSubsystem(subsystem.Key, subsystem.Value));
    }

    public async Task ShutdownAllRegisteredSubsystem()
    {
        try
        {
            var copySubsystems = GetCopySubsystems();
            
            if (copySubsystems.Any())
            {
                await SendRequest(
                    _subsystemLauncherCommunicator.SendShutdownSubsystemsRequest(copySubsystems),
                    _subsystemLauncher.ShutdownSubsystems(copySubsystems));
            }
        }
        catch (Exception exception)
        {
            _logger.CannotTerminateSubsystemError(exception);
        }
    }

    private IEnumerable<KeyValuePair<Guid, string>> GetCopySubsystems()
    {
        IEnumerable<KeyValuePair<Guid, string>> copySubsystems = Enumerable.Empty<KeyValuePair<Guid, string>>();

        lock (_subsystemLock)
        {
            copySubsystems = _subsystems.Select(subsystem => new KeyValuePair<Guid, string>(subsystem.Key, subsystem.Value.Name));
        }

        return copySubsystems;
    }

    public async Task ShutdownSubsystem(string subsystemId)
    {
        var subsystem = GetSubsystem(subsystemId);
        if (subsystem.Value == null) return;

        await SendRequest(
            _subsystemLauncherCommunicator.SendShutdownSubsystemsRequest(new Dictionary<Guid, string>() { { subsystem.Key, subsystem.Value } }),
            _subsystemLauncher.ShutdownSubsystem(subsystem.Key, subsystem.Value));
    }

    public async Task ShutdownSubsystems(IEnumerable<string> subsystems)
    {
        try
        {
            IEnumerable<KeyValuePair<Guid, string>> copySubsystems;

            lock (_subsystemLock)
            {
                copySubsystems = _subsystems.Select(subsystem => new KeyValuePair<Guid, string>(subsystem.Key, subsystem.Value.Name));
            }

            await SendRequest(
                _subsystemLauncherCommunicator.SendShutdownSubsystemsRequest(copySubsystems),
                _subsystemLauncher.ShutdownSubsystems(copySubsystems));
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
            subsystem = _subsystems.FirstOrDefault(sub => sub.Key.Equals(subsystemId));

            if (subsystem.Value == null || subsystem.Value.State == state) return;

            try
            {
                subsystem.Value.State = state;
            }
            catch (Exception exception)
            {
                _logger.ModifyingSubsystemsStateError(subsystemId, exception);
            }
        }

        if (_updateInfoOnUi == null) return;
        await _updateInfoOnUi.Invoke(handler => handler.UpdateSubsystemInfo(subsystemId, subsystem.Value));
    }

    public async Task AddSubsystems(IEnumerable<KeyValuePair<Guid, SubsystemInfo>> subsystems)
    {
        UpdateOrAddElements(subsystems);
        if (_updateInfoOnUi == null) return;
        await _updateInfoOnUi.Invoke(handler => handler.AddSubsystems(subsystems));
    }

    public void AddSubsystem(Guid subsystemId, SubsystemInfo subsystem)
    {
        lock (_subsystemLock)
        {
            var succeed = _subsystems.TryAdd(subsystemId, subsystem);
            if (!succeed) _logger.SubsystemAddError(subsystemId.ToString(), subsystem.Name);
        }
    }

    public void RemoveSubsystem(Guid subsystemId)
    {
        lock (_subsystemLock)
        {
            var succeed = _subsystems.TryRemove(subsystemId, out _);
            if (!succeed) return;
            Task.Run(() => SendModifiedSubsystems());
        }
    }

    private async Task SendModifiedSubsystems()
    {
        List<KeyValuePair<Guid, SubsystemInfo>> subsystemsCopy;
        lock (_subsystemLock)
        {
            subsystemsCopy = _subsystems.ToList();
        }

        if (_updateInfoOnUi == null) return;
        await _updateInfoOnUi.Invoke(handler => handler.AddSubsystems(subsystemsCopy));
    }


    public Task SendInitializedSubsystemInfoToUis(ReadOnlySpan<IUIHandler> handlers)
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
                return uiHandler.AddSubsystems(subsystemCopy);
            }
        }

        return Task.CompletedTask;
    }

    private async Task SetState(Guid subsystemId, string state)
    {
        SubsystemInfo subsystem;

        lock (_subsystemLock)
        {
            if (!_subsystems.ContainsKey(subsystemId)) return;
            _subsystems[subsystemId].State = state;
            subsystem = _subsystems[subsystemId];
        }

        if (_updateInfoOnUi == null || subsystem == null) return;
        await _updateInfoOnUi.Invoke(handler => handler.UpdateSubsystemInfo(subsystemId, subsystem));
    }

    private async Task SetStates(IEnumerable<KeyValuePair<Guid, string>> states)
    {
        foreach (var state in states)
        {
            await SetState(state.Key, state.Value);
        }
    }

    private void UpdateOrAddElements(IEnumerable<KeyValuePair<Guid, SubsystemInfo>> subsystems)
    {
        lock (_subsystemLock)
        {
            foreach (var subsystem in subsystems)
            {
                _subsystems.AddOrUpdate(subsystem.Key, subsystem.Value, (key, value) => value = subsystem.Value);
            }
        }
    }

    public void SetUiDelegate(Func<Func<IUIHandler, Task>, Task> updateInfoOnUI)
    {
        _updateInfoOnUi = updateInfoOnUI;
    }

    public IEnumerable<KeyValuePair<Guid, SubsystemInfo>> GetSubsystems()
    {
        lock (_subsystemLock)
            return _subsystems;
    }
}
