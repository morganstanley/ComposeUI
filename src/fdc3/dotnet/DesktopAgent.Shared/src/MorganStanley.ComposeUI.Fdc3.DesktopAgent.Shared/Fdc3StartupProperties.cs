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

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared;

internal class Fdc3StartupProperties
{
    /// <summary>
    /// Fdc3 DesktopAgent's specific identifier for the created application instance.
    /// </summary>
    public string InstanceId { get; init; }

    /// <summary>
    /// Id of the channel the opened app should join
    /// </summary>
    public string? ChannelId { get; init; }

    /// <summary>
    /// This implies that the opened app was started via using the fdc3.open() call. Thi id ensures that if the app opens and it's available on the object then the opened app can request the context and handle it when its context listener is being registered for the right context type.
    /// </summary>
    public string? OpenedAppContextId { get; set; }
}
