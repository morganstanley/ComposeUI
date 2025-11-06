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

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Contracts;

internal class CreatePrivateChannelResponse
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public string? ChannelId { get; init; }

    public static CreatePrivateChannelResponse Created(string channelId) => new CreatePrivateChannelResponse { Success = true, ChannelId = channelId, Error = null};
    public static CreatePrivateChannelResponse Failed(string error) => new CreatePrivateChannelResponse { Success = false, ChannelId = null, Error = error };
}
