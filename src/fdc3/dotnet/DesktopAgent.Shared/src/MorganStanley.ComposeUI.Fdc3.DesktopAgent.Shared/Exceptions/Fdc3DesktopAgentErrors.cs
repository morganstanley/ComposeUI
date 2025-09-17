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

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Exceptions;

public static class Fdc3DesktopAgentErrors
{
    /// <summary>
    /// Indicates that the instanceId of the app that was raising the request is missing.
    /// </summary>
    public const string MissingId = nameof(MissingId);

    /// <summary>
    /// Error message when a null request is received through the messaging service.
    /// </summary>
    public const string PayloadNull = nameof(PayloadNull);

    /// <summary>
    /// Error message when a null response is received through the messaging service.
    /// </summary>
    public const string NoResponse = nameof(NoResponse);

    /// <summary>
    /// Indicates that multiple matching were registered to an IntentResolver.
    /// </summary>
    public const string MultipleIntent = nameof(MultipleIntent);

    /// <summary>
    /// Indicates that getting the IntentResult from the backend, has no appropriate attribute.
    /// </summary>
    public const string ResponseHasNoAttribute = nameof(ResponseHasNoAttribute);

    /// <summary>
    /// Indicates that no user channel set was configured.
    /// </summary>
    public const string NoUserChannelSetFound = nameof(NoUserChannelSetFound);

    /// <summary>
    /// Indicates that the listener was not found for execution.
    /// </summary>
    public const string ListenerNotFound = nameof(ListenerNotFound);

    /// <summary>
    /// Indicates that the given id is not the expected type.
    /// </summary>
    public const string IdNotParsable = nameof(IdNotParsable);

    /// <summary>
    /// Indicates that the context for the given context id is not found during the GetOpenedAppContext call.
    /// </summary>
    public const string OpenedAppContextNotFound = nameof(OpenedAppContextNotFound);

    /// <summary>
    /// Indicates that during intent resolution the requested app id exists but the instance does not
    /// </summary>
    public const string TargetInstanceUnavailable = nameof(TargetInstanceUnavailable);

    /// <summary>
    /// Indicates that the private channel a client tried to join cannot be found
    /// </summary>
    public const string PrivateChannelNotFound = nameof(PrivateChannelNotFound);
}