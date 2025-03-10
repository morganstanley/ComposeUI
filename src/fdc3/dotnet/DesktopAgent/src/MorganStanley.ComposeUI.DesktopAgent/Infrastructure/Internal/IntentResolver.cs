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

using System.Collections.Concurrent;
using Finos.Fdc3.AppDirectory;
using Finos.Fdc3.Context;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Exceptions;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Extensions;

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
        IEnumerable<FlatAppIntent> appIntents;

        if (appIdentifier != null)
        {
            appIntents = new[] { await _appDirectory.GetApp(appIdentifier) }.AsFlatAppIntents();            
        }
        else
        {
            appIntents = (await _appDirectory.GetApps()).AsFlatAppIntents();
        }

        if (appIntents == null)
        {
            return [];
        }

        if (intent != null)
        {
            appIntents = appIntents.Where(ai => ai.Intent.Name == intent);
        }

        if (contextType != null)
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
            var appIntents = module.Value.AsFlatAppIntents(module.Key);

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

        Fdc3App? runningInstance = null;
        if (_runningModules.TryGetValue(instanceId, out runningInstance)
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
            var appSpecified = appIdentifier != null;
            var otherFilters = intent != null || contextType != null || resultType != null;

            if (runningInstance == null)
            {
                throw ThrowHelper.TargetInstanceUnavailable();
            }
            if (otherFilters)
            {
                throw ThrowHelper.NoAppsFound();
            }
            if (appIdentifier != null)
            {
                try
                {
                    _ = await _appDirectory.GetApp(appIdentifier);
                    throw ThrowHelper.NoAppsFound();
                }
                catch (AppNotFoundException)
                {
                    throw ThrowHelper.TargetAppUnavailable();
                }
            }
        }

        return appIntents;
    }
}
