using Finos.Fdc3;
using Finos.Fdc3.AppDirectory;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol;
using AppMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.AppMetadata;
using Icon = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.Icon;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent;

public static class Fdc3AppExtensions
{
    /// <summary>
    /// Determines if the app listens to the specified intent
    /// </summary>    
    public static bool DoesListenForIntent(this Fdc3App app, string intentName)
    {
        if (app?.Interop?.Intents?.ListensFor == null) { return false; }
        return app.Interop.Intents.ListensFor.Keys.Contains(intentName);
    }

    /// <summary>
    /// Determines if the app can accept the desired context in response to a raised intent.
    /// </summary>    
    public static bool DoesAcceptContextType(this Fdc3App app, string contextType)
    {
        if (app?.Interop?.Intents?.ListensFor == null) { return false; }
        return app.Interop.Intents.ListensFor.SelectMany(x => x.Value.Contexts).Any(x => x == contextType);
    }

    /// <summary>
    /// Determines if the app can provide the desired result for an intent.
    /// </summary>
    /// <remarks>
    /// In case of filtering for a generic channel, specific channels need to be returned as well. These are marked as "channel<contextType>"
    /// <see cref="https://fdc3.finos.org/docs/api/ref/DesktopAgent#findintent"/>
    /// </remarks>    
    public static bool HasResultType(this Fdc3App app, string resultType)
    {
        if (app?.Interop?.Intents?.ListensFor == null)
        {
            return false;
        }
                
        if (resultType == "channel")
        {
            return app.Interop.Intents.ListensFor.Values.Any(intent => intent.ResultType != null && intent.ResultType.StartsWith("channel"));
        }

        return app.Interop.Intents.ListensFor.Values.Any(intent => intent.ResultType == resultType);
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
