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
/// Provides methods for interacting with FDC3 intents in the desktop agent client infrastructure.
/// </summary>
internal interface IIntentsClient
{
    /// <summary>
    /// Finds an intent by name, optionally filtering by context and result type by sending the request to the backend.
    /// </summary>
    /// <param name="intent">The name of the intent to find.</param>
    /// <param name="context">Optional context(type) to filter the intent search.</param>
    /// <param name="resultType">Optional result type to filter the intent search.</param>
    /// <returns>
    /// A <see cref="ValueTask{IAppIntent}"/> representing the asynchronous operation to find the intent.
    /// </returns>
    public ValueTask<IAppIntent> FindIntentAsync(string intent, IContext? context = null, string? resultType = null);

    /// <summary>
    /// Finds all intents that can handle the specified context, optionally filtering by result type by sending the request to the backend.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="resultType"></param>
    /// <returns></returns>
    public ValueTask<IEnumerable<IAppIntent>> FindIntentsByContextAsync(IContext context, string? resultType = null);
}
