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

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;

/// <summary>
/// Request for the backend, indicating that the IntentHandler of the FDC3 client has executed the raised Intent, handled appropriately, and the backend should store and send back to the originating client if the client calls the IntentResolution.getResult().
/// </summary>
public class StoreIntentResultRequest
{
    /// <summary>
    /// Intent from IntentResolution by IntentHandler of FDC3 clients.
    /// </summary>
    public string? Intent { get; set; }

    /// <summary>
    /// Target app instance id, which should receive the IntentResult, that have raised the intent.
    /// </summary>
    public string? TargetInstanceId { get; set; }

    /// <summary>
    /// IntentResult, which either can be Context or IChannel type.
    /// </summary>
    public IIntentResult? IntentResult { get; set; }
}
