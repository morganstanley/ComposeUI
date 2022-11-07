// /*
//  * Morgan Stanley makes this available to you under the Apache License,
//  * Version 2.0 (the "License"). You may obtain a copy of the License at
//  *
//  *      http://www.apache.org/licenses/LICENSE-2.0.
//  *
//  * See the NOTICE file distributed with this work for additional information
//  * regarding copyright ownership. Unless required by applicable law or agreed
//  * to in writing, software distributed under the License is distributed on an
//  * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
//  * or implied. See the License for the specific language governing permissions
//  * and limitations under the License.
//  */

namespace ProcessExplorerMessageRouterTopics;

public static class Topics
{
    public const string addingConnections = "local-connections-information-update";
    public const string updatingRuntime = "local-runtime-information-update";
    public const string updatingConnection = "local-connection-update";
    public const string updatingEnvironmentVariables = "local-environment-variables-update";
    public const string updatingModules = "local-modules-information-update";
    public const string updatingRegistrations = "local-registration-information-update";
    public const string enableProcessWatcher = "process-monitor-enable";
    public const string disableProcessWatcher = "process-monitor-disable";
    public const string initilaizingSubsystems = "process-explorer-init-subsystems";
    public const string addingSubsystem = "process-explorer-add-subsystem";
    public const string modifyingSubsystem = "process-explorer-modify-subsystem";
    public const string removingSubsystem = "process-explorer-remove-subsystem";
    public const string changedProcessInfo = "process-explorer-changed-element";
    public const string watchingProcessChanges = "process-explorer";
    public const string launchingSubsystemWithDelay = "process-explorer-launch-subsystem-delay";
    public const string launchingSubsystems = "process-explorer-launch-subsystems";
    public const string restartingSubsystems = "process-explorer-restart-subsystems";
    public const string terminatingSubsystems = "process-explorer-shutdown-subsystems";
}
