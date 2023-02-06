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
using ProcessExplorer.Abstraction.Subsystems;
using ProcessExplorer.Server.Logging;

namespace ProcessExplorer.Server.Server.Infrastructure;

internal class SubsystemLauncherCommunicator : ISubsystemLauncherCommunicator
{
    private readonly ILogger<SubsystemLauncherCommunicator> _logger;
    private readonly ISubsystemLauncher _subsystemLauncher; // we should inject it from the shell where we control the applications states

    public SubsystemLauncherCommunicator(
        ILogger<SubsystemLauncherCommunicator> logger,
        ISubsystemLauncher subsystemLauncher)
    {
        _logger = logger ?? NullLogger<SubsystemLauncherCommunicator>.Instance;
        _subsystemLauncher = subsystemLauncher;
    }

    //it is not necessary in this case
    public Task InitializeCommunicationRoute()
    {
        return Task.CompletedTask;
    }

    public async Task SendLaunchSubsystemAfterTimeRequest(Guid subsystemId, int periodOfTime)
    {
        var result = await _subsystemLauncher.LaunchSubsystemAfterTime(subsystemId, periodOfTime);

        if (result == SubsystemState.Stopped) _logger.StartSubsystemError(subsystemId.ToString());
    }

    public async Task SendLaunchSubsystemsRequest(IEnumerable<Guid> subsystems)
    {
        var result = await _subsystemLauncher.LaunchSubsystems(subsystems);

        foreach (var subsystem in result)
        {
            if (subsystem.Value == SubsystemState.Stopped) _logger.StartSubsystemError(subsystem.Key.ToString());
        }
    }

    public async Task SendRestartSubsystemsRequest(IEnumerable<Guid> subsystems)
    {
        var result = await _subsystemLauncher.RestartSubsystems(subsystems);

        foreach (var subsystem in result)
        {
            if (subsystem.Value == SubsystemState.Stopped) _logger.RestartSubsystemError(subsystem.Key.ToString());
        }
    }

    public async Task SendShutdownSubsystemsRequest(IEnumerable<Guid> subsystems)
    {
        var result = await _subsystemLauncher.ShutdownSubsystems(subsystems);

        foreach (var subsystem in result)
        {
            if (subsystem.Value != SubsystemState.Stopped) _logger.ShutdownSubsystemError(subsystem.Key.ToString());
        }
    }
}
