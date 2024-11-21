using Finos.Fdc3;
using Finos.Fdc3.AppDirectory;
using Finos.Fdc3.Context;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure.Internal;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol;
using AppMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.AppMetadata;
using Icon = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.Icon;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent;

public static class Fdc3AppExtensions
{
    /// <summary>
    /// Determines if the app can accept the desired context in response to a raised intent.
    /// </summary>    
    internal static bool DoesAcceptContextType(this FlatAppIntent app, string contextType)
    {
        var contexts = app?.Intent?.Contexts;
        if (contexts == null || !contexts.Any()) { return contextType == ContextType.Nothing.Type; }
        return app.Intent.Contexts.Any(x => x == contextType);
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
            Icons = app.Icons == null ? Enumerable.Empty<Icon>() : app.Icons.Select(Icon.GetIcon),
            Screenshots = app.Screenshots == null
                        ? Enumerable.Empty<Screenshot>()
                        : app.Screenshots.Select(Screenshot.GetScreenshot),
            ResultType = resultType
        };
    }
}
