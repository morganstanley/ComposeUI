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
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Infrastructure;
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Logging;
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Subsystems;

namespace MorganStanley.ComposeUI.ProcessExplorer.Core.Subsystems;

/// <summary>
/// Gets the messages from the UI and sends command to the subsystemHandler (proxy object)
/// </summary>
internal class SubsystemController : ISubsystemController
{
    private readonly ConcurrentDictionary<Guid, SubsystemInfo> _subsystems = new();
    private readonly object _subsystemLock = new();
    private readonly ILogger _logger;

    //We are allowing that to users to create their own backend server or their own SubsystemLauncher, without depending on certain types and without circular dependency.
    //We are using the _subsystemLauncher in the Server project.
    private readonly ISubsystemLauncherCommunicator? _subsystemLauncherCommunicator;
    private readonly IUiHandler _handler;
    private readonly ISubsystemLauncher? _subsystemLauncher;

    public SubsystemController(IUiHandler handler, ILogger? logger = null)
    {
        _handler = handler;
        _logger = logger ?? NullLogger<SubsystemController>.Instance;
    }

    public SubsystemController(
        ISubsystemLauncher subsystemLauncher,
        IUiHandler uiHandler,
        ILogger? logger = null)
        :this(uiHandler, logger)
    {
        _subsystemLauncher = subsystemLauncher;
    }

    public SubsystemController(
        ISubsystemLauncherCommunicator subsystemLauncherCommunicator,
        IUiHandler uiHandler,
        ILogger? logger = null)
        :this(uiHandler, logger)
    {
        _subsystemLauncherCommunicator = subsystemLauncherCommunicator;
    }

    public async Task InitializeSubsystems(IEnumerable<KeyValuePair<Guid, SubsystemInfo>> subsystems)
    {
        if (subsystems == null || !subsystems.Any()) return;

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
                communicatorTask: _subsystemLauncherCommunicator?.SendLaunchSubsystemsRequest(subsystemIds),
                launcherTask: _subsystemLauncher?.LaunchSubsystems(subsystemIds));

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
            communicatorTask: _subsystemLauncherCommunicator?.SendLaunchSubsystemAfterTimeRequest(subsystemId, subsystem.Name, periodOfTime),
            launcherTask: _subsystemLauncher?.LaunchSubsystemAfterTime(subsystemId, subsystem.Name, periodOfTime));
    }

    private Task SendRequest(
        Task? communicatorTask,
        Task? launcherTask)
    {
        if (_subsystemLauncherCommunicator == null && _subsystemLauncher == null) return Task.CompletedTask;

        return _subsystemLauncherCommunicator == null
            ? launcherTask
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
            var subsystemIds = new Dictionary<Guid, string>();

            if (subsystems == null || !subsystems.Any()) return;

            lock (_subsystemLock)
            {
                foreach (var id in subsystems)
                {
                    var subsystemState = GetSubsystemState(id);

                    if (subsystemState != SubsystemState.Stopped) continue;

                    var guidId = Guid.Parse(id);

                    if(!_subsystems.TryGetValue(guidId, out var subsystem)) continue;

                    subsystemIds.Add(guidId, subsystem.Name);
                }
            }

            await SendRequest(
                    communicatorTask: _subsystemLauncherCommunicator?.SendLaunchSubsystemsRequest(subsystemIds),
                    launcherTask: _subsystemLauncher?.LaunchSubsystems(subsystemIds));
        }
        catch (Exception exception)
        {
            _logger.CannotSendLaunchRequestError(exception);
        }
    }

    private KeyValuePair<Guid, string?> GetSubsystemIdAndName(string subsystemId)
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
        var subsystemState = GetSubsystemState(subsystemId);

        if (subsystemState != SubsystemState.Stopped) return;

        var subsystem = GetSubsystemIdAndName(subsystemId);
        if(subsystem.Value == null) return;

        await SendRequest(
            communicatorTask: _subsystemLauncherCommunicator?.SendLaunchSubsystemsRequest(new Dictionary<Guid, string> { { subsystem.Key, subsystem.Value } }),
            launcherTask: _subsystemLauncher?.LaunchSubsystem(subsystem.Key, subsystem.Value));
    }

    public async Task RestartSubsystems(IEnumerable<string> subsystems)
    {
        if (subsystems == null || !subsystems.Any()) return;

        var subsystemIds = new Dictionary<Guid, string>();
        foreach (var subsystemId in subsystems)
        {
            var subsystemState = GetSubsystemState(subsystemId);

            if (subsystemState != SubsystemState.Started && subsystemState != SubsystemState.Running) continue;

            var subsystem = GetSubsystemIdAndName(subsystemId);

            subsystemIds.TryAdd(subsystem.Key, subsystem.Value!);
        }

        await SendRequest(
            communicatorTask: _subsystemLauncherCommunicator?.SendRestartSubsystemsRequest( subsystemIds ),
            launcherTask: _subsystemLauncher?.RestartSubsystems(subsystemIds));
    }

    public async Task RestartSubsystem(string subsystemId)
    {
        var subsystemState = GetSubsystemState(subsystemId);

        if (subsystemState != SubsystemState.Started && subsystemState != SubsystemState.Running) return;

        var subsystem = GetSubsystemIdAndName(subsystemId);
        if (subsystem.Value == null) return;

        await SendRequest(
            communicatorTask: _subsystemLauncherCommunicator?.SendRestartSubsystemsRequest(new Dictionary<Guid, string> { { subsystem.Key, subsystem.Value } }),
            launcherTask: _subsystemLauncher?.RestartSubsystem(subsystem.Key, subsystem.Value));
    }

    public async Task ShutdownAllRegisteredSubsystem()
    {
        try
        {
            var copySubsystems = Enumerable.Empty<KeyValuePair<Guid, string>>();

            lock (_subsystemLock)
            {
                copySubsystems = _subsystems
                        .Where(subsystem => subsystem.Value.State == SubsystemState.Started || subsystem.Value.State == SubsystemState.Running)
                        .Select(subsystem => new KeyValuePair<Guid, string>(subsystem.Key, subsystem.Value.Name));
            }
            
            if (copySubsystems.Any())
            {
                await SendRequest(
                    communicatorTask: _subsystemLauncherCommunicator?.SendShutdownSubsystemsRequest(copySubsystems),
                    launcherTask: _subsystemLauncher?.ShutdownSubsystems(copySubsystems));
            }
        }
        catch (Exception exception)
        {
            _logger.CannotTerminateSubsystemError(exception);
        }
    }

    private IEnumerable<KeyValuePair<Guid, string>> GetCopySubsystems()
    {
        var copySubsystems = Enumerable.Empty<KeyValuePair<Guid, string>>();

        lock (_subsystemLock)
        {
            copySubsystems = _subsystems.Select(subsystem => new KeyValuePair<Guid, string>(subsystem.Key, subsystem.Value.Name));
        }

        return copySubsystems;
    }

    public async Task ShutdownSubsystem(string subsystemId)
    {
        var subsystemState = GetSubsystemState(subsystemId);

        if (subsystemState != SubsystemState.Started && subsystemState != SubsystemState.Running) return;
        
        var subsystem = GetSubsystemIdAndName(subsystemId);
        if (subsystem.Value == null) return;

        await SendRequest(
            communicatorTask: _subsystemLauncherCommunicator?.SendShutdownSubsystemsRequest(new Dictionary<Guid, string>() { { subsystem.Key, subsystem.Value } }),
            launcherTask: _subsystemLauncher?.ShutdownSubsystem(subsystem.Key, subsystem.Value));
    }

    private string GetSubsystemState(string subsystemId)
    {
        var result = string.Empty;

        lock (_subsystemLock)
        {
            if (_subsystems.Any())
                result = _subsystems
                    .First(subsystem => subsystem.Key == Guid.Parse(subsystemId))
                    .Value
                    .State;
        }

        return result;
    }

    public async Task ShutdownSubsystems(IEnumerable<string> subsystems)
    {
        try
        {
            var copySubsystems = GetCopySubsystems();

            var subsystemsToTerminate = new Dictionary<Guid, string>();

            foreach (var subsystemId in subsystems)
            {
                var subsystemKvp = copySubsystems
                    .First(sub => sub.Key == Guid.Parse(subsystemId));

                if (!subsystemsToTerminate.TryAdd(subsystemKvp.Key, subsystemKvp.Value)) _logger.SubsystemTerminationAddWarning(subsystemKvp.Key.ToString(), subsystemKvp.Value);
            }

            await SendRequest(
                communicatorTask: _subsystemLauncherCommunicator?.SendShutdownSubsystemsRequest(copySubsystems),
                launcherTask: _subsystemLauncher?.ShutdownSubsystems(copySubsystems));
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

        await _handler.UpdateSubsystemInfo(subsystemId, subsystem.Value);
    }

    public async Task AddSubsystems(IEnumerable<KeyValuePair<Guid, SubsystemInfo>> subsystems)
    {
        if (subsystems == null || !subsystems.Any()) return;

        UpdateOrAddElements(subsystems);

        foreach (var subsystem in subsystems)
        {
            if (subsystem.Value.AutomatedStart) await Task.Run(async () => await LaunchSubsystem(subsystem.Key.ToString()));
        }

        await _handler.AddSubsystems(subsystems);
    }

    //TODO(Lilla): Missing test
    public void AddSubsystem(Guid subsystemId, SubsystemInfo subsystem)
    {
        lock (_subsystemLock)
        {
            var succeed = _subsystems.TryAdd(subsystemId, subsystem);
            if (!succeed) _logger.SubsystemAddError(subsystemId.ToString(), subsystem.Name);
        }
    }

    public async Task RemoveSubsystem(Guid subsystemId)
    {
        var subsystem = _subsystems.First(subsystem => subsystem.Key == subsystemId);

        if (subsystem.Key == default) return;

        if (subsystem.Value.State == SubsystemState.Started || subsystem.Value.State == SubsystemState.Running)
            await ShutdownSubsystem(subsystemId.ToString());

        lock (_subsystemLock)
        {
            var succeed = _subsystems.TryRemove(subsystemId, out _);

            if (!succeed) return;

            Task.Run(async () => await SendModifiedSubsystems());
        }
    }

    private async Task SendModifiedSubsystems()
    {
        var subsystemsCopy = Enumerable.Empty<KeyValuePair<Guid, SubsystemInfo>>();
        lock (_subsystemLock)
        {
            subsystemsCopy = _subsystems.ToList();
        }

        await _handler.AddSubsystems(subsystemsCopy);
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

    public IEnumerable<KeyValuePair<Guid, SubsystemInfo>> GetSubsystems()
    {
        lock (_subsystemLock)
            return _subsystems;
    }
}
