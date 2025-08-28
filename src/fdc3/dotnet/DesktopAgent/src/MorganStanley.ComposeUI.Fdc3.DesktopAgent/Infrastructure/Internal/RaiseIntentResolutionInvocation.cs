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

using Finos.Fdc3;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure.Internal;

internal class RaiseIntentResolutionInvocation
{
    public RaiseIntentResolutionInvocation(
        int raiseIntentMessageId,
        string intent,
        string originFdc3InstanceId,
        string        contextToHandle,
        string? resultContext = null,
        string? resultChannelId = null,
        ChannelType? resultChannelType = null,
        bool? resultVoid = null,
        string? resultError = null)
    {
        RaiseIntentMessageId = $"{raiseIntentMessageId}-{Guid.NewGuid()}";
        Intent = intent;
        OriginFdc3InstanceId = originFdc3InstanceId;
        Context = contextToHandle;
        ResultContext = resultContext;
        ResultChannelId = resultChannelId;
        ResultChannelType = resultChannelType;
        ResultVoid = resultVoid;
        ResultError = resultError;
    }

    public string RaiseIntentMessageId { get; }
    public string Intent { get; }
    public string OriginFdc3InstanceId { get; }
    public string Context { get; }
    public string? ResultContext { get; set; }
    public string? ResultChannelId { get; set; }
    public ChannelType? ResultChannelType { get; set; }
    public bool? ResultVoid { get; set; }
    public string? ResultError { get; set; }
    public bool IsResolved { get; set; } = false;
}
