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

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Exceptions;

public static class Fdc3DesktopAgentErrors
{
    /// <summary>
    /// Indicates that the instanceId of the app that was raising the request is missing.
    /// </summary>
    public const string MissingId = $"{nameof(MissingId)}";

    /// <summary>
    /// Given payload from <seealso cref="MorganStanley.ComposeUI.Messaging.IMessageRouter"/> is null.
    /// </summary>
    public const string PayloadNull = $"{nameof(PayloadNull)}";

    /// <summary>
    /// Indicates that multiple matching were registered to an IntentResolver.
    /// </summary>
    public const string MultipleIntent = $"{nameof(MultipleIntent)}";

    /// <summary>
    /// Indicates that getting the IntentResult from the backend, has no appropriate attribute.
    /// </summary>
    public const string ResponseHasNoAttribute = $"{nameof(ResponseHasNoAttribute)}";
}