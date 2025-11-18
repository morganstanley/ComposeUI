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
using Finos.Fdc3.Context;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Infrastructure;

/// <summary>
/// Provides functionality to open an application with an optional FDC3 context.
/// </summary>
internal interface IOpenClient
{
    /// <summary>
    /// Opens the specified application, optionally passing an FDC3 context.
    /// </summary>
    /// <param name="app">The application identifier to open.</param>
    /// <param name="context">The optional FDC3 context to pass to the application.</param>
    /// <returns>
    /// A <see cref="ValueTask{IAppIdentifier}"/> representing the asynchronous operation, 
    /// with the identifier of the opened application as the result.
    /// </returns>
    public ValueTask<IAppIdentifier> OpenAsync(IAppIdentifier app, IContext? context);

    /// <summary>
    /// Retrieves the sent context from the initiator if it was opened via the fdc3.open call.
    /// </summary>
    /// <param name="openAppContextId">The context ID associated with the open app request.</param>
    /// <returns></returns>
    public ValueTask<IContext> GetOpenAppContextAsync(string openAppContextId);
}
