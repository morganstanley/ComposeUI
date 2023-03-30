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

namespace ProcessExplorer.Server.Server.Infrastructure.WebSocket;

//Here we need to handle when we are publishing and receiving some context from clients.(UI, LocalCollector)
public static class Topics
{
    public const string AddingConnections = "local-connections-information-update";
    public const string UpdatingRuntime = "local-runtime-information-update";
    public const string UpdatingConnection = "local-connection-update";
    public const string UpdatingEnvironmentVariables = "local-environment-variables-update";
    public const string UpdatingModules = "local-modules-information-update";
    public const string UpdatingRegistrations = "local-registration-information-update";


    public const string EnableProcessWatcher = "process-monitor-enable";
    public const string DisableProcessWatcher = "process-monitor-disable";

    public const string InitilaizingSubsystems = "process-explorer-init-subsystems";
    public const string AddingSubsystem = "process-explorer-add-subsystem";
    public const string UpdatingSubsystem = "process-explorer-modify-subsystem";
    public const string RemovingSubsystem = "process-explorer-remove-subsystem";
    public const string LaunchingSubsystemWithDelay = "process-explorer-launch-subsystem-delay";
    public const string LaunchingSubsystems = "process-explorer-launch-subsystems";
    public const string RestartingSubsystems = "process-explorer-restart-subsystems";
    public const string TerminatingSubsystems = "process-explorer-shutdown-subsystems";


    public const string UpdatingProcessStatus = "process-explorer-process-status-update";
    public const string ChangedProcessInfo = "process-explorer-changed-element";
    public const string WatchingProcessChanges = "process-explorer";
    public const string AddProcesses = "process-explorer-add-processes";
    public const string TeminatingProcess = "process-explorer-remove-processes";
    public const string AddProcess = "process-explorer-add-process";
}
