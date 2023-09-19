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

using MorganStanley.Fdc3;
using MorganStanley.Fdc3.Context;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;

/// <summary>
/// Request, originated from fdc3.raiseIntent, to publish the raiseIntent request to the selected app.
/// </summary>
public class RaiseIntentResolutionRequest
{
    /// <summary>
    /// Context, which should be sent to the selected app via raiseIntent by the FDC3 client.
    /// </summary>
    public Context? Context { get; set; }

    /// <summary>
    /// ContextMetadata, which contains information about the source app, that raised the request.
    /// </summary>
    public ContextMetadata? ContextMetadata { get; set; }
}