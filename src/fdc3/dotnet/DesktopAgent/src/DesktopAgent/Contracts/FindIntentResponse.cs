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
/// Response from the backend to the fdc3.findIntent <see cref="FindIntentRequest"/>.
/// </summary>
public class FindIntentResponse
{
    /// <summary>
    /// <see cref="MorganStanley.Fdc3.AppIntent"/>, as result of executing the fdc3.findIntent.
    /// </summary>
    public AppIntent? AppIntent { get; init; }

    /// <summary>
    /// Error, which indicates that some error happened during the execution of fdc3.findIntent.
    /// </summary>
    public string? Error { get; init; }

    public static FindIntentResponse Success(AppIntent appIntent) => new() { AppIntent = appIntent };
    public static FindIntentResponse Failure(string error) => new() { Error = error };
}
