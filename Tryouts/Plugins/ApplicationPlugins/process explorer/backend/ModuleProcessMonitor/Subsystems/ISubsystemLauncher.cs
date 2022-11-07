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

namespace ModuleProcessMonitor.Subsystems;

public interface ISubsystemLauncher
{
    /// <summary>
    /// Sets the available manifests, which are initialized through ModuleLoader.
    /// </summary>
    /// <param name="serializedSubsystems"></param>
    void SetSubsystems(string serializedSubsystems);

    /// <summary>
    /// Initializes all the subsystems, which are communicated via the selected communicated route.
    /// </summary>
    /// <returns></returns>
    ValueTask InitSubsystems();

    /// <summary>
    /// Adds a subsystem to the subsystemController's list.
    /// </summary>
    /// <param name="subsystemId"></param>
    /// <param name="subsystem"></param>
    /// <returns></returns>
    ValueTask AddSubsystem(Guid subsystemId, SubsystemInfo subsystem);

    /// <summary>
    /// Modifies a state of a subsystem in the subsystemController's list.
    /// </summary>
    /// <param name="subsystemId"></param>
    /// <param name="state"></param>
    /// <returns></returns>
    ValueTask ModifySubsystemState(Guid subsystemId, string state);

    /// <summary>
    /// Removes a subsystem from the subsystemController's list.
    /// </summary>
    /// <param name="subsystemId"></param>
    /// <returns></returns>
    ValueTask RemoveSubsystem(Guid subsystemId);

    /// <summary>
    /// Sends launch command to the subsystem module via the selected communication route.
    /// </summary>
    /// <param name="subsystemId"></param>
    /// <returns></returns>
    ValueTask<string> LaunchSubsystem(Guid subsystemId);

    /// <summary>
    /// Sends launch command (after a timeperiod) to the subsystem module via the selected communication route.
    /// </summary>
    /// <param name="subsystemId"></param>
    /// <param name="periodOfTime"></param>
    /// <returns></returns>
    ValueTask<string> LaunchSubsystemAfterTime(Guid subsystemId, int periodOfTime);

    /// <summary>
    /// Sends launch command to the subsystem module via the selected communication route.
    /// </summary>
    /// <param name="subsystems"></param>
    /// <returns></returns>
    ValueTask<IEnumerable<KeyValuePair<Guid, string>>> LaunchSubsystems(IEnumerable<Guid> subsystems);

    /// <summary>
    /// Sends restart command to the subsystem module via the selected communication route.
    /// </summary>
    /// <param name="subsystemId"></param>
    /// <returns></returns>
    ValueTask<string> RestartSubsystem(Guid subsystemId);

    /// <summary>
    /// Sends restart command to the subsystem module via the selected communication route.
    /// </summary>
    /// <param name="subsystems"></param>
    /// <returns></returns>
    ValueTask<IEnumerable<KeyValuePair<Guid, string>>> RestartSubsystems(IEnumerable<Guid> subsystems);

    /// <summary>
    /// Sends shutdown command to the subsystem module via the selected communication route.
    /// </summary>
    /// <param name="subsystemId"></param>
    /// <returns></returns>
    ValueTask<string> ShutdownSubsystem(Guid subsystemId);

    /// <summary>
    /// Sends shutdown command to the subsystem module via the selected communication route.
    /// </summary>
    /// <param name="subsystems"></param>
    /// <returns></returns>
    ValueTask<IEnumerable<KeyValuePair<Guid, string>>> ShutdownSubsystems(IEnumerable<Guid> subsystems);
}
