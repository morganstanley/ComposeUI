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
/// Identifies the module instance a message router access token was issued to.
/// </summary>
/// <param name="ModuleInstanceId">The unique ID of the module instance.</param>
/// <param name="ModuleId">The ID of the module (from its manifest).</param>
internal sealed record ModuleIdentity(Guid ModuleInstanceId, string ModuleId);
