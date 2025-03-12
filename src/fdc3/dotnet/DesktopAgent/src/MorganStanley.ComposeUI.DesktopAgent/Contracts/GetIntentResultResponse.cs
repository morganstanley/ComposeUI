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

using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Exceptions;
using Finos.Fdc3;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Converters;
using System.Text.Json.Serialization;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;

/// <summary>
/// Response for the <see cref="GetIntentResultRequest"/>, raised by the clients by calling the IntentResolution.getResult().
/// </summary>
internal sealed class GetIntentResultResponse
{
    /// <summary>
    /// Indicates that the IntentResult is a typeof Context.
    /// </summary>
    [JsonConverter(typeof(ContextJsonConverter))]
    public string? Context { get; set; }

    /// <summary>
    /// Indicates that the IntentResult is a typeof Channel.
    /// </summary>
    public string? ChannelId { get; set; }

    /// <summary>
    /// Indicates that the IntentResult is a typeof Channel. Identifies the type of the Channel.
    /// </summary>
    public ChannelType? ChannelType { get; set; }

    /// <summary>
    /// Contains error text if an error happened during retrieving the IntentResult.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Indicates that the IntentHandler returned without result during its execution.
    /// </summary>
    public bool? VoidResult { get; set; }

    public static GetIntentResultResponse Success(
        string? channelId = null,
        ChannelType? channelType = null,
        string? context = null,
        bool? voidResult = null)
    {
        var response = new GetIntentResultResponse();
        if (channelId != null && channelType != null)
        {
            response.ChannelId = channelId;
            response.ChannelType = channelType;
        }
        else if (context != null)
        {
            response.Context = context;
        }
        else if (voidResult != null)
        {
            response.VoidResult = voidResult;
        }
        else
        {
            return Failure(Fdc3DesktopAgentErrors.ResponseHasNoAttribute);
        }

        return response;
    }

    public static GetIntentResultResponse Failure(string error) => new() { Error = error };
}
