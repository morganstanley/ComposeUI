// Morgan Stanley makes this available to you under the Apache License,
// Version 2.0 (the "License"). You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0.
// 
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership. Unless required by applicable law or agreed
// to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
// or implied. See the License for the specific language governing permissions
// and limitations under the License.

using System.Text.Json.Serialization;
using Finos.Fdc3;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol;

/// <summary>
/// Class used for parsing the UserChannel from the user channel set config file.
/// </summary>
public class ChannelItem
{
    /// <summary>
    /// Unique identifier of the channel.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Type of the channel.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ChannelType Type { get; set; }

    /// <summary>
    /// Metadata specific to the channel.
    /// </summary>
    public DisplayMetadata DisplayMetadata { get; set; }
}
