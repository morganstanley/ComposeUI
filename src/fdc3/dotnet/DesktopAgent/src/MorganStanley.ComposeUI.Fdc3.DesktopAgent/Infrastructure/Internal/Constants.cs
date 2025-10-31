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

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure.Internal;

internal static class Constants
{
    public static string DesktopAgentProvider = "ComposeUI";
    public static string SupportedFdc3Version = "2.0";
    public static string? ComposeUIVersion = typeof(Constants).Assembly.GetName().Version?.ToString();

    /// <summary>
    /// Used to indicate whether the optional `fdc3.joinUserChannel`, `fdc3.getCurrentChannel` and `fdc3.leaveCurrentChannel` are implemented by the Desktop Agent.
    /// </summary>
    public static bool SupportUserChannelMembershipAPI = true;
}