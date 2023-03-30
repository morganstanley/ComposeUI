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

namespace ProcessExplorer.Abstractions.Subsystems;

public interface ISubsystemLauncher
{
    /// <summary>
    /// Sends launch command to the subsystem module via the selected communication route.
    /// </summary>
    /// <param name="subsystemId"></param>
    /// <param name="subsystemName"></param>
    /// <returns></returns>
    Task<string> LaunchSubsystem(Guid subsystemId, string subsystemName);

    /// <summary>
    /// Sends launch command (after a timeperiod) to the subsystem module via the selected communication route.
    /// </summary>
    /// <param name="subsystemId"></param>
    /// <param name="subsystemName"></param>
    /// <param name="periodOfTime"></param>
    /// <returns></returns>
    Task<string> LaunchSubsystemAfterTime(Guid subsystemId, string subsystemName, int periodOfTime);

    /// <summary>
    /// Sends launch command to the subsystem module via the selected communication route.
    /// </summary>
    /// <param name="subsystems"></param>
    /// <returns></returns>
    Task<IEnumerable<KeyValuePair<Guid, string>>> LaunchSubsystems(IEnumerable<KeyValuePair<Guid, string>> subsystems);

    /// <summary>
    /// Sends restart command to the subsystem module via the selected communication route.
    /// </summary>
    /// <param name="subsystemId"></param>
    /// <param name="subsystemName"></param>
    /// <returns></returns>
    Task<string> RestartSubsystem(Guid subsystemId, string subsystemName);

    /// <summary>
    /// Sends restart command to the subsystem module via the selected communication route.
    /// </summary>
    /// <param name="subsystems"></param>
    /// <returns></returns>
    Task<IEnumerable<KeyValuePair<Guid, string>>> RestartSubsystems(IEnumerable<KeyValuePair<Guid, string>> subsystems);

    /// <summary>
    /// Sends shutdown command to the subsystem module via the selected communication route.
    /// </summary>
    /// <param name="subsystemId"></param>
    /// <param name="subsystemName"></param>
    /// <returns></returns>
    Task<string> ShutdownSubsystem(Guid subsystemId, string subsystemName);

    /// <summary>
    /// Sends shutdown command to the subsystem module via the selected communication route.
    /// </summary>
    /// <param name="subsystems"></param>
    /// <returns></returns>
    Task<IEnumerable<KeyValuePair<Guid, string>>> ShutdownSubsystems(IEnumerable<KeyValuePair<Guid, string>> subsystems);
}
