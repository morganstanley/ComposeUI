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

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure.Internal;

/// <summary>
/// This message type is used to check if an IntentResult could be sent to the client(s).
/// </summary>
/// <typeparam name="TResponse">This is either IntentListenerResponse or RaiseIntentResponse</typeparam>
internal partial class RaiseIntentResult<TResponse>
{
    /// <summary>
    /// Response for the <seealso cref="IFdc3DesktopAgentBridge"/> call.
    /// </summary>
    public TResponse Response { get; set; }

    /// <summary>
    /// RaiseIntentResolution messages to forward to the registered IntentListeners to resolve with their registered IntentListener.
    /// </summary>
    public IEnumerable<RaiseIntentResolutionMessage> RaiseIntentResolutionMessages { get; set; } = Enumerable.Empty<RaiseIntentResolutionMessage>();
}
