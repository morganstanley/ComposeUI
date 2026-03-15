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

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client;

/// <summary>
/// Represents the FDC3 properties for an in-process application, providing necessary
/// identification and context information for proper FDC3 integration.
/// </summary>
public class Fdc3Properties
{
    /// <summary>
    /// Gets or sets the unique application identifier as defined in the FDC3 App Directory.
    /// </summary>
    public string AppId { get; set; }

    /// <summary>
    /// Gets or sets the unique instance identifier for this running instance of the application.
    /// </summary>
    public string InstanceId { get; set; }

    /// <summary>
    /// Gets or sets the channel identifier that the application is currently joined to.
    /// </summary>
    public string ChannelId { get; set; }

    /// <summary>
    /// Gets or sets the context identifier used when opening the application, typically
    /// containing context data passed during the Open or RaiseIntent operations.
    /// </summary>
    public string OpenAppContextId { get; set; }
}