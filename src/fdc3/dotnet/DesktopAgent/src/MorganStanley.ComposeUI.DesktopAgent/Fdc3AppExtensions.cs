using Finos.Fdc3;
using Finos.Fdc3.AppDirectory;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent;

public static class Fdc3AppExtensions
{
    public static bool DoesListenForIntent(this Fdc3App app, string intentName)
    {
        if (app?.Interop?.Intents?.ListensFor == null) { return false; }
        return app.Interop.Intents.ListensFor.Keys.Contains(intentName);
    }

    public static bool DoesAcceptContextType(this Fdc3App app, string contextType)
    {
        if (app?.Interop?.Intents?.ListensFor == null) { return false; }
        return app.Interop.Intents.ListensFor.SelectMany(x => x.Value.Contexts).Any(x => x == contextType);
    }

    public static bool HasResultType(this Fdc3App app, string resultType)
    {
        if (app?.Interop?.Intents?.ListensFor == null)
        {
            return false;
        }

        // In case of filtering for a generic channel, specific channels need to be returned as well. These are marked as "channel<contextType>"
        // https://fdc3.finos.org/docs/api/ref/DesktopAgent#findintent
        if (resultType == "channel")
        {
            return app.Interop.Intents.ListensFor.Values.Any(intent => intent.ResultType != null && intent.ResultType.StartsWith("channel"));
        }

        return app.Interop.Intents.ListensFor.Values.Any(intent => intent.ResultType == resultType);
    }


    /// <summary>
    /// Converts a collection of Fdc3Apps into a KeyValuePair collection of AppIntent values keyed by intent type.
    /// </summary>
    /// <param name="apps"></param>
    /// <returns></returns>
    public static IEnumerable<KeyValuePair<string, AppIntent>> ToAppIntents(this Fdc3App app)
    {
        if (app.Interop?.Intents?.ListensFor == null)
        {
            return Enumerable.Empty<KeyValuePair<string, AppIntent>>();
        }

        Dictionary<string, AppIntent> appIntents = new Dictionary<string, AppIntent>();


        foreach (var intent in app.Interop.Intents.ListensFor)
        {
            var newApp = new AppMetadata(app.AppId,
                    name: app.Name,
                    version: app.Version,
                    title: app.Title,
                    tooltip: app.ToolTip,
                    description: app.Description,
                    icons: app.Icons,
                    screenshots: app.Screenshots,
                    resultType: intent.Value.ResultType);


            if (appIntents.TryGetValue(intent.Key, out var appIntent))
            {
                appIntents[intent.Key] = new AppIntent(intent.Value, appIntent.Apps.Append(newApp));
            }
            else
            {
                appIntents.Add(intent.Key, new AppIntent(intent.Value, new[] { newApp }));
            }
        }

        return appIntents;
    }
}
