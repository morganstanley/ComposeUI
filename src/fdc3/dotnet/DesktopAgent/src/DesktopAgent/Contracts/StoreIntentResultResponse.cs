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

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;

/// <summary>
/// Response for the client, which handled the intent, if the request <see cref="StoreIntentResultRequest"/> was successfully stored.
/// </summary>
public class StoreIntentResultResponse
{
    /// <summary>
    /// Indicates that the IntentResult is successfully stored or not.
    /// </summary>
    public bool Stored { get; set; } = false;

    /// <summary>
    /// Error, which indicates that an error might have occured while handling the request.
    /// </summary>
    public string? Error { get; set; }

    public static StoreIntentResultResponse Success()
    {
        var response = new StoreIntentResultResponse()
        {
            Stored = true
        };

        return response;
    }

    public static GetIntentResultResponse Failure(string error) => new() { Error = error };
}
