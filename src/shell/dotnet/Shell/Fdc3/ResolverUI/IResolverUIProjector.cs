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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Finos.Fdc3;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;

namespace MorganStanley.ComposeUI.Shell.Fdc3.ResolverUI;

/// <summary>
///     Abstraction for showing the ResolverUI for the raised intent.
/// </summary>
public interface IResolverUIProjector
{
    /// <summary>
    ///     Shows ResolverUi for the user to select an module to resolve the raised intent.
    /// </summary>
    /// <param name="apps">Possible modules to resolve the intent.</param>
    /// <param name="timeout">Configurable timeout to show the blocking window for the set of time.</param>
    /// <returns></returns>
    ValueTask<ResolverUIResponse> ShowResolverUI(IEnumerable<IAppMetadata> apps, TimeSpan timeout);
}