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

public sealed class Fdc3Options : IOptions<Fdc3Options>
{
    public static readonly string Fdc3OptionsName = "Fdc3Options";

    /// <summary>
    /// When set to <value>true</value>, it will enable Fdc3 backend service.
    /// </summary>
    public bool EnableFdc3 { get; set; }

    /// <summary>
    /// When set to any value, it will start the DesktopAgent with passing the value to the `WithUserChannel` builder action.
    /// </summary>
    public string? ChannelId { get; set; }

    Fdc3Options IOptions<Fdc3Options>.Value => this;
}
