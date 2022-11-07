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

using ModuleProcessMonitor.Subsystems;
using ProcessExplorer.Infrastructure;

namespace ProcessExplorer.Subsystems;

public interface ISubsystemController
{
    /// <summary>
    /// Communicator interface, which is used to send launch/shutdown and restart commands to the launcher.
    /// </summary>
    ISubsystemLauncherCommunicator SubsystemLauncherCommunicator { get; }

    /// <summary>
    /// It gets the subsystems, automatically starts them if their AutomatedStart property set to true, and send the information to the declared UI's
    /// </summary>
    /// <param name="subsystems"></param>
    /// <returns></returns>
    Task InitializeSubsystems(IEnumerable<KeyValuePair<Guid, SubsystemInfo>> subsystems);

    /// <summary>
    /// Sends a launch request to the launcher for all subsystem which is registered into the controller, and sets the states with the gotten data.
    /// </summary>
    /// <returns></returns>
    Task LaunchAllRegisteredSubsystem();

    /// <summary>
    /// Sends a launch request to the launcher, which will try to launch the subsystem after the given timeperiod.
    /// </summary>
    /// <param name="subsystemId"></param>
    /// <param name="periodOfTime"></param>
    /// <returns></returns>
    Task LaunchSubsystemAfterTime(Guid subsystemId, int periodOfTime);

    /// <summary>
    /// It will send a launch request for a subsystem, and set AutomatedStart property to true.
    /// </summary>
    /// <param name="subsystemId"></param>
    /// <returns></returns>
    Task LaunchSubsystemAutomatically(Guid subsystemId);

    /// <summary>
    /// It will send a launch request for all the subsystem, which AutomatedStart property set to true.
    /// </summary>
    /// <returns></returns>
    Task LaunchSubsystemsAutomatically();

    /// <summary>
    /// Sends a launch request to the launcher with the given subsystem IDs, and sets the states with the gotten data.
    /// </summary>
    /// <param name="subsystems"></param>
    /// <returns></returns>
    Task LaunchSubsystems(IEnumerable<string> subsystems);

    /// <summary>
    /// It will send a launch request for a subsystem.
    /// </summary>
    /// <param name="subsystemId"></param>
    /// <returns></returns>
    Task LaunchSubsystem(string subsystemId);

    /// <summary>
    /// It will send a restart request for a given subsystem IDs, and set the state with the gotten data. Also it sends update to the UIs.
    /// </summary>
    /// <param name="subsystems"></param>
    /// <returns></returns>
    Task RestartSubsystems(IEnumerable<string> subsystems);

    /// <summary>
    /// It will send a restart request for a subsystem, and set the state with the gotten data. Also it sends update to the UIs.
    /// </summary>
    /// <param name="subsystemId"></param>
    /// <returns></returns>
    Task RestartSubsystem(string subsystemId);

    /// <summary>
    /// It will send a shutdown request for all the registered subsystem, and set the state with the gotten data. Also it sends update to the UIs.
    /// </summary>
    /// <returns></returns>
    Task ShutdownAllRegisteredSubsystem();

    /// <summary>
    /// It will send a shutdown request for a subsystem, and set the state with the gotten data. Also it sends update to the UIs.
    /// </summary>
    /// <param name="subsystemId"></param>
    /// <returns></returns>
    Task ShutdownSubsystem(string subsystemId);

    /// <summary>
    /// It will send a shutdown request for a given subsystem IDs, and set the state with the gotten data. Also it sends update to the UIs.
    /// </summary>
    /// <param name="subsystems"></param>
    /// <returns></returns>
    Task ShutdownSubsystems(IEnumerable<string> subsystems);

    /// <summary>
    /// Adds a UIConnection to the store, this is a help method to register the UIs and give the IUIHandler an implementation,
    /// </summary>
    /// <param name="uiHandler"></param>
    /// <returns></returns>
    Task AddUIConnection(IUIHandler uiHandler);

    /// <summary>
    /// Removes a UIHandler from the collection.
    /// </summary>
    /// <param name="uiHandler"></param>
    /// <returns></returns>
    Task RemoveUIConnection(IUIHandler uiHandler);

    /// <summary>
    /// It will register a collection of subsystems, and save it into a list. Sends update to the UIs.
    /// </summary>
    /// <param name="subsystems"></param>
    /// <returns></returns>
    Task AddSubsystems(IEnumerable<KeyValuePair<Guid, SubsystemInfo>> subsystems);

    /// <summary>
    /// It will register a subsystem into the list. Sends update to the UIs.
    /// </summary>
    /// <param name="subsystemId"></param>
    /// <param name="subsystem"></param>
    /// <returns></returns>
    void AddSubsystem(Guid subsystemId, SubsystemInfo subsystem);

    /// <summary>
    /// Removes a subsystem from the collection and it will send an update to the UIs.
    /// </summary>
    /// <param name="subsystemId"></param>
    /// <returns></returns>
    void RemoveSubsystem(Guid subsystemId);

    /// <summary>
    /// Modifies a state of a subsystem with the given data. Send update to the registered UIs.
    /// </summary>
    /// <param name="subsystemId"></param>
    /// <param name="state"></param>
    /// <returns></returns>
    Task ModifySubsystemState(Guid subsystemId, string state);
}
