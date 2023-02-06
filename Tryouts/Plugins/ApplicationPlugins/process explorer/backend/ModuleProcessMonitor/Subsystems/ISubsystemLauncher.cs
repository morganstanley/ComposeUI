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

namespace ProcessExplorer.Abstraction.Subsystems;

public interface ISubsystemLauncher
{
    /// <summary>
    /// Sets the available subsystems, which are initialized through ModuleLoader.
    /// </summary>
    /// <param name="serializedSubsystems"></param>
    void SetSubsystems(string serializedSubsystems);

    /// <summary>
    /// Initializes all the subsystems, which are communicated via the selected communicated route.
    /// </summary>
    /// <returns></returns>
    Task InitSubsystems();

    ///// <summary>
    ///// Adds a subsystem to the subsystemController's list.
    ///// </summary>
    ///// <param name="subsystemId"></param>
    ///// <param name="subsystem"></param>
    ///// <returns></returns>
    //Task AddSubsystem(Guid subsystemId, SubsystemInfo subsystem);

    /// <summary>
    /// Modifies a state of a subsystem in the subsystemController's list.
    /// </summary>
    /// <param name="subsystemId"></param>
    /// <param name="state"></param>
    /// <returns></returns>
    Task ModifySubsystemState(Guid subsystemId, string state);

    ///// <summary>
    ///// Removes a subsystem from the subsystemController's list.
    ///// </summary>
    ///// <param name="subsystemId"></param>
    ///// <returns></returns>
    //Task RemoveSubsystem(Guid subsystemId);

    /// <summary>
    /// Sends launch command to the subsystem module via the selected communication route.
    /// </summary>
    /// <param name="subsystemId"></param>
    /// <returns></returns>
    Task<string> LaunchSubsystem(Guid subsystemId);

    /// <summary>
    /// Sends launch command (after a timeperiod) to the subsystem module via the selected communication route.
    /// </summary>
    /// <param name="subsystemId"></param>
    /// <param name="periodOfTime"></param>
    /// <returns></returns>
    Task<string> LaunchSubsystemAfterTime(Guid subsystemId, int periodOfTime);

    /// <summary>
    /// Sends launch command to the subsystem module via the selected communication route.
    /// </summary>
    /// <param name="subsystems"></param>
    /// <returns></returns>
    Task<IEnumerable<KeyValuePair<Guid, string>>> LaunchSubsystems(IEnumerable<Guid> subsystems);

    /// <summary>
    /// Sends restart command to the subsystem module via the selected communication route.
    /// </summary>
    /// <param name="subsystemId"></param>
    /// <returns></returns>
    Task<string> RestartSubsystem(Guid subsystemId);

    /// <summary>
    /// Sends restart command to the subsystem module via the selected communication route.
    /// </summary>
    /// <param name="subsystems"></param>
    /// <returns></returns>
    Task<IEnumerable<KeyValuePair<Guid, string>>> RestartSubsystems(IEnumerable<Guid> subsystems);

    /// <summary>
    /// Sends shutdown command to the subsystem module via the selected communication route.
    /// </summary>
    /// <param name="subsystemId"></param>
    /// <returns></returns>
    Task<string> ShutdownSubsystem(Guid subsystemId);

    /// <summary>
    /// Sends shutdown command to the subsystem module via the selected communication route.
    /// </summary>
    /// <param name="subsystems"></param>
    /// <returns></returns>
    Task<IEnumerable<KeyValuePair<Guid, string>>> ShutdownSubsystems(IEnumerable<Guid> subsystems);
}
