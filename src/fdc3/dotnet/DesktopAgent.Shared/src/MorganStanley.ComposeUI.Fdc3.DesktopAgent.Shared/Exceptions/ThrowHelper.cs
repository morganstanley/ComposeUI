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
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Contracts;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Exceptions;

internal static class ThrowHelper
{
    public static Fdc3DesktopAgentException MissingFdc3InstanceId(string moduleId) =>
        new(Fdc3DesktopAgentErrors.MissingId, $"Missing Fdc3InstanceId for module: {moduleId}, when module is started and FDC3 is enabled by the application.");

    public static Fdc3DesktopAgentException MissingAppFromRaisedIntentInvocations(string instanceId) =>
        new(Fdc3DesktopAgentErrors.MissingId, $"Missing Fdc3InstanceId: {instanceId}, when module has added its intent listener and FDC3 is enabled by the application.");

    public static Fdc3DesktopAgentException MultipleIntentRegisteredToAnAppInstance(string intent) =>
        new(Fdc3DesktopAgentErrors.MultipleIntent, $"Multiple intents were registered to the running instance. Intent: {intent}.");

    public static Fdc3DesktopAgentException TargetInstanceUnavailable() =>
        new(ResolveError.TargetInstanceUnavailable, "Target instance was unavailable when intent was raised.");

    public static Fdc3DesktopAgentException TargetAppUnavailable() =>
        new(ResolveError.TargetAppUnavailable, "Target app was unavailable when intent was raised");

    public static Fdc3DesktopAgentException NoAppsFound() =>
        new(ResolveError.NoAppsFound, "No app matched the filter criteria.");

    public static Fdc3DesktopAgentException MissingChannelId() =>
        new("No channel ID was passed for registering the channel on the client.");

    public static Fdc3DesktopAgentException MissingResponse() =>
        new("No response was received from the FDC3 backend server.");

    public static Fdc3DesktopAgentException ErrorResponseReceived(string? error) =>
        new($"Error was received from the FDC3 backend server: {error}.");

    public static Fdc3DesktopAgentException UnsuccessfulSubscription(AddContextListenerRequest request) =>
        new($"The {request.Fdc3InstanceId} app was not able to register its context listener for channel:{request.ChannelId}; type: {request.ChannelType}; context: {request.ContextType}..");

    public static Fdc3DesktopAgentException MissingSubscriptionId() =>
        new("No successful response containing the subscription ID was received from the FDC3 backend server while registering the context listener.");

    public static Fdc3DesktopAgentException UnsuccessfulSubscriptionUnRegistration(RemoveContextListenerRequest request) =>
        new($"The {request.Fdc3InstanceId} app was not able to unregister its context listener for context: {request.ContextType} with listener ID: {request.ListenerId}...");

    public static Fdc3DesktopAgentException UnsuccessfulUserChannelJoining(JoinUserChannelRequest request) =>
        new($"The {request.InstanceId} app was not able to join to user channel: {request.ChannelId}...");

    public static Fdc3DesktopAgentException MissingAppId(string appId) =>
        new Fdc3DesktopAgentException($"AppId for the app cannot be retrieved: {appId}!");

    public static Fdc3DesktopAgentException MissingInstanceId(string appId, string instanceId) =>
        new Fdc3DesktopAgentException($"InstanceId defined by the ModuleLoader/StartupAction for the app cannot be retrieved! AppID: {appId}; instanceID: {instanceId}");

    public static Fdc3DesktopAgentException ClientNotConnectedToUserChannel() =>
        new("No current channel to broadcast the context to.");

    public static Fdc3DesktopAgentException ErrorResponseReceived(string initiator, string appId, string type, string error) =>
        new($"{initiator} cannot return the {type} for app: {appId} due to: {error}.");

    internal static Fdc3DesktopAgentException ContextListenerNotCreated(string id, string? contextType) =>
        new($"The context listener is not created/returned on channel: {id} for context: {contextType}.");

    internal static Fdc3DesktopAgentException InvalidResponseRecevied(string instanceId, string appId, string methodName) =>
        new($"The method: {methodName} returned a not valid response from the server, app: {appId}, instance: {instanceId}.");
    public static Fdc3DesktopAgentException NoChannelsReturned() =>
        new("The DesktopAgent backend did not return any channel.");

    public static Fdc3DesktopAgentException AppChannelIsNotCreated(string channelId) =>
        new($"The application channel with ID: {channelId} is not created successfully.");

    public static Fdc3DesktopAgentException AppIntentIsNotDefined(string intent, string? contextType = null, string? resultType = null) =>
        new($"The {nameof(AppIntent)} was not returned by the FDC3 DesktopAgent backend for intent: {intent}; context: {contextType}; and resultType: {resultType}.");

    public static Fdc3DesktopAgentException ChannelNotFound(string channelId, ChannelType channelType) =>
        new($"The channel with ID: {channelId} and type: {channelType} is not found.");

    public static Fdc3DesktopAgentException IntentResolutionIsNotDefined(string contextType, string? appId = null, string? instanceId = null, string? intent = null) =>
        new($"The app with ID: {appId} and instanceId: {instanceId} for context: {contextType} could not return an {nameof(IntentResolution)} as message id, the {nameof(AppMetadata)} or the intent: {intent} was not retrieved from the backend.");

    public static Fdc3DesktopAgentException IntentResolutionFailed(string intent, string messageId, IAppIdentifier appIdentifier) =>
        new($"Retrieving the intent resolution failed from the backend for intent: {intent}, app: {appIdentifier.AppId}, instanceId: {appIdentifier.InstanceId}, messageId: {messageId}.");

    public static Fdc3DesktopAgentException MissingContext() =>
        new($"No context or invalid context was received as part of the {nameof(IntentResolution)}.");

    public static Fdc3DesktopAgentException MissingOpenedAppContext() =>
        new("No context was received from the backend.");

    public static Fdc3DesktopAgentException IntentResultStoreFailed(string intent, string instanceId) =>
        new($"Instance: {instanceId} was not able to store the {nameof(IIntentResult)} on the backend.");

    public static Fdc3DesktopAgentException ListenerNotRegistered(string intent, string instanceId) =>
        new($"Intent listener is not registered for the intent: {intent} on the backend for instance: {instanceId}.");

    public static Fdc3DesktopAgentException ListenerNotUnRegistered(string intent, string instanceId) =>
        new($"Intent listener is still registered for the intent: {intent} on the backend for instance: {instanceId}.");

    internal static Fdc3DesktopAgentException MalformedContext() =>
        new ($"The context is malformed when the fdc3.open method was called for the native client.");

    internal static Fdc3DesktopAgentException AppIdentifierNotRetrieved() =>
        new($"The {nameof(AppIdentifier)} cannot be returned when calling the fdc3.open from the native client...");

    internal static Fdc3DesktopAgentException MissingOpenAppContext() =>
        new("The context id was received while checking if the app is opened via the fdc3.open call...");
}