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
/// Response, for calling the fdc3.findIntentsByContext by the clients with <see cref="FindIntentsByContextRequest"/>.
/// </summary>
internal sealed class FindIntentsByContextResponse
{
    /// <summary>
    /// AppIntents, that might be returned as result of executing the fdc3.findIntentsByContext.
    /// </summary>
    public IEnumerable<AppIntent>? AppIntents { get; set; }

    /// <summary>
    /// Error, which indicates that some error happened during execution of the fdc3.findIntentsByContext.
    /// </summary>
    public string? Error { get; set; }

    public static FindIntentsByContextResponse Success(IEnumerable<AppIntent> appIntents) => new() { AppIntents = appIntents };

    public static FindIntentsByContextResponse Failure(string error) => new() { Error = error };
}