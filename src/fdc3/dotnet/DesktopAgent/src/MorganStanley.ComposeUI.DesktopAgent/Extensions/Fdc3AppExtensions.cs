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

using Finos.Fdc3.AppDirectory;
using Finos.Fdc3.Context;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure.Internal;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol;
using AppMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.AppMetadata;
using Icon = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.Icon;

// ReSharper disable once CheckNamespace
namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent;

public static class Fdc3AppExtensions
{
    /// <summary>
    /// Determines if the app can accept the desired context in response to a raised intent.
    /// </summary>    
    internal static bool DoesAcceptContextType(this FlatAppIntent app, string contextType)
    {
        var contexts = app!.Intent?.Contexts;
        if (contexts == null || !contexts.Any())
        {
            return contextType == ContextType.Nothing.Type;
        }
        return contexts.Any(x => x == contextType);
    }

    /// <summary>
    /// Determines if the app can provide the desired result for an intent.
    /// </summary>
    /// <remarks>
    /// In case of filtering for a generic channel, specific channels need to be returned as well. These are marked as "channel<contextType>"
    /// Filtering for fdc3.nothing will match both omitted resultType and fdc3.nothing result type
    /// <see cref="https://fdc3.finos.org/docs/api/ref/DesktopAgent#findintent"/>
    /// <seealso cref="https://github.com/finos/FDC3/issues/1410"/>
    /// </remarks>    
    internal static bool HasResultType(this FlatAppIntent app, string resultType)
    {
        if (app?.Intent == null)
        {
            return false;
        }

        if (resultType == ContextTypes.Nothing)
        {
            return app.Intent.ResultType == null || app.Intent.ResultType == ContextTypes.Nothing;
        }

        if (resultType == "channel")
        {
            return app.Intent.ResultType != null && app.Intent.ResultType.StartsWith("channel");
        }

        return app.Intent.ResultType == resultType;
    }

    /// <summary>
    /// Converts an Fdc3App to an AppMetadata object, optionally setting instanceId and resultType.
    /// </summary>
    /// <param name="app">The original Fdc3App</param>
    /// <param name="instanceId">Provide if the AppMetadata represents a running instance.</param>
    /// <param name="resultType">Provide if the app has a result for intents.</param>
    /// <returns></returns>
    public static AppMetadata ToAppMetadata(this Fdc3App app, string? instanceId = null, string? resultType = null)
    {
        return new AppMetadata()
        {
            AppId = app.AppId,
            InstanceId = instanceId,
            Name = app.Name,
            Version = app.Version,
            Title = app.Title,
            Tooltip = app.ToolTip,
            Description = app.Description,
            Icons = app.Icons == null 
                    ? Enumerable.Empty<Icon>() 
                    : app.Icons.Select(Icon.GetIcon),
            Screenshots = app.Screenshots == null
                        ? Enumerable.Empty<Screenshot>()
                        : app.Screenshots.Select(Screenshot.GetScreenshot),
            ResultType = resultType
        };
    }

    /// <summary>
    /// Determines if the app can raise the desired intent with the given context by checking the <see cref="Fdc3App.Interop"/> Intents.Raises section.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="intent"></param>
    /// <param name="contextType"></param>
    /// <returns></returns>
    public static bool CanRaiseIntent(
        this Fdc3App? app, 
        string? intent = null, 
        string? contextType = null)
    {
        if (app == null)
        {
            return false;
        }

        if (app.Interop?.Intents?.Raises == null)
        {
            return false;
        }

        if (contextType == null)
        {
            return false;
        }

        if (intent == null)
        {
            var contextTypes = app.Interop.Intents.Raises.Values.SelectMany(contextType => contextType);
            return contextTypes.Contains(contextType);
        }

        if (!app.Interop.Intents.Raises.TryGetValue(intent, out var selectedContextTypes))
        {
            return false;
        }

        if (selectedContextTypes == null
            || !selectedContextTypes.Any())
        {
            return contextType == ContextTypes.Nothing;
        }

        if (selectedContextTypes.Contains(contextType))
        {
            return true;
        }

        return contextType == ContextTypes.Nothing;
    }
}
