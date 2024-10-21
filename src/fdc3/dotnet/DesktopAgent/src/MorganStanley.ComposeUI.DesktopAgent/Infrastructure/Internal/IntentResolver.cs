using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Finos.Fdc3;
using Finos.Fdc3.AppDirectory;
using Finos.Fdc3.Context;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Exceptions;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol;

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

    public async Task<IEnumerable<Fdc3App>> GetMatchingAppsFromAppDirectory(string? intent = null, string? contextType = null, string? resultType = null, string? appIdentifier = null)
    {
        var apps = await _appDirectory.GetApps();

        if (apps == null)
        {
            return Enumerable.Empty<Fdc3App>();
        }

        if (appIdentifier != null)
        {
            apps = apps.Where(app => app.AppId == appIdentifier);
        }

        if (intent != null)
        {
            apps = apps.Where(app => app.DoesListenForIntent(intent));
        }

        if (contextType != null && contextType != ContextTypes.Nothing)
        {
            apps = apps.Where(app => app.DoesAcceptContextType(contextType));
        }

        if (resultType != null)
        {
            apps = apps.Where(app => app.HasResultType(resultType));
        }

        return apps;
    }

    public async Task<IEnumerable<KeyValuePair<Guid, Fdc3App>>> GetMatchingAppInstances(
        string? intent = null,
        string? contextType = null,
        string? resultType = null,
        string? appIdentifier = null,
        Guid? instanceId = null
        )
    {
        IEnumerable<KeyValuePair<Guid, Fdc3App>> apps = _runningModules;

        if (instanceId != null)
        {
            return new[] { await MatchSpecificInstance(instanceId.Value, intent, contextType, resultType, appIdentifier) };
        }

        if (appIdentifier != null)
        {
            apps = apps.Where(app => app.Value.AppId == appIdentifier);
        }

        if (intent != null)
        {
            apps = apps.Where(app => app.Value.DoesListenForIntent(intent));
        }

        if (contextType != null && contextType != ContextTypes.Nothing)
        {
            apps = apps.Where(app => app.Value.DoesAcceptContextType(contextType));
        }

        if (resultType != null)
        {
            apps = apps.Where(app => app.Value.HasResultType(resultType));
        }

        return apps;
    }

    private async Task<KeyValuePair<Guid, Fdc3App>> MatchSpecificInstance(
        Guid instanceId,
        string? intent = null,
        string? contextType = null,
        string? resultType = null,
        string? appIdentifier = null
        )
    {
        if (_runningModules.TryGetValue(instanceId, out var runningInstance)
                && (appIdentifier == null || runningInstance.AppId == appIdentifier)
                && (intent == null || runningInstance.DoesListenForIntent(intent))
                && (contextType == null || contextType == ContextTypes.Nothing || runningInstance.DoesAcceptContextType(contextType))
                && (resultType == null || runningInstance.HasResultType(resultType)))
        {
            return new KeyValuePair<Guid, Fdc3App>(instanceId, runningInstance);
        }
        else
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
    }
}
