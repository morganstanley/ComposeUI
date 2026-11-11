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

namespace MorganStanley.ComposeUI.Shell.Messaging;

/// <summary>
/// The claims set on the principal of an authenticated message router client.
/// </summary>
internal static class MessageRouterClaimTypes
{
    public const string AuthenticationType = "ComposeUI.MessageRouter";

    /// <summary>
    /// The kind of client: <see cref="IdentityTypes.Module"/> or <see cref="IdentityTypes.Host"/>.
    /// </summary>
    public const string IdentityType = "composeui:identity-type";

    /// <summary>
    /// The unique ID of the module instance (module clients only).
    /// </summary>
    public const string ModuleInstanceId = "composeui:module-instance-id";

    /// <summary>
    /// The ID of the module from its manifest (module clients only).
    /// </summary>
    public const string ModuleId = "composeui:module-id";

    public static class IdentityTypes
    {
        public const string Module = "module";
        public const string Host = "host";
    }
}
