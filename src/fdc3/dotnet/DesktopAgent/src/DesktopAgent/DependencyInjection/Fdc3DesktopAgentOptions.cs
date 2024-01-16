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

using Microsoft.Extensions.Options;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.DependencyInjection;

public sealed class Fdc3DesktopAgentOptions : IOptions<Fdc3DesktopAgentOptions>
{
    /// <summary>
    /// When set to any value, the Desktop Agent will create the specified user channel on startup.
    /// </summary>
    public string? ChannelId { get; set; }

    /// <summary>
    /// Indicates a timeout value for getting the IntentResult from the backend in milliseconds.
    /// When set to any value, it sets the timeout for the getResult() client calls, which should wait either for this timeout or the task which gets the appropriate resolved IntentResolution.
    /// Timeout by default is 1000 milliseconds. 
    /// </summary>
    public TimeSpan IntentResultTimeout { get; set; } = TimeSpan.FromMilliseconds(1000);

    Fdc3DesktopAgentOptions IOptions<Fdc3DesktopAgentOptions>.Value => this;
}
