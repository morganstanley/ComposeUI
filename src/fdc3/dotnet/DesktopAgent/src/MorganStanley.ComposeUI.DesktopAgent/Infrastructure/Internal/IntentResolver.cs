using System.Collections.Concurrent;
using Finos.Fdc3.AppDirectory;
using Finos.Fdc3.Context;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Exceptions;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure.Internal;

internal class IntentResolver
{
    private IAppDirectory _appDirectory;
    private ConcurrentDictionary<Guid, Fdc3App> _runningModules;

    public IntentResolver(IAppDirectory appDirectory, ConcurrentDictionary<Guid, Fdc3App> runningModules)
    {
        _appDirectory = appDirectory;
        _runningModules = runningModules;
    }



    public async Task<IEnumerable<FlatAppIntent>> GetMatchingAppsFromAppDirectory(string? intent = null, string? contextType = null, string? resultType = null, string? appIdentifier = null)
    {
        var appIntents = (await _appDirectory.GetApps()).AsFlatAppIntents();

        if (appIntents == null)
        {
            return [];
        }

        if (appIdentifier != null)
        {
            appIntents = appIntents.Where(ai => ai.App.AppId == appIdentifier);
        }

        if (intent != null)
        {
            appIntents = appIntents.Where(ai => ai.Intent.Name == intent);
        }

        if (contextType != null && contextType != ContextTypes.Nothing)
        {
            appIntents = appIntents.Where(ai => ai.DoesAcceptContextType(contextType));
        }

        if (resultType != null)
        {
            appIntents = appIntents.Where(ai => ai.HasResultType(resultType));
        }

        return appIntents;
    }

    public async Task<IEnumerable<FlatAppIntent>> GetMatchingAppInstances(
        string? intent = null,
        string? contextType = null,
        string? resultType = null,
        string? appIdentifier = null,
        Guid? instanceId = null
        )
    {
        if (instanceId != null)
        {
            return await MatchSpecificInstance(instanceId.Value, intent, contextType, resultType, appIdentifier);
        }

        var apps = Enumerable.Empty<FlatAppIntent>();
        foreach (var module in _runningModules)
        {
            var appIntents = module.Value.AsFlatAppIntents();

            if (appIdentifier != null)
            {
                appIntents = appIntents.Where(ai => ai.App.AppId == appIdentifier);
            }

            if (intent != null)
            {
                appIntents = appIntents.Where(ai => ai.Intent.Name == intent);
            }

            if (contextType != null && contextType != ContextTypes.Nothing)
            {
                appIntents = appIntents.Where(ai => ai.DoesAcceptContextType(contextType));
            }

            if (resultType != null)
            {
                appIntents = appIntents.Where(ai => ai.HasResultType(resultType));
            }
            apps = apps.Concat(appIntents);
        }
        return apps;
    }

    private async Task<IEnumerable<FlatAppIntent>> MatchSpecificInstance(
        Guid instanceId,
        string? intent = null,
        string? contextType = null,
        string? resultType = null,
        string? appIdentifier = null
        )
    {
        List<FlatAppIntent> appIntents = [];

        if (_runningModules.TryGetValue(instanceId, out var runningInstance)
            && (appIdentifier == null || runningInstance.AppId == appIdentifier))
        {
            var fai = runningInstance.AsFlatAppIntents(instanceId);
            if (intent != null)
            {
                fai = fai.Where(f => f.Intent.Name == intent);
            }
            if (contextType != null)
            {
                fai = fai.Where(f => f.DoesAcceptContextType(contextType));
            }
            if (resultType != null)
            {
                fai = fai.Where(f => f.HasResultType(resultType));
            }

            appIntents.AddRange(fai);
        }

        if (!appIntents.Any())
        {
            try
            {
                //If the app exists, but the specified instance id does not.
                if (appIdentifier != null)
                {
                    _ = await _appDirectory.GetApp(appIdentifier);
                }
                throw ThrowHelper.TargetInstanceUnavailable();
            }
            catch (AppNotFoundException)
            {
                throw ThrowHelper.TargetAppUnavailable();
            }
        }
        return appIntents;
    }
}
