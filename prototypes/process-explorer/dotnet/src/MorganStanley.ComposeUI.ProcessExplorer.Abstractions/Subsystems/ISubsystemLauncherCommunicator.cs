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

namespace MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Subsystems;

public interface ISubsystemLauncherCommunicator
{
    /// <summary>
    /// Initializes the route of the communication defined by the user.
    /// </summary>
    /// <returns></returns>
    Task InitializeCommunicationRoute();

    /// <summary>
    /// Sends a launch request to the launcher, which will launch the subsystem after the given timeperiod.
    /// </summary>
    /// <param name="subsystemId"></param>
    /// <param name="subsystemName"></param>
    /// <param name="periodOfTime"></param>
    /// <returns>A state of the sent subsystem, regarding the success of the launch.</returns>
    Task SendLaunchSubsystemAfterTimeRequest(Guid subsystemId, string subsystemName, int periodOfTime);

    /// <summary>
    /// Sends launch requests to the launcher.
    /// </summary>
    /// <param name="subsystems"></param>
    /// <returns>A list of the states of the sent subsystems, regarding the success of the launches.</returns>
    Task SendLaunchSubsystemsRequest(IEnumerable<KeyValuePair<Guid, string>> subsystems);

    /// <summary>
    /// Sends restart requests to the launcher.
    /// </summary>
    /// <param name="subsystems"></param>
    /// <returns>A list of the states of the sent subsystems, regarding the success of the restarts.</returns>
    Task SendRestartSubsystemsRequest(IEnumerable<KeyValuePair<Guid, string>> subsystems);

    /// <summary>
    /// Sends shutdown requests to the launcher.
    /// </summary>
    /// <param name="subsystems"></param>
    /// <returns>A list of the states of the sent subsystems, regarding the success of the shutdowns.</returns>
    Task SendShutdownSubsystemsRequest(IEnumerable<KeyValuePair<Guid, string>> subsystems);
}
