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

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Contracts;

/// <summary>
/// Response from the server that it successfully registered the IntentListener of the client raised by the client via <seealso cref="IntentListenerRequest"/>.
/// </summary>
internal sealed class IntentListenerResponse
{
    /// <summary>
    /// Indicates if the server successfully stored the IntentListener.
    /// </summary>
    public bool Stored { get; set; }

    /// <summary>
    /// Contains error text if an error happened during the registration.
    /// </summary>
    public string? Error { get; set; }

    public static IntentListenerResponse Failure(string error) => new() { Error = error };
    public static IntentListenerResponse SubscribeSuccess() => new() { Stored = true };
    public static IntentListenerResponse UnsubscribeSuccess() => new() { Stored = false };
}
