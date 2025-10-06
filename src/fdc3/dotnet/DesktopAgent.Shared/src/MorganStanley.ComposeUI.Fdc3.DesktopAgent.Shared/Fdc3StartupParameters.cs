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

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared;

/// <summary>
/// This class is for setting internally the properties that should be injected to the opened app.
/// </summary>
internal class Fdc3StartupParameters
{
    public static string Fdc3InstanceId = nameof(Fdc3InstanceId);
    public static string Fdc3ChannelId = nameof(Fdc3ChannelId);
    public static string OpenedAppContextId = nameof(OpenedAppContextId);
}
