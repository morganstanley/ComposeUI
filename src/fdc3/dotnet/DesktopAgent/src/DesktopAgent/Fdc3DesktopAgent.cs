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
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.DependencyInjection;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Exceptions;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure.Internal;
using MorganStanley.ComposeUI.ModuleLoader;
using Finos.Fdc3;
using Finos.Fdc3.AppDirectory;
using Finos.Fdc3.Context;
using AppIdentifier = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.AppIdentifier;
using AppIntent = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.AppIntent;
using AppMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.AppMetadata;
using ContextMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.ContextMetadata;
using Icon = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.Icon;
using IntentMetadata = Finos.Fdc3.AppDirectory.IntentMetadata;
using Screenshot = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.Screenshot;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent;

internal class Fdc3DesktopAgent : IFdc3DesktopAgentBridge
{
    private readonly ILogger<Fdc3DesktopAgent> _logger;
    private readonly List<UserChannel> _userChannels = new();
    private readonly ILoggerFactory _loggerFactory;
    private readonly Fdc3DesktopAgentOptions _options;
    private readonly IAppDirectory _appDirectory;
    private readonly IModuleLoader _moduleLoader;
    private readonly ConcurrentDictionary<Guid, Fdc3App> _runningModules = new();
    private readonly ConcurrentDictionary<Guid, RaisedIntentRequestHandler> _raisedIntentResolutions = new();
    private IAsyncDisposable? _subscription;

    public Fdc3DesktopAgent(
        IAppDirectory appDirectory,
        IModuleLoader moduleLoader,
        IOptions<Fdc3DesktopAgentOptions> options,
        ILoggerFactory? loggerFactory = null)
    {
        _appDirectory = appDirectory;
        _moduleLoader = moduleLoader;
        _options = options.Value;
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _logger = _loggerFactory.CreateLogger<Fdc3DesktopAgent>() ?? NullLogger<Fdc3DesktopAgent>.Instance;
    }

    public async ValueTask AddUserChannel(UserChannel userChannel)
    {
        await userChannel.Connect();
        _userChannels.Add(userChannel);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var observable = _moduleLoader.LifetimeEvents.ToAsyncObservable();
        var subscription = await observable.SubscribeAsync(
            async (lifetimeEvent) =>
            {
                switch (lifetimeEvent)
                {
                    case LifetimeEvent.Stopped:
                        await RemoveModuleAsync(lifetimeEvent.Instance);
                        break;

                    case LifetimeEvent.Started:
                        await AddOrUpdateModuleAsync(lifetimeEvent.Instance);
                        break;
                }
            });

        _subscription = subscription;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var tasks = _userChannels.Select(x => x.DisposeAsync()).ToArray();

        await SafeWaitAsync(tasks);

        if (_subscription != null)
        {
            _runningModules.Clear();
            await _subscription.DisposeAsync();
        }
    }

    public bool FindChannel(string channelId, ChannelType channelType)
    {
        return channelType switch
        {
            ChannelType.User => _userChannels.Any(x => x.Id == channelId),
            ChannelType.App or ChannelType.Private => throw new NotSupportedException(),
            _ => false,
        };
    }

    public async ValueTask<FindIntentResponse> FindIntent(FindIntentRequest? request)
    {
        if (request == null)
        {
            return FindIntentResponse.Failure(ResolveError.IntentDeliveryFailed);
        }

        //This function returns null, if the app could not be accepted based on the intent (required), context (optional in request), resultType (optional in request)
        //else for consistency it will return a single element array containing the intentMetadata which is allowed by the request.
        Func<Fdc3App, IEnumerable<IntentMetadata>?> selector = (fdc3App) =>
        {
            if (fdc3App.Interop?.Intents?.ListensFor == null
                || !fdc3App.Interop.Intents.ListensFor.TryGetValue(request.Intent!, out var intentMetadata))
                return null;
            if (request.Context != null
                && (intentMetadata.Contexts == null || !intentMetadata.Contexts.Contains(request.Context.Type))
                && request.Context?.Type != ContextTypes.Nothing) return null;
            if (request.ResultType != null
                && (intentMetadata.ResultType == null || !intentMetadata.ResultType.Contains(request.ResultType)))
                return null;
            return new[] {intentMetadata};
        };

        var appIntents = await GetAppIntentsByRequest(selector, null, false);

        if (!appIntents.TryGetValue(request.Intent, out var appIntent))
            return FindIntentResponse.Failure(ResolveError.NoAppsFound);

        return FindIntentResponse.Success(appIntent);
    }

    public async ValueTask<FindIntentsByContextResponse> FindIntentsByContext(FindIntentsByContextRequest? request)
    {
        if (request == null)
        {
            return FindIntentsByContextResponse.Failure(ResolveError.IntentDeliveryFailed);
        }

        //This function returns null, if the app could not be accepted based on the context(optional in request), resultType (optional in request)
        //else for consistency it will return a collection containing the intentMetadata which is allowed by the request.
        Func<Fdc3App, IEnumerable<IntentMetadata>?> selector = (fdc3App) =>
        {
            var intentMetadataCollection = new List<IntentMetadata>();
            if (fdc3App.Interop?.Intents?.ListensFor?.Values != null)
            {
                foreach (var intentMetadata in fdc3App.Interop.Intents.ListensFor.Values)
                {
                    if (intentMetadata.Contexts == null
                        || !intentMetadata.Contexts.Contains(request.Context?.Type)
                        && request.Context?.Type != ContextTypes.Nothing) continue;
                    if (request.ResultType != null
                        && (intentMetadata.ResultType == null
                            || !intentMetadata.ResultType.Contains(request.ResultType))) continue;
                    intentMetadataCollection.Add(intentMetadata);
                }
            }

            if (intentMetadataCollection.Any())
            {
                return intentMetadataCollection;
            }

            return null;
        };

        var appIntents = await GetAppIntentsByRequest(selector, null, false);

        if (!appIntents.Any())
        {
            return FindIntentsByContextResponse.Failure(ResolveError.NoAppsFound);
        }

        return FindIntentsByContextResponse.Success(appIntents.Values);
    }

    public async ValueTask<GetIntentResultResponse> GetIntentResult(GetIntentResultRequest? request)
    {
        if (request == null)
        {
            return GetIntentResultResponse.Failure(ResolveError.IntentDeliveryFailed);
        }

        if (request.TargetAppIdentifier?.InstanceId == null || request.Intent == null || request.MessageId == null)
        {
            return GetIntentResultResponse.Failure(ResolveError.IntentDeliveryFailed);
        }

        using var cancellationTokenSource = new CancellationTokenSource();
        try
        {
            var intentResolution = await GetIntentResolutionResult(request).WaitAsync(_options.IntentResultTimeout);

            return GetIntentResultResponse.Success(
                intentResolution!.ResultChannelId,
                intentResolution!.ResultChannelType,
                intentResolution!.ResultContext,
                intentResolution!.ResultVoid);
        }
        catch (TimeoutException)
        {
            return GetIntentResultResponse.Failure(ResolveError.IntentDeliveryFailed);
        }
    }

    public ValueTask<StoreIntentResultResponse> StoreIntentResult(StoreIntentResultRequest? request)
    {
        if (request == null)
        {
            return ValueTask.FromResult(StoreIntentResultResponse.Failure(ResolveError.IntentDeliveryFailed));
        }

        if (request.TargetFdc3InstanceId == null || request.Intent == null)
        {
            return ValueTask.FromResult(StoreIntentResultResponse.Failure(ResolveError.IntentDeliveryFailed));
        }

        _raisedIntentResolutions.AddOrUpdate(
            new Guid(request.OriginFdc3InstanceId),
            _ => throw ThrowHelper.MissingAppFromRaisedIntentInvocations(request.OriginFdc3InstanceId),
            (key, oldValue) => oldValue.AddIntentResult(
                messageId: request.MessageId,
                intent: request.Intent,
                channelId: request.ChannelId,
                channelType: request.ChannelType,
                context: request.Context,
                voidResult: request.VoidResult,
                error: request.Error));

        return ValueTask.FromResult(StoreIntentResultResponse.Success());
    }

    public async ValueTask<RaiseIntentResult<IntentListenerResponse>> AddIntentListener(IntentListenerRequest? request)
    {
        if (request == null)
        {
            return new()
            {
                Response = IntentListenerResponse.Failure(Fdc3DesktopAgentErrors.PayloadNull)
            };
        }

        switch (request.State)
        {
            case SubscribeState.Subscribe:
                if (_raisedIntentResolutions.TryGetValue(new(request.Fdc3InstanceId), out var resolver))
                {
                    resolver.AddIntentListener(request.Intent);

                    var resolutions = new List<RaiseIntentResolutionMessage>();
                    foreach (var raisedIntent in resolver.RaiseIntentResolutions.Where(
                                 invocation => invocation.Intent == request.Intent))
                    {
                        var resolution = await RaiseIntentResolution(
                            raisedIntent.RaiseIntentMessageId,
                            raisedIntent.Intent,
                            raisedIntent.Context,
                            request.Fdc3InstanceId,
                            raisedIntent.OriginFdc3InstanceId);

                        if (resolution != null)
                        {
                            resolutions.Add(resolution);
                        }
                    }

                    return new()
                    {
                        Response = IntentListenerResponse.SubscribeSuccess(),
                        RaiseIntentResolutionMessages = resolutions
                    };
                }
                else
                {
                    var createdResolver = _raisedIntentResolutions.GetOrAdd(
                        new(request.Fdc3InstanceId),
                        new RaisedIntentRequestHandler());

                    createdResolver.AddIntentListener(request.Intent);

                    return new()
                    {
                        Response = IntentListenerResponse.SubscribeSuccess(),
                    };
                }

            case SubscribeState.Unsubscribe:

                if (_raisedIntentResolutions.TryGetValue(new(request.Fdc3InstanceId), out var resolverToRemove))
                {
                    resolverToRemove.RemoveIntentListener(request.Intent);
                    return new()
                    {
                        Response = IntentListenerResponse.UnsubscribeSuccess()
                    };
                }

                break;
        }

        return new()
        {
            Response = IntentListenerResponse.Failure(Fdc3DesktopAgentErrors.MissingId)
        };
    }

    public async ValueTask<RaiseIntentResult<RaiseIntentResponse>> RaiseIntent(RaiseIntentRequest? request)
    {
        if (request == null)
        {
            return new()
            {
                Response = RaiseIntentResponse.Failure(ResolveError.IntentDeliveryFailed)
            };
        }

        if (!string.IsNullOrEmpty(request.Error))
        {
            return new()
            {
                Response = RaiseIntentResponse.Failure(request.Error)
            };
        }

        //This function returns null, if the app could not be accepted based on the intent (required), context (optional in request), appIdentifier (optional in request)
        //else for consistency it will return a single element array containing the intentMetadata which is allowed by the request.
        Func<Fdc3App, IEnumerable<IntentMetadata>?> selector = (fdc3App) =>
        {
            if (fdc3App.Interop?.Intents?.ListensFor == null
                || !fdc3App.Interop.Intents.ListensFor.TryGetValue(request.Intent!, out var intentMetadata))
                return null;
            if (request.Context != null
                && (intentMetadata.Contexts == null || !intentMetadata.Contexts.Contains(request.Context.Type))
                && request.Context?.Type != ContextTypes.Nothing) return null;
            if (request.TargetAppIdentifier != null && (fdc3App.AppId != request.TargetAppIdentifier.AppId))
                return null;
            return new[] {intentMetadata};
        };

        var appIntents = await GetAppIntentsByRequest(selector, request.TargetAppIdentifier, request.Selected);

        //No intents were found which would have the right information to handle the raised intent
        if (!appIntents.TryGetValue(request.Intent, out var appIntent) || !appIntent.Apps.Any())
        {
            return new()
            {
                Response = RaiseIntentResponse.Failure(ResolveError.NoAppsFound)
            };
        }

        if (appIntent.Apps.Count() == 1)
        {
            return await RaiseIntentToApplication(
                request.MessageId,
                appIntent.Apps.ElementAt(0),
                request.Intent,
                request.Context,
                request.Fdc3InstanceId);
        }

        //Resolve to one app via ResolverUI.
        var result = await WaitForResolverUIAsync(request.Intent, appIntent.Apps);

        if (result != null)
        {
            return await RaiseIntentToApplication(
                request.MessageId,
                result,
                request.Intent,
                request.Context,
                request.Fdc3InstanceId);
        }
        else
        {
            return new()
            {
                Response = RaiseIntentResponse.Failure(ResolveError.UserCancelledResolution)
            };
        }
    }

    //TODO: Placeholder for the right implementation of returning the chosen application from the ResolverUI.
    private async Task<AppMetadata?> WaitForResolverUIAsync(string intent, IEnumerable<AppMetadata> apps)
    {
        Task<bool> IsIntentListenerRegisteredAsync(AppMetadata appMetadata)
        {
            if (_raisedIntentResolutions.TryGetValue(new Guid(appMetadata.InstanceId!), out var resolver))
            {
                if (resolver.IsIntentListenerRegistered(intent))
                {
                    return Task.FromResult(true);
                }
            }

            return Task.FromResult(false);
        }

        ;

        var runningApplications = apps.Where(app => app.InstanceId != null).ToArray();
        if (runningApplications.Length >= 1)
        {
            for (var i = 0; i <= runningApplications.Length; i++)
            {
                var application = runningApplications[i];
                if (await IsIntentListenerRegisteredAsync(application))
                {
                    return application;
                }
            }
        }

        return apps.First();
    }

    //Here we have a specific application which should either start or we should send a intent resolution request
    private async ValueTask<RaiseIntentResult<RaiseIntentResponse>> RaiseIntentToApplication(
        int messageId,
        IAppMetadata targetAppMetadata,
        string intent,
        Context context,
        string sourceFdc3InstanceId)
    {
        RaiseIntentResolutionMessage? resolution = null;
        if (!string.IsNullOrEmpty(targetAppMetadata.InstanceId))
        {
            var raisedIntentMessageId = StoreRaisedIntentForTarget(
                messageId,
                targetAppMetadata.InstanceId,
                intent,
                context,
                sourceFdc3InstanceId);

            if (!_raisedIntentResolutions.TryGetValue(new(targetAppMetadata.InstanceId), out var registeredFdc3App))
            {
                return new()
                {
                    Response = RaiseIntentResponse.Failure(ResolveError.TargetInstanceUnavailable)
                };
            }

            if (registeredFdc3App.IsIntentListenerRegistered(intent))
            {
                resolution = await RaiseIntentResolution(
                    raisedIntentMessageId,
                    intent,
                    context,
                    targetAppMetadata.InstanceId,
                    sourceFdc3InstanceId);
            }

            return new()
            {
                Response = RaiseIntentResponse.Success(raisedIntentMessageId, intent, targetAppMetadata),
                RaiseIntentResolutionMessages = resolution != null
                    ? new[] {resolution}
                    : Enumerable.Empty<RaiseIntentResolutionMessage>()
            };
        }
        else
        {
            try
            {
                var fdc3InstanceId = Guid.NewGuid();
                var moduleInstance = await _moduleLoader.StartModule(
                                         new StartRequest(
                                             targetAppMetadata
                                                 .AppId, //TODO: possible remove some identifier like @"fdc3."
                                             new List<KeyValuePair<string, string>>()
                                             {
                                                 {new(Fdc3StartupParameters.Fdc3InstanceId, fdc3InstanceId.ToString())}
                                             }))
                                     ?? throw ThrowHelper.TargetInstanceUnavailable();

                var raisedIntentMessageId = StoreRaisedIntentForTarget(
                    messageId,
                    fdc3InstanceId.ToString(),
                    intent,
                    context,
                    sourceFdc3InstanceId);

                var target = new AppMetadata()
                {
                    AppId = targetAppMetadata.AppId,
                    InstanceId = fdc3InstanceId.ToString(),
                    Name = targetAppMetadata.Name,
                    Version = targetAppMetadata.Version,
                    Title = targetAppMetadata.Title,
                    Tooltip = targetAppMetadata.Tooltip,
                    Description = targetAppMetadata.Description,
                    Icons = targetAppMetadata.Icons.Select(Icon.GetIcon),
                    Screenshots = targetAppMetadata.Screenshots.Select(Screenshot.GetScreenshot),
                    ResultType = targetAppMetadata.ResultType
                };

                return new()
                {
                    Response = RaiseIntentResponse.Success(raisedIntentMessageId, intent, target)
                };
            }
            catch (Fdc3DesktopAgentException exception)
            {
                _logger.LogError(exception, "Error while starting module.");

                return new()
                {
                    Response = RaiseIntentResponse.Failure(exception.Message),
                };
            }
        }
    }

    private async Task<RaiseIntentResolutionInvocation?> GetIntentResolutionResult(GetIntentResultRequest request)
    {
        RaiseIntentResolutionInvocation? resolution;
        while (
            !_raisedIntentResolutions.TryGetValue(new Guid(request.TargetAppIdentifier.InstanceId!), out var resolver)
            || !resolver.TryGetRaisedIntentResult(request.MessageId, request.Intent, out resolution)
            || (resolution.ResultChannelId == null
                && resolution.ResultChannelType == null
                && resolution.ResultContext == null
                && resolution.ResultVoid == null))
        {
            await Task.Delay(100);
        }

        return resolution;
    }

    private string StoreRaisedIntentForTarget(
        int messageId,
        string targetFdc3InstanceId,
        string intent,
        Context context,
        string sourceFdc3InstanceId)
    {
        var invocation = new RaiseIntentResolutionInvocation(
            raiseIntentMessageId: messageId,
            intent: intent,
            originFdc3InstanceId: sourceFdc3InstanceId,
            contextToHandle: context);

        _raisedIntentResolutions.AddOrUpdate(
            new(targetFdc3InstanceId),
            _ =>
            {
                var resolver = new RaisedIntentRequestHandler();
                resolver.AddRaiseIntentToHandle(invocation);
                return resolver;
            },
            (key, oldValue) => oldValue.AddRaiseIntentToHandle(invocation));

        return invocation.RaiseIntentMessageId;
    }

    //Publishing intent resolution request to the fdc3 clients, they will receive the message and start their intenthandler appropriately, and send a store request back to the backend.
    private Task<RaiseIntentResolutionMessage?> RaiseIntentResolution(
        string raisedIntentMessageId,
        string intent,
        Context context,
        string targetId,
        string sourceFdc3InstanceId)
    {
        if (_runningModules.TryGetValue(new(sourceFdc3InstanceId), out var sourceApp))
        {
            var sourceAppIdentifier = new AppIdentifier()
            {
                AppId = sourceApp.AppId,
                InstanceId = sourceFdc3InstanceId
            };

            return Task.FromResult<RaiseIntentResolutionMessage?>(
                new()
                {
                    Intent = intent,
                    TargetModuleInstanceId = targetId,
                    Request = new RaiseIntentResolutionRequest()
                    {
                        MessageId = raisedIntentMessageId,
                        Context = context,
                        ContextMetadata = new ContextMetadata()
                        {
                            Source = sourceAppIdentifier
                        }
                    }
                });
        }

        return Task.FromResult<RaiseIntentResolutionMessage?>(null);
    }

    private async Task<Dictionary<string, AppIntent>> GetAppIntentsByRequest(
        Func<Fdc3App, IEnumerable<IntentMetadata>?> selector,
        IAppIdentifier? targetAppIdentifier,
        bool selected)
    {
        var appIntents = new Dictionary<string, AppIntent>();

        if (targetAppIdentifier?.InstanceId == null)
        {
            appIntents = await GetAppIntentsFromAppDirectory(selector, targetAppIdentifier, appIntents);

            if (selected && appIntents.Count > 0)
            {
                return appIntents;
            }
        }

        appIntents = GetAppIntentsFromRunningModules(selector, targetAppIdentifier, appIntents);

        return appIntents;
    }

    private Dictionary<string, AppIntent> GetAppIntentsFromRunningModules(
        Func<Fdc3App, IEnumerable<IntentMetadata>?> selector,
        IAppIdentifier? targetAppIdentifier,
        Dictionary<string, AppIntent> appIntents)
    {
        foreach (var app in _runningModules)
        {
            if (targetAppIdentifier?.InstanceId != null
                && Guid.TryParse(targetAppIdentifier.InstanceId, out var instanceId)
                && instanceId != app.Key)
            {
                continue;
            }

            var intentMetadataCollection = selector(app.Value);

            if (intentMetadataCollection == null)
            {
                continue;
            }

            appIntents = GetAppIntentsFromIntentMetadataCollection(
                app.Value,
                app.Key.ToString(),
                intentMetadataCollection,
                appIntents);
        }

        return appIntents;
    }

    private async Task<Dictionary<string, AppIntent>> GetAppIntentsFromAppDirectory(
        Func<Fdc3App, IEnumerable<IntentMetadata>?> selector,
        IAppIdentifier? targetAppIdentifier,
        Dictionary<string, AppIntent> appIntents)
    {
        foreach (var app in await _appDirectory.GetApps())
        {
            if (targetAppIdentifier != null && targetAppIdentifier.AppId != app.AppId)
            {
                continue;
            }

            var intentMetadataCollection = selector(app);

            if (intentMetadataCollection == null)
            {
                continue;
            }

            appIntents = GetAppIntentsFromIntentMetadataCollection(app, null, intentMetadataCollection, appIntents);
        }

        return appIntents;
    }

    private Dictionary<string, AppIntent> GetAppIntentsFromIntentMetadataCollection(
        Fdc3App app,
        string? instanceId,
        IEnumerable<IntentMetadata> intentMetadataCollection,
        Dictionary<string, AppIntent> appIntents)
    {
        foreach (var intentMetadata in intentMetadataCollection)
        {
            var appMetadata =
                new AppMetadata()
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
                    ResultType = intentMetadata.ResultType
                };

            if (!appIntents.TryGetValue(intentMetadata.Name, out var appIntent))
            {
                appIntent = new AppIntent
                {
                    Intent = new Protocol.IntentMetadata
                        {Name = intentMetadata.Name, DisplayName = intentMetadata.DisplayName},
                    Apps = Enumerable.Empty<AppMetadata>()
                };

                appIntents.Add(intentMetadata.Name, appIntent);
            }

            appIntent.Apps = appIntent.Apps.Append(appMetadata);
        }

        return appIntents;
    }

    private Task RemoveModuleAsync(IModuleInstance instance)
    {
        var fdc3InstanceId = GetFdc3InstanceId(instance);
        if (!_runningModules.TryRemove(new(fdc3InstanceId), out _))
        {
            _logger.LogError($"Could not remove the closed window with instanceId: {fdc3InstanceId}.");
        }

        return Task.CompletedTask;
    }

    private async Task AddOrUpdateModuleAsync(IModuleInstance instance)
    {
        Fdc3App fdc3App;
        try
        {
            fdc3App = await _appDirectory.GetApp(instance.Manifest.Id);
        }
        catch (AppNotFoundException)
        {
            _logger.LogError($"Could not retrieve app: {instance.Manifest.Id} from AppDirectory.");
            return;
        }

        var fdc3InstanceId = GetFdc3InstanceId(instance);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        _runningModules.GetOrAdd(
            new(fdc3InstanceId),
            _ => fdc3App);
    }

    private bool IsFdc3StartedModule(IModuleInstance instance, out string instanceId)
    {
        instanceId = string.Empty;
        var fdc3InstanceId =
            instance.StartRequest.Parameters.FirstOrDefault(
                parameter => parameter.Key == Fdc3StartupParameters.Fdc3InstanceId);

        if (string.IsNullOrEmpty(fdc3InstanceId.Value))
        {
            return false;
        }

        instanceId = fdc3InstanceId.Value;
        return true;
    }

    private string GetFdc3InstanceId(IModuleInstance instance)
    {
        if (!IsFdc3StartedModule(instance, out var fdc3InstanceId))
        {
            var startupProperties = instance.GetProperties().FirstOrDefault(p => p is Fdc3StartupProperties);

            return startupProperties == null
                ? throw ThrowHelper.MissingFdc3InstanceId(instance.Manifest.Id)
                : ((Fdc3StartupProperties) startupProperties).InstanceId;
        }

        return fdc3InstanceId;
    }

    private async ValueTask SafeWaitAsync(IEnumerable<ValueTask> tasks)
    {
        foreach (var task in tasks)
        {
            try
            {
                await task;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "An exception was thrown while waiting for a task to finish.");
            }
        }
    }
}