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
/// Response for the <see cref="GetIntentResultRequest"/>, originated by the clients by calling the IntentResolution.getResult().
/// </summary>
public class GetIntentResultResponse
{
    /// <summary>
    /// Describes result that an Intent handler may return that should be communicated back to the app that raised the intent, via the IntentResolution.
    /// </summary>
    public IIntentResult? IntentResult { get; set; }

    /// <summary>
    /// Indicates that an error happened during retrieving the IntentResult.
    /// </summary>
    public string? Error { get; set; }

    public static GetIntentResultResponse Success(IIntentResult intentResult)
    {
        var response = new GetIntentResultResponse()
        {
            IntentResult = intentResult
        };

        return response;
    }

    public static GetIntentResultResponse Failure(string error) => new() { Error = error };
}
