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
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent;

public interface IResolverUICommunicator
{
    /// <summary>
    /// Sends request for the shell to show a window for the user to select the wished intent for resolving the RaiseIntentForContext.
    /// </summary>
    /// <param name="intents"></param>
    /// <returns></returns>
    public Task<ResolverUIIntentResponse?> SendResolverUIIntentRequest(IEnumerable<string> intents, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a request for the shell to show a window, aka ResolverUI, with the appropriate AppMetadata that can solve the raised intent.
    /// </summary>
    /// <param name="appMetadata"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<ResolverUIResponse?> SendResolverUIRequest(IEnumerable<IAppMetadata> appMetadata, CancellationToken cancellationToken = default);
}