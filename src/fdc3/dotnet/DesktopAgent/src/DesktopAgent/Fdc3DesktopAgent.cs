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
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Converters;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.DependencyInjection;
using MorganStanley.ComposeUI.Messaging;
using MorganStanley.ComposeUI.ModuleLoader;
using MorganStanley.Fdc3;
using MorganStanley.Fdc3.AppDirectory;
using MorganStanley.Fdc3.Context;
using IntentMetadata = MorganStanley.Fdc3.AppDirectory.IntentMetadata;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent;

internal class Fdc3DesktopAgent : IHostedService
{
    private readonly ILogger<Fdc3DesktopAgent> _logger;
    private readonly List<UserChannel> _userChannels = new();
    private readonly ILoggerFactory _loggerFactory;
    private readonly Fdc3DesktopAgentOptions _options;
    private readonly IMessageRouter _messageRouter;
    private readonly IAppDirectory _appDirectory;
    private readonly IModuleLoader _moduleLoader;
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<KeyValuePair<string, string>, Context>> _queuedContexts = new();
    private readonly ConcurrentDictionary<KeyValuePair<string, string>, IIntentResult> _intentResolutions = new();
    private readonly ConcurrentDictionary<Guid, string> _startRequests = new();
    private readonly ConcurrentDictionary<Guid, Fdc3App> _runningApplications = new();
    private readonly SemaphoreSlim _runningApplicationsLock = new(1, 1);
    private readonly SemaphoreSlim _subscriptionLock = new(1, 1);
    private readonly List<IDisposable> _subscriptions = new();

    private readonly JsonSerializerOptions _intentJsonSerializeOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new IntentResultJsonConverter(), new AppIntentJsonConverter() }
    };

    public Fdc3DesktopAgent(
        IAppDirectory appDirectory,
        IModuleLoader moduleLoader,
        IOptions<Fdc3DesktopAgentOptions> options,
        IMessageRouter messageRouter,
        ILoggerFactory? loggerFactory = null)
    {
        _appDirectory = appDirectory;
        _moduleLoader = moduleLoader;
        _options = options.Value;
        _messageRouter = messageRouter;
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _logger = _loggerFactory.CreateLogger<Fdc3DesktopAgent>() ?? NullLogger<Fdc3DesktopAgent>.Instance;
    }

    public async ValueTask AddUserChannel(string id)
    {
        var uc = new UserChannel(id, _messageRouter, _loggerFactory.CreateLogger<UserChannel>());
        await uc.Connect();
        _userChannels.Add(uc);
    }

    internal async ValueTask<MessageBuffer?> FindChannel(string endpoint, MessageBuffer? payload, MessageContext context)
    {
        var request = payload?.ReadJson<FindChannelRequest>();
        if (request?.ChannelType != ChannelType.User) return MessageBuffer.Factory.CreateJson(FindChannelResponse.Failure(ChannelError.NoChannelFound));
        return MessageBuffer.Factory.CreateJson(_userChannels.Any(x => x.Id == request.ChannelId) ? FindChannelResponse.Success : FindChannelResponse.Failure(ChannelError.NoChannelFound));
    }

    internal async ValueTask<MessageBuffer?> FindIntent(string endpoint, MessageBuffer? payload, MessageContext context)
    {
        try
        {
            await _runningApplicationsLock.WaitAsync();

            var request = payload?.ReadJson<FindIntentRequest>(_intentJsonSerializeOptions);
            if (request == null) return MessageBuffer.Factory.CreateJson(FindIntentResponse.Failure(ResolveError.IntentDeliveryFailed), _intentJsonSerializeOptions);

            Func<Fdc3App, IEnumerable<IntentMetadata>?> predicate = (fdc3App) =>
            {
                if (fdc3App.Interop?.Intents?.ListensFor == null || !fdc3App.Interop.Intents.ListensFor.TryGetValue(request.Intent!, out var intentMetadata)) return null;
                if (request.Context != null && (intentMetadata.Contexts == null || !intentMetadata.Contexts.Contains(request.Context.Type)) && request.Context?.Type != "fdc3.nothing") return null;
                if (request.ResultType != null && (intentMetadata.ResultType == null || !intentMetadata.ResultType.Contains(request.ResultType))) return null;
                return new[] { intentMetadata };
            };

            var appIntents = await GetAppIntentsByRequest(predicate, null);

            if (!appIntents.Any()) return MessageBuffer.Factory.CreateJson(FindIntentResponse.Failure(ResolveError.NoAppsFound), _intentJsonSerializeOptions);

            return MessageBuffer.Factory.CreateJson(appIntents.Count() > 1 ? FindIntentResponse.Failure(ResolveError.IntentDeliveryFailed) : FindIntentResponse.Success(appIntents.ElementAt(0)), _intentJsonSerializeOptions);
        }
        finally
        {
            _runningApplicationsLock.Release();
        }
    }

    internal async ValueTask<MessageBuffer?> FindIntentsByContext(string endpoint, MessageBuffer? payload, MessageContext context)
    {
        try
        {
            await _runningApplicationsLock.WaitAsync();

            var request = payload?.ReadJson<FindIntentsByContextRequest>(_intentJsonSerializeOptions);
            if (request == null) return MessageBuffer.Factory.CreateJson(FindIntentResponse.Failure(ResolveError.IntentDeliveryFailed), _intentJsonSerializeOptions);

            Func<Fdc3App, IEnumerable<IntentMetadata>?> predicate = (fdc3App) =>
            {
                var intentMetadatas = new List<IntentMetadata>();
                if (fdc3App.Interop?.Intents?.ListensFor?.Values != null)
                {
                    foreach (var intentMetadata in fdc3App.Interop.Intents.ListensFor.Values)
                    {
                        if (intentMetadata.Contexts == null || !intentMetadata.Contexts.Contains(request.Context?.Type) && request.Context?.Type != "fdc3.nothing") continue;
                        if (request.ResultType != null && (intentMetadata.ResultType == null || !intentMetadata.ResultType.Contains(request.ResultType))) continue;
                        intentMetadatas.Add(intentMetadata);
                    }
                }
                if (intentMetadatas.Any()) return intentMetadatas;
                return null;
            };

            var appIntents = await GetAppIntentsByRequest(predicate, null);

            return !appIntents.Any()
                ? MessageBuffer.Factory.CreateJson(FindIntentsByContextResponse.Failure(ResolveError.NoAppsFound), _intentJsonSerializeOptions)
                : MessageBuffer.Factory.CreateJson(FindIntentsByContextResponse.Success(appIntents), _intentJsonSerializeOptions);
        }
        finally
        {
            _runningApplicationsLock.Release();
        }
    }

    internal async ValueTask<MessageBuffer?> RaiseIntent(string endpoint, MessageBuffer? payload, MessageContext context)
    {
        try
        {
            await _runningApplicationsLock.WaitAsync();

            var request = payload?.ReadJson<RaiseIntentRequest>(_intentJsonSerializeOptions);

            if (request == null) return MessageBuffer.Factory.CreateJson(RaiseIntentResponse.Failure(ResolveError.IntentDeliveryFailed), _intentJsonSerializeOptions);
            if (!string.IsNullOrEmpty(request.Error)) return MessageBuffer.Factory.CreateJson(RaiseIntentResponse.Failure(request.Error), _intentJsonSerializeOptions); //UserCancelledError for example, maybe here we can return null and immediately reject on the ui

            Func<Fdc3App, IEnumerable<IntentMetadata>?> predicate = (fdc3App) =>
            {
                if (fdc3App.Interop?.Intents?.ListensFor == null || !fdc3App.Interop.Intents.ListensFor.TryGetValue(request.Intent!, out var intentMetadata)) return null;
                if (request.Context != null && (intentMetadata.Contexts == null || !intentMetadata.Contexts.Contains(request.Context.Type)) && request.Context?.Type != "fdc3.nothing") return null;
                if (request.AppIdentifier != null && (fdc3App.AppId != request.AppIdentifier.AppId)) return null;
                return new[] { intentMetadata };
            };

            var appIntents = await GetAppIntentsByRequest(predicate, request.AppIdentifier);

            //No intents were found which would have the right information to handle the raised intent
            if (!appIntents.Any()) return MessageBuffer.Factory.CreateJson(RaiseIntentResponse.Failure(ResolveError.NoAppsFound), _intentJsonSerializeOptions);

            //Here we have used a method, which could return multiple IAppIntents to multiple intents, this is for abstracting method to findIntent, etc.
            //Here we should get just one IAppIntent, as the intent field is obligated (at least fdc3.nothing)
            if (appIntents.Count() > 1) return MessageBuffer.Factory.CreateJson(FindIntentResponse.Failure(ResolveError.IntentDeliveryFailed), _intentJsonSerializeOptions);

            var appIntent = appIntents.ElementAt(0);

            if (appIntent.Apps.Count() == 1)
            {
                var response = await RaiseIntentToApplication(
                    request.Intent!,
                    request.InstanceId!,
                    appIntent.Apps.ElementAt(0),
                    request.Context!);

                //Handling errors from starting an application or sending context to the application.
                return MessageBuffer.Factory.CreateJson(response ??
                                                        //Here as we have just one app which can handle the intent we can explicitly select the version for it.
                                                        RaiseIntentResponse.Success(appIntent.Intent.Name, appIntent.Apps), _intentJsonSerializeOptions);
            }

            await CreateRaiseIntentServiceByInstanceIdAsync(request.InstanceId!);

            return MessageBuffer.Factory.CreateJson(RaiseIntentResponse.Success(appIntent.Intent.Name, appIntent.Apps), _intentJsonSerializeOptions);
        }
        finally
        {
            _runningApplicationsLock.Release();
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _messageRouter.RegisterServiceAsync(Fdc3Topic.FindChannel, FindChannel, cancellationToken: cancellationToken);
        await _messageRouter.RegisterServiceAsync(Fdc3Topic.FindIntent, FindIntent, cancellationToken: cancellationToken);
        await _messageRouter.RegisterServiceAsync(Fdc3Topic.FindIntentsByContext, FindIntentsByContext, cancellationToken: cancellationToken);
        await _messageRouter.RegisterServiceAsync(Fdc3Topic.RaiseIntent, RaiseIntent, cancellationToken: cancellationToken);
        await _messageRouter.RegisterServiceAsync(Fdc3Topic.GetIntentResult, GetIntentResult, cancellationToken: cancellationToken);
        await _messageRouter.RegisterServiceAsync(Fdc3Topic.SendIntentResult, StoreIntentResult, cancellationToken: cancellationToken);

        await SubscribeAsync();

        if (_options.ChannelId == null) return;

        await AddUserChannel(_options.ChannelId);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var tasks = _userChannels.Select(x => x.DisposeAsync()).ToArray();
        var unregisteringTasks = new ValueTask[]
        {
            _messageRouter.UnregisterServiceAsync(Fdc3Topic.FindChannel, cancellationToken),
            _messageRouter.UnregisterServiceAsync(Fdc3Topic.FindIntent, cancellationToken),
            _messageRouter.UnregisterServiceAsync(Fdc3Topic.RaiseIntent, cancellationToken),
            _messageRouter.UnregisterServiceAsync(Fdc3Topic.FindIntentsByContext, cancellationToken),
            _messageRouter.UnregisterServiceAsync(Fdc3Topic.GetIntentResult, cancellationToken),
            _messageRouter.UnregisterServiceAsync(Fdc3Topic.SendIntentResult, cancellationToken)
        };

        await DisposeSafeAsync(tasks);
        await DisposeSafeAsync(unregisteringTasks);
        await UnsubscribeSafeAsync();
        _runningApplicationsLock.Dispose();
    }

    //It is used in semaphore blocks
    private async Task<IEnumerable<IAppIntent>> GetAppIntentsByRequest(Func<Fdc3App, IEnumerable<IntentMetadata>?> predicate, IAppIdentifier? appIdentifier)
    {
        var appIntents = new ConcurrentDictionary<string, IAppIntent>();

        if (appIdentifier?.InstanceId == null)
        {
            foreach (var app in await _appDirectory.GetApps())
            {
                if (appIdentifier != null && appIdentifier.AppId != app.AppId) continue;
                var intentMetadatas = predicate(app);
                if (intentMetadatas == null) continue;

                foreach (var intentMetadata in intentMetadatas)
                {
                    var appMetadata =
                        new AppMetadata(
                            appId: app.AppId,
                            instanceId: null,
                            name: app.Name,
                            version: app.Version,
                            title: app.Title,
                            tooltip: app.ToolTip,
                            description: app.Description,
                            icons: app.Icons,
                            images: app.Screenshots,
                            resultType: intentMetadata?.ResultType);

                    appIntents.AddOrUpdate(
                        intentMetadata!.Name,
                        new AppIntent(intentMetadata, new List<IAppMetadata>() { appMetadata }),
                        (key, oldValue) => new AppIntent(intentMetadata, oldValue.Apps.Append(appMetadata)));
                }
            }
        }

        foreach (var app in _runningApplications)
        {
            if (appIdentifier?.InstanceId != null && new Guid(appIdentifier.InstanceId) != app.Key) continue;
            var intentMetadatas = predicate(app.Value);
            if (intentMetadatas == null) continue;

            foreach (var intentMetadata in intentMetadatas)
            {
                var appMetadata = new AppMetadata(
                    appId: app.Value.AppId,
                    instanceId: app.Key.ToString(),
                    name: app.Value.Name,
                    version: app.Value.Version,
                    title: app.Value.Title,
                    tooltip: app.Value.ToolTip,
                    description: app.Value.Description,
                    icons: app.Value.Icons,
                    images: app.Value.Screenshots,
                    resultType: intentMetadata?.ResultType);

                appIntents.AddOrUpdate(
                    intentMetadata!.Name,
                    new AppIntent(intentMetadata, new List<IAppMetadata>() { appMetadata }),
                    (key, oldValue) => new AppIntent(intentMetadata, oldValue.Apps.Append(appMetadata)));
            }
        }

        return appIntents.Values;
    }

    //Here we have a specific application which should either start or we should send a intent resolution request
    private async ValueTask<RaiseIntentResponse?> RaiseIntentToApplication(
        string intent,
        string sourceInstanceId,
        IAppIdentifier appIdentifier,
        Context context)
    {
        if (!string.IsNullOrEmpty(appIdentifier.InstanceId)) //which means that the app is running
        {
            //Getting the source from the running application
            if (_runningApplications.TryGetValue(new Guid(appIdentifier.InstanceId), out _))
            {
                if (!_runningApplications.TryGetValue(new Guid(sourceInstanceId), out var source))
                    return RaiseIntentResponse.Failure("Could not resolve the sourceId for raising intent.");
                
                await RaiseIntentResolution(intent, context, appIdentifier.InstanceId, new AppIdentifier(source.AppId, sourceInstanceId));
                return null;
            }
            else return RaiseIntentResponse.Failure(ResolveError.TargetInstanceUnavailable);
        }
        else //the application is currently not running, we have selected an app which should start
        {
            try
            {
                //Starting application, as it is not started.
                var instance = await _moduleLoader.StartModule(new StartRequest(appIdentifier.AppId));

                if (instance is not null)
                {
                    if (_runningApplications.TryGetValue(new Guid(sourceInstanceId), out var app))
                    {
                        await RaiseIntentResolution(intent, context, instance.InstanceId.ToString(), new AppIdentifier(app.AppId, sourceInstanceId));
                    }
                    else
                    {
                        // This is the step where we queue the context to send the intentListener the context we want to handle by the application,
                        // if the application start, then we wil handle it when we get te LifetimeEvent from the ModuleLoader, that it has been started.                    
                        _queuedContexts.AddOrUpdate(
                            instance.InstanceId,
                            _ =>
                            {
                                var contexts = new ConcurrentDictionary<KeyValuePair<string, string>, Context>();
                                contexts.GetOrAdd(new KeyValuePair<string, string>(intent, sourceInstanceId), context);

                                return contexts;
                            },
                            (key, oldValue) =>
                            {
                                if (!oldValue.TryAdd(new KeyValuePair<string, string>(intent, sourceInstanceId), context))
                                    _logger.LogError($"Could not update the {nameof(_queuedContexts)} for raiseIntent command with intent: {intent}, origin: {sourceInstanceId}.");

                                return oldValue;
                            });
                    }
                }
            }
            catch (Exception exception)
            {
                return RaiseIntentResponse.Failure(exception.ToString());
            }
        }

        return null;
    }

    //Publishing intent resolution request to the fdc3 clients, they will receive the message and start their intenthandler appropriately, and send a store request back to the backend.
    private async Task RaiseIntentResolution(string intent, Context context, string targetId, IAppIdentifier sourceAppIdentifier)
    {
        var intentResolutionRequest = JsonSerializer.Serialize(
            new RaiseIntentResolutionRequest()
            {
                Context = context,
                ContextMetadata = new ContextMetadata(sourceAppIdentifier)
            });

        await _messageRouter.PublishAsync(Fdc3Topic.RaiseIntentResolution(intent, targetId), intentResolutionRequest);
    }

    //This is for creating service to a topic which handles the result of the RESOLVER UI
    private async Task CreateRaiseIntentServiceByInstanceIdAsync(string originInstanceId)
    {
        await _messageRouter.RegisterServiceAsync(Fdc3Topic.RaiseIntentResolution(originInstanceId), RaiseIntentById);
    }

    //This is for handling the clients answer with selecting the right app from the ResolverUI 
    internal async ValueTask<MessageBuffer?> RaiseIntentById(string endpoint, MessageBuffer? payload, MessageContext context)
    {
        try
        {
            var request = payload?.ReadJson<RaiseIntentRequest>(_intentJsonSerializeOptions);
            if (request == null) return MessageBuffer.Factory.CreateJson(RaiseIntentResponse.Failure(ResolveError.IntentDeliveryFailed), _intentJsonSerializeOptions);

            var fdc3App = await _appDirectory.GetApp(request?.AppIdentifier!.AppId!);

            if (!string.IsNullOrEmpty(request?.Error)) return MessageBuffer.Factory.CreateJson(RaiseIntentResponse.Failure(ResolveError.UserCancelledResolution));

            if (fdc3App == default) return MessageBuffer.Factory.CreateJson(RaiseIntentResponse.Failure(ResolveError.TargetAppUnavailable), _intentJsonSerializeOptions);

            var response = await RaiseIntentToApplication(
                request!.Intent!,
                request!.InstanceId!,
                request!.AppIdentifier!,
                request!.Context!);

            return MessageBuffer.Factory.CreateJson(response ?? RaiseIntentResponse.Success(request.Intent!), _intentJsonSerializeOptions);
        }
        catch (Exception exception)
        {
            _logger.LogError($"Exception occurred while resolving raiseIntent by id request: {exception}");
            return MessageBuffer.Factory.CreateJson(RaiseIntentResponse.Failure(ResolveError.ResolverUnavailable), _intentJsonSerializeOptions);
        }
        finally
        {
            //Unregistering this endpoint as it resolved the raising intent to a specific application after communicating with the ResolverUI 
            await _messageRouter.UnregisterServiceAsync(endpoint);
        }
    }

    internal async ValueTask<MessageBuffer?> StoreIntentResult(string endpoint, MessageBuffer? payload, MessageContext context)
    {
        var request = payload?.ReadJson<StoreIntentResultRequest>(_intentJsonSerializeOptions);
        if (request == null) return MessageBuffer.Factory.CreateJson(StoreIntentResultResponse.Failure(ResolveError.IntentDeliveryFailed), _intentJsonSerializeOptions);
        if (request.TargetInstanceId == null || request.Intent == null || request.IntentResult == default) return MessageBuffer.Factory.CreateJson(StoreIntentResultResponse.Failure(ResolveError.IntentDeliveryFailed), _intentJsonSerializeOptions);

        _intentResolutions.AddOrUpdate(
            new(request.TargetInstanceId!, request.Intent!),
            _ => request.IntentResult!,
            (key, oldValue) => request.IntentResult!);

        return MessageBuffer.Factory.CreateJson(StoreIntentResultResponse.Success(), _intentJsonSerializeOptions);
    }

    internal async ValueTask<MessageBuffer?> GetIntentResult(string endpoint, MessageBuffer? payload, MessageContext context)
    {
        var request = payload?.ReadJson<GetIntentResultRequest>(_intentJsonSerializeOptions);
        if (request == null) return MessageBuffer.Factory.CreateJson(GetIntentResultResponse.Failure(ResolveError.IntentDeliveryFailed), _intentJsonSerializeOptions);
        if (request.Source?.InstanceId == null || request.Intent == null) return MessageBuffer.Factory.CreateJson(GetIntentResultResponse.Failure(ResolveError.IntentDeliveryFailed), _intentJsonSerializeOptions);

        return MessageBuffer.Factory.CreateJson(
            _intentResolutions.TryGetValue(
                new(request.Source.InstanceId, request.Intent), out var intentResult)
                ? GetIntentResultResponse.Success(intentResult)
                : GetIntentResultResponse.Failure(ResolveError.IntentDeliveryFailed), 
            _intentJsonSerializeOptions);
    }

    internal async Task SubscribeAsync()
    {
        var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        try
        {
            await _subscriptionLock.WaitAsync(cancellationTokenSource.Token);
            
            var subscription = _moduleLoader.LifetimeEvents.Select(lifetimeEvent => 
                Observable.FromAsync(async () => await SubscribeToApplicationLifetimeEventsAsync(lifetimeEvent)))
                .Concat()
                .Subscribe();

            _subscriptions.Add(subscription);
        }
        finally
        {
            _subscriptionLock.Release();
        }
    }

    private async Task SubscribeToApplicationLifetimeEventsAsync(LifetimeEvent lifetimeEvent)
    {
        switch (lifetimeEvent.EventType)
        {
            case LifetimeEventType.Starting:
                _startRequests.AddOrUpdate(
                    lifetimeEvent.Instance.InstanceId,
                    lifetimeEvent.Instance.Manifest.Id,
                    (key, oldValue) => lifetimeEvent.Instance.Manifest.Id);
                break;

            case LifetimeEventType.Started:
                await AddOrUpdateApplicationAsync(lifetimeEvent.Instance.InstanceId);
                break;

            case LifetimeEventType.Stopped:
                await RemoveApplication(lifetimeEvent.Instance.InstanceId);
                break;
        }
    }

    private async Task RemoveApplication(Guid instanceId)
    {
        try
        {
            await _runningApplicationsLock.WaitAsync();
            if (!_runningApplications.TryRemove(instanceId, out var appId))
                _logger.LogError($"Could not remove the closed window with instanceId: {instanceId}, and appId: {appId}.");
        }
        finally
        {
            _runningApplicationsLock.Release();
        }
    }

    private async Task AddOrUpdateApplicationAsync(Guid instanceId)
    {
        try
        {
            await _runningApplicationsLock.WaitAsync();

            if (_startRequests.TryRemove(instanceId, out var appId))
            {
                var fdc3App = await _appDirectory.GetApp(appId);
                if (fdc3App == null)
                {
                    _logger.LogError($"Could not retrieve app: {appId} from AppDirectory.");
                    return;
                }

                _runningApplications.AddOrUpdate(
                    instanceId,
                    _ => fdc3App,
                    (key, oldValue) => oldValue = fdc3App);

                //We collect the related items for the started application, to handle by the intenthandler
                if (_queuedContexts.TryRemove(instanceId!, out var waitingContexts))
                {
                    foreach (var context in waitingContexts)
                    {
                        var appIdentifier = new AppIdentifier(appId, context.Key.Value);
                        await RaiseIntentResolution(context.Key.Key, context.Value, instanceId.ToString(), appIdentifier);
                    }
                }
            }
        }
        finally
        {
            _runningApplicationsLock.Release();
        }
    }

    private async ValueTask DisposeSafeAsync(IEnumerable<ValueTask> tasks)
    {
        foreach (var task in tasks)
        {
            try
            {
                await task;
            }
            catch (Exception exception)
            {
                _logger.LogWarning($"Could not dispose task: {task.IsCompleted}. Exception: {exception}");
            }
        }
    }

    private async Task UnsubscribeSafeAsync()
    {
        var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await _subscriptionLock.WaitAsync(cancellationTokenSource.Token);
        var subscriptions = _subscriptions.AsEnumerable().Reverse().ToArray();
        _subscriptions.Clear();
        _subscriptionLock.Release();

        foreach (var subscription in subscriptions)
        {
            try
            {
                subscription.Dispose();
            }
            catch (Exception exception)
            {
                _logger.LogWarning($"Could not dispose task: {subscription}. Exception: {exception}");
            }
        }
        _subscriptionLock.Dispose();
    }
}
