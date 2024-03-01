/*
 * Morgan Stanley makes this available to you under the Apache License,
 * Version 2.0 (the "License"). You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0.
 *
 * See the NOTICE file distributed with this work for additional information
 * regarding copyright ownership. Unless required by applicable law or agreed
 * to in writing, software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
 * or implied. See the License for the specific language governing permissions
 * and limitations under the License.
 */

using Finos.Fdc3;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Exceptions;

public static class ThrowHelper
{
    public static Fdc3DesktopAgentException MissingFdc3InstanceId(string moduleId) =>
        new(Fdc3DesktopAgentErrors.MissingId, $"Missing Fdc3InstanceId for module: {moduleId}, when module is started and FDC3 is enabled by the application.");

    public static Fdc3DesktopAgentException MissingAppFromRaisedIntentInvocations(string instanceId) =>
        new(Fdc3DesktopAgentErrors.MissingId, $"Missing Fdc3InstanceId: {instanceId}, when module has added its intent listener and FDC3 is enabled by the application.");

    public static Fdc3DesktopAgentException MultipleIntentRegisteredToAnAppInstance(string intent) =>
        new(Fdc3DesktopAgentErrors.MultipleIntent, $"Multiplpe intent were registered to the running instance. Intent: {intent}.");

    public static Fdc3DesktopAgentException TargetInstanceUnavailable() =>
        new(ResolveError.TargetAppUnavailable, $"Target app is unavailable when intent was raised.");

    public static Fdc3DesktopAgentException StartModuleFailure(Exception exception) =>
        new(Fdc3DesktopAgentErrors.StartModuleFailure, $"Could not start module when intent was raised.", exception);

    public static Fdc3DesktopAgentException MissingIntentForMessage(string messageId, string intent) =>
        new(Fdc3DesktopAgentErrors.MissingIntent, $"Could not found raised intent invocations for message: {messageId} with intent: {intent}");    
}