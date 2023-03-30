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
using Microsoft.Extensions.Options;
using ProcessExplorer.Abstractions.Logging;
using ProcessExplorer.Abstractions.Subsystems;
using ProcessExplorer.Core.DependencyInjection;

namespace ProcessExplorer.Core.Subsystems;

internal class SubsystemLauncher<LaunchRequestType, StopRequestType> : ISubsystemLauncher
{
    private delegate Task<string> RequestSubsystemAction(Guid subsystemId, string subsystemName);
    private readonly ILogger _logger;
    private Action<LaunchRequestType>? _launchRequest;
    private Action<StopRequestType>? _stopRequest;
    private Func<Guid, string, LaunchRequestType>? _createLaunchRequest;
    private Func<Guid, StopRequestType>? _createStopRequest;

    public SubsystemLauncher(
        ILogger? logger = null,
        IOptions<SubsystemLauncherOptions<LaunchRequestType, StopRequestType>>? options = null)
    {
        _logger = logger ?? NullLogger.Instance;
        _launchRequest = options?.Value.LaunchRequest;
        _stopRequest= options?.Value.StopRequest;
        _createLaunchRequest = options?.Value.CreateLaunchRequest;
        _createStopRequest = options?.Value.CreateStopRequest;
    }

    public void SetHandlers(
        Action<LaunchRequestType> launchRequest,
        Action<StopRequestType> stopRequest,
        Func<Guid, string, LaunchRequestType> launchRequestCreator,
        Func<Guid, StopRequestType> stopRequestCreator)
    {
        _launchRequest = launchRequest;
        _stopRequest = stopRequest;
        _createLaunchRequest = launchRequestCreator;
        _createStopRequest = stopRequestCreator;
    }

    public Task<string> LaunchSubsystem(Guid subsystemId, string subsystemName)
    {
        try
        {

            if (_launchRequest == null || _createLaunchRequest == null) return Task.FromResult(SubsystemState.Stopped);

            _launchRequest.Invoke(_createLaunchRequest.Invoke(subsystemId, subsystemName));
            _logger.SubsystemStartedDebug(subsystemId.ToString());

            return Task.FromResult(SubsystemState.Started);
        }
        catch (Exception exception)
        {
            _logger.SubsystemStartError(subsystemId.ToString(), exception);
        }

        return Task.FromResult(SubsystemState.Stopped);
    }

    public Task<string> LaunchSubsystemAfterTime(Guid subsystemId, string subsystemName, int periodOfTime)
    {
        Thread.Sleep(periodOfTime);
        return LaunchSubsystem(subsystemId, subsystemName);
    }

    public Task<IEnumerable<KeyValuePair<Guid, string>>> LaunchSubsystems(IEnumerable<KeyValuePair<Guid, string>> subsystems)
    {
        return HandleSubsystemAction(subsystems, LaunchSubsystem);
    }

    public async Task<string> RestartSubsystem(Guid subsystemId, string subsystemName)
    {
        await ShutdownSubsystem(subsystemId, subsystemName);

        var startedStateResult = await LaunchSubsystem(subsystemId, subsystemName);
        if (startedStateResult == SubsystemState.Stopped)
        {
            _logger.SubsystemRestartError(subsystemId.ToString());
        }

        return startedStateResult;
    }

    public Task<IEnumerable<KeyValuePair<Guid, string>>> RestartSubsystems(IEnumerable<KeyValuePair<Guid, string>> subsystems)
    {
        return HandleSubsystemAction(subsystems, RestartSubsystem);
    }

    public Task<string> ShutdownSubsystem(Guid subsystemId, string subsystemName)
    {
        try
        {
            _logger.SubsystemStoppingDebug(subsystemId.ToString());

            if (_stopRequest == null || _createStopRequest == null) return Task.FromResult(SubsystemState.Running);

            _stopRequest.Invoke(_createStopRequest.Invoke(subsystemId));

            return Task.FromResult(SubsystemState.Stopped);
        }
        catch (Exception exception)
        {
            _logger.SubsystemStopError(subsystemId.ToString(), exception);
            return Task.FromResult(SubsystemState.Running);
        }
    }

    public Task<IEnumerable<KeyValuePair<Guid, string>>> ShutdownSubsystems(IEnumerable<KeyValuePair<Guid, string>> subsystems)
    {
        return HandleSubsystemAction(subsystems, ShutdownSubsystem);
    }

    private async Task<IEnumerable<KeyValuePair<Guid, string>>> HandleSubsystemAction(IEnumerable<KeyValuePair<Guid, string>> subsystems, RequestSubsystemAction action)
    {
        var result = new Dictionary<Guid, string>();

        foreach (var subsystem in subsystems)
        {
            var resultSubsystemState = await action(subsystem.Key, subsystem.Value);

            result.Add(subsystem.Key, resultSubsystemState);
        }

        return result;
    }
}
