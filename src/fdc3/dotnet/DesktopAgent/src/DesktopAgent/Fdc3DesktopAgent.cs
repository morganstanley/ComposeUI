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
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Exceptions;
using MorganStanley.ComposeUI.Messaging;
using MorganStanley.ComposeUI.ModuleLoader;
using MorganStanley.Fdc3;
using MorganStanley.Fdc3.AppDirectory;
using MorganStanley.Fdc3.Context;
using Nito.AsyncEx;
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
    private readonly SemaphoreSlim _runningApplicationsLock = new(1);
    private readonly ConcurrentDictionary<Guid, Fdc3App> _runningApplications = new();
    private readonly AsyncLock _mutex = new();
    private readonly ConcurrentDictionary<Guid, RaisedIntentResolver> _raisedIntentResolutions = new();
    private readonly SemaphoreSlim _subscriptionLock = new(1);
    private readonly List<IAsyncDisposable> _subscriptions = new();

    private readonly JsonSerializerOptions _intentJsonSerializeOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new IAppIdentifierJsonConverter(),
            new IAppMetadataJsonConverter(),
            new IIntentMetadataJsonConverter()
        }
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

    internal ValueTask<MessageBuffer?> FindChannel(string endpoint, MessageBuffer? payload, MessageContext context)
    {
        var request = payload?.ReadJson<FindChannelRequest>();
        if (request?.ChannelType != ChannelType.User) return ValueTask.FromResult<MessageBuffer?>(MessageBuffer.Factory.CreateJson(FindChannelResponse.Failure(ChannelError.NoChannelFound)));
        return ValueTask.FromResult<MessageBuffer?>(MessageBuffer.Factory.CreateJson(_userChannels.Any(x => x.Id == request.ChannelId) ? FindChannelResponse.Success : FindChannelResponse.Failure(ChannelError.NoChannelFound)));
    }

    internal async ValueTask<MessageBuffer?> FindIntent(string endpoint, MessageBuffer? payload, MessageContext context)
    {
        try
        {
            await _runningApplicationsLock.LockAsync();
            var request = payload?.ReadJson<FindIntentRequest>(_intentJsonSerializeOptions);
            if (request == null) return MessageBuffer.Factory.CreateJson(FindIntentResponse.Failure(ResolveError.IntentDeliveryFailed), _intentJsonSerializeOptions);

            Func<Fdc3App, IEnumerable<IntentMetadata>?> selector = (fdc3App) =>
            {
                if (fdc3App.Interop?.Intents?.ListensFor == null || !fdc3App.Interop.Intents.ListensFor.TryGetValue(request.Intent!, out var intentMetadata)) return null;
                if (request.Context != null && (intentMetadata.Contexts == null || !intentMetadata.Contexts.Contains(request.Context.Type)) && request.Context?.Type != "fdc3.nothing") return null;
                if (request.ResultType != null && (intentMetadata.ResultType == null || !intentMetadata.ResultType.Contains(request.ResultType))) return null;
                return new[] { intentMetadata };
            };

            var appIntents = await GetAppIntentsByRequest(selector, null, false);

            if (!appIntents.Any()) return MessageBuffer.Factory.CreateJson(FindIntentResponse.Failure(ResolveError.NoAppsFound), _intentJsonSerializeOptions);

            return MessageBuffer.Factory.CreateJson(appIntents.Count() > 1
                ? FindIntentResponse.Failure(ResolveError.IntentDeliveryFailed)
                : FindIntentResponse.Success(appIntents.ElementAt(0)), _intentJsonSerializeOptions);
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
            await _runningApplicationsLock.LockAsync();
            var request = payload?.ReadJson<FindIntentsByContextRequest>(_intentJsonSerializeOptions);
            if (request == null) return MessageBuffer.Factory.CreateJson(FindIntentResponse.Failure(ResolveError.IntentDeliveryFailed), _intentJsonSerializeOptions);

            Func<Fdc3App, IEnumerable<IntentMetadata>?> selector = (fdc3App) =>
            {
                var intentMetadataCollection = new List<IntentMetadata>();
                if (fdc3App.Interop?.Intents?.ListensFor?.Values != null)
                {
                    foreach (var intentMetadata in fdc3App.Interop.Intents.ListensFor.Values)
                    {
                        if (intentMetadata.Contexts == null || !intentMetadata.Contexts.Contains(request.Context?.Type) && request.Context?.Type != "fdc3.nothing") continue;
                        if (request.ResultType != null && (intentMetadata.ResultType == null || !intentMetadata.ResultType.Contains(request.ResultType))) continue;
                        intentMetadataCollection.Add(intentMetadata);
                    }
                }
                if (intentMetadataCollection.Any()) return intentMetadataCollection;
                return null;
            };

            var appIntents = await GetAppIntentsByRequest(selector, null, false);

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
            await _runningApplicationsLock.LockAsync();
            var request = payload?.ReadJson<RaiseIntentRequest>(_intentJsonSerializeOptions);

            if (request == null) return MessageBuffer.Factory.CreateJson(RaiseIntentResponse.Failure(ResolveError.IntentDeliveryFailed), _intentJsonSerializeOptions);

            //UserCancelledError for example
            if (!string.IsNullOrEmpty(request.Error)) return MessageBuffer.Factory.CreateJson(RaiseIntentResponse.Failure(request.Error), _intentJsonSerializeOptions);

            Func<Fdc3App, IEnumerable<IntentMetadata>?> selector = (fdc3App) =>
            {
                if (fdc3App.Interop?.Intents?.ListensFor == null || !fdc3App.Interop.Intents.ListensFor.TryGetValue(request.Intent!, out var intentMetadata)) return null;
                if (request.Context != null && (intentMetadata.Contexts == null || !intentMetadata.Contexts.Contains(request.Context.Type)) && request.Context?.Type != "fdc3.nothing") return null;
                if (request.AppIdentifier != null && (fdc3App.AppId != request.AppIdentifier.AppId)) return null;
                return new[] { intentMetadata };
            };

            var appIntents = await GetAppIntentsByRequest(selector, request.AppIdentifier, request.Selected);

            //No intents were found which would have the right information to handle the raised intent
            if (!appIntents.Any()) return MessageBuffer.Factory.CreateJson(RaiseIntentResponse.Failure(ResolveError.NoAppsFound), _intentJsonSerializeOptions);

            //Here we have used a method, which could return multiple IAppIntents to multiple intents, this is for abstracting method to findIntent, findIntentsByContext, etc.
            //Here we should get just one IAppIntent, as the intent field is required (at least fdc3.nothing)
            if (appIntents.Count() > 1) return MessageBuffer.Factory.CreateJson(FindIntentResponse.Failure(ResolveError.IntentDeliveryFailed), _intentJsonSerializeOptions);

            var appIntent = appIntents.First();

            if (appIntent.Apps.Count() == 1)
            {
                var response = await RaiseIntentToApplication(
                    request.RaiseIntentMessageId,
                    appIntent.Apps.ElementAt(0),
                    request.Intent,
                    request.Context,
                    request.Fdc3InstanceId);

                //Handling errors from starting an application/successfully getting the created fdc3InstanceId, or sending context to the application.
                return MessageBuffer.Factory.CreateJson(response ??
                                                        //Here as we have just one app which can handle the intent we can explicitly select the version for it.
                                                        RaiseIntentResponse.Success(appIntent), _intentJsonSerializeOptions);
            }

            return MessageBuffer.Factory.CreateJson(RaiseIntentResponse.Success(appIntent), _intentJsonSerializeOptions);
        }
        catch (Fdc3DesktopAgentException)
        {
            throw;
        }
        finally
        {
            _runningApplicationsLock.Release();
        }
    }

    internal async ValueTask<MessageBuffer?> AddIntentListener(string endpoint, MessageBuffer? payload, MessageContext context)
    {
        using (await _mutex.LockAsync())
        {
            var request = payload?.ReadJson<AddIntentListenerRequest>(_intentJsonSerializeOptions);
            if (request == null) return MessageBuffer.Factory.CreateJson(AddIntentListenerResponse.Failure(Fdc3DesktopAgentErrors.PayloadNull), _intentJsonSerializeOptions);

            switch (request.State)
            {
                case SubscribeState.Subscribe:
                    if (_raisedIntentResolutions.TryGetValue(new(request.Fdc3InstanceId), out var resolver))
                    {
                        resolver.AddIntentListener(request.Intent);

                        foreach (var raisedIntent in resolver.RaiseIntentResolutions)
                        {
                            await RaiseIntentResolution(raisedIntent.Intent, raisedIntent.Context, request.Fdc3InstanceId, raisedIntent.OriginFdc3InstanceId);
                        }

                        return MessageBuffer.Factory.CreateJson(AddIntentListenerResponse.SubscribeSuccess(), _intentJsonSerializeOptions);
                    }
                    else
                    {
                        var createdResolver = _raisedIntentResolutions.GetOrAdd(
                            new(request.Fdc3InstanceId),
                            new RaisedIntentResolver());

                        createdResolver.AddIntentListener(request.Intent);

                        return MessageBuffer.Factory.CreateJson(AddIntentListenerResponse.SubscribeSuccess(), _intentJsonSerializeOptions);
                    }

                case SubscribeState.Unsubscribe:

                    if (_raisedIntentResolutions.TryGetValue(new(request.Fdc3InstanceId), out var resolverToRemove))
                    {
                        resolverToRemove.RemoveIntentListener(request.Intent);
                        return MessageBuffer.Factory.CreateJson(AddIntentListenerResponse.UnsubscribeSuccess(), _intentJsonSerializeOptions);
                    }

                    break;
            }

            return MessageBuffer.Factory.CreateJson(AddIntentListenerResponse.Failure(Fdc3DesktopAgentErrors.MissingId));
        }
    }

    internal async ValueTask<MessageBuffer?> StoreIntentResult(string endpoint, MessageBuffer? payload, MessageContext context)
    {
        using (await _mutex.LockAsync())
        {
            var request = payload?.ReadJson<StoreIntentResultRequest>(_intentJsonSerializeOptions);
            if (request == null) return MessageBuffer.Factory.CreateJson(StoreIntentResultResponse.Failure(ResolveError.IntentDeliveryFailed), _intentJsonSerializeOptions);
            if (request.TargetFdc3InstanceId == null || request.Intent == null) return MessageBuffer.Factory.CreateJson(StoreIntentResultResponse.Failure(ResolveError.IntentDeliveryFailed), _intentJsonSerializeOptions);

            _raisedIntentResolutions.AddOrUpdate(
                new Guid(request.OriginFdc3InstanceId),
                _ => throw ThrowHelper.MissingAppFromRaisedIntentInvocationsException(request.OriginFdc3InstanceId),
                (key, oldValue) => oldValue.AddIntentResult(
                    intent: request.Intent,
                    channelId: request.ChannelId,
                    channelType: request.ChannelType,
                    context: request.Context,
                    errorResult: request.ErrorResult));

            return MessageBuffer.Factory.CreateJson(StoreIntentResultResponse.Success(), _intentJsonSerializeOptions);
        }
    }

    internal async ValueTask<MessageBuffer?> GetIntentResult(string endpoint, MessageBuffer? payload, MessageContext context)
    {
        using (await _mutex.LockAsync())
        {
            var request = payload?.ReadJson<GetIntentResultRequest>(_intentJsonSerializeOptions);
            if (request == null) return MessageBuffer.Factory.CreateJson(GetIntentResultResponse.Failure(ResolveError.IntentDeliveryFailed), _intentJsonSerializeOptions);
            if (request.TargetAppIdentifier?.InstanceId == null || request.Intent == null) return MessageBuffer.Factory.CreateJson(GetIntentResultResponse.Failure(ResolveError.IntentDeliveryFailed), _intentJsonSerializeOptions);

            if (_raisedIntentResolutions.TryGetValue(new Guid(request.TargetAppIdentifier.InstanceId), out var registeredFdc3App))
            {
                if (registeredFdc3App.TryGetRaisedIntentResult(request.Intent, out var raiseIntentInvocation))
                {
                    return MessageBuffer.Factory.CreateJson(
                        GetIntentResultResponse.Success(
                            raiseIntentInvocation.ResultChannelId,
                            raiseIntentInvocation.ResultChannelType,
                            raiseIntentInvocation.ResultContext,
                            raiseIntentInvocation.ErrorResult),
                        _intentJsonSerializeOptions);
                }
            }

            return MessageBuffer.Factory.CreateJson(GetIntentResultResponse.Failure(ResolveError.IntentDeliveryFailed), _intentJsonSerializeOptions);
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
        await _messageRouter.RegisterServiceAsync(Fdc3Topic.AddIntentListener, AddIntentListener, cancellationToken: cancellationToken);

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
            _messageRouter.UnregisterServiceAsync(Fdc3Topic.SendIntentResult, cancellationToken),
            _messageRouter.UnregisterServiceAsync(Fdc3Topic.AddIntentListener, cancellationToken)
    };

        await DisposeSafeAsync(tasks);
        await DisposeSafeAsync(unregisteringTasks);
        UnsubscribeSafe();
    }

    internal async Task SubscribeAsync()
    {
        try
        {
            await _subscriptionLock.LockAsync();
            var observable = _moduleLoader.LifetimeEvents.ToAsyncObservable();
            var subscription = await observable.SubscribeAsync(async (lifetimeEvent) =>
            {
                switch (lifetimeEvent)
                {
                    case LifetimeEvent.Stopped:
                        await RemoveApplication(lifetimeEvent.Instance);
                        break;

                    case LifetimeEvent.Started:
                        await AddOrUpdateApplicationAsync(lifetimeEvent.Instance);
                        break;
                }
            });

            _subscriptions.Add(subscription);
        }
        finally
        {
            _subscriptionLock.Release();
        }
    }

    //Here we have a specific application which should either start or we should send a intent resolution request
    private async ValueTask<RaiseIntentResponse?> RaiseIntentToApplication(
        int messageId,
        IAppMetadata targetAppIdentifier,
        string intent,
        Context context,
        string sourceFdc3InstanceId)
    {
        using (await _mutex.LockAsync())
        {
            if (!string.IsNullOrEmpty(targetAppIdentifier.InstanceId))
            {
                StoreRaisedIntentForTarget(messageId, targetAppIdentifier.InstanceId, intent, context, sourceFdc3InstanceId);

                if (!_raisedIntentResolutions.TryGetValue(new(targetAppIdentifier.InstanceId), out var registeredFdc3App))
                    return RaiseIntentResponse.Failure(ResolveError.TargetInstanceUnavailable);

                if (registeredFdc3App.IsIntentListenerRegistered(intent))
                {
                    await RaiseIntentResolution(intent, context, targetAppIdentifier.InstanceId, sourceFdc3InstanceId);
                }
            }
            else
            {
                try
                {
                    var fdc3InstanceId = await StartModule(messageId, targetAppIdentifier, intent, context, sourceFdc3InstanceId);
                    if (fdc3InstanceId != null)
                    {
                        var target = new AppMetadata(
                            targetAppIdentifier.AppId,
                            fdc3InstanceId,
                            targetAppIdentifier.Name,
                            targetAppIdentifier.Version,
                            targetAppIdentifier.Title,
                            targetAppIdentifier.Tooltip,
                            targetAppIdentifier.Description,
                            targetAppIdentifier.Icons,
                            targetAppIdentifier.Screenshots,
                            targetAppIdentifier.ResultType);

                        return RaiseIntentResponse.Success(intent, target);
                    }
                }
                catch (Fdc3DesktopAgentException exception)
                {
                    _logger.LogError("Error while starting module.", exception);
                    throw;
                }
            }

            return null;
        }
    }

    private void StoreRaisedIntentForTarget(
        int messageId,
        string targetFdc3InstanceId,
        string intent,
        Context context,
        string sourceFdc3InstanceId)
    {
        _raisedIntentResolutions.AddOrUpdate(
                        new(targetFdc3InstanceId),
                        _ =>
                        {
                            var resolver = new RaisedIntentResolver();
                            resolver.AddRaiseIntentToHandle(
                                new RaiseIntentResolutionInvocation(
                                    raiseIntentMessageId: messageId,
                                    intent: intent,
                                    originFdc3InstanceId: sourceFdc3InstanceId,
                                    contextToHandle: context));
                            return resolver;
                        },
                        (key, oldValue) => oldValue.AddRaiseIntentToHandle(
                            new RaiseIntentResolutionInvocation(
                                raiseIntentMessageId: messageId,
                                intent: intent,
                                originFdc3InstanceId: sourceFdc3InstanceId,
                                contextToHandle: context)));
    }

    private async Task<string> StartModule(
        int messageId,
        IAppMetadata targetAppIdentifier,
        string intent,
        Context context,
        string sourceFdc3InstanceId)
    {
        Guid fdc3InstanceId;
        try
        {
            fdc3InstanceId = Guid.NewGuid();
            var moduleInstance = await _moduleLoader.StartModule(
                new StartRequest(
                    targetAppIdentifier.AppId,
                    new List<KeyValuePair<string, string>>()
                    {
                            { new KeyValuePair<string, string>(Fdc3StartupProperties.Fdc3InstanceId, fdc3InstanceId.ToString()) }
                    }));

            if (moduleInstance == null) throw ThrowHelper.TargetInstanceUnavailable();

            if (_raisedIntentResolutions.TryGetValue(fdc3InstanceId, out var resolver)
                && resolver.IsIntentListenerRegistered(intent))
            {
                await RaiseIntentResolution(intent, context, fdc3InstanceId.ToString(), sourceFdc3InstanceId);
            }
            else
            {
                StoreRaisedIntentForTarget(messageId, fdc3InstanceId.ToString(), intent, context, sourceFdc3InstanceId);
            }
        }
        catch (Exception exception)
        {
            throw ThrowHelper.StartModuleFailure(exception);
        }

        return fdc3InstanceId.ToString();
    }

    //Publishing intent resolution request to the fdc3 clients, they will receive the message and start their intenthandler appropriately, and send a store request back to the backend.
    private async Task RaiseIntentResolution(string intent, Context context, string targetId, string sourceFdc3InstanceId)
    {
        if (_runningApplications.TryGetValue(new(sourceFdc3InstanceId), out var sourceApp))
        {
            var sourceAppIdentifier = new AppIdentifier(sourceApp.AppId, sourceFdc3InstanceId);
            var intentResolutionRequest = JsonSerializer.Serialize(
                new RaiseIntentResolutionRequest(context, new ContextMetadata(sourceAppIdentifier)), _intentJsonSerializeOptions);

            await _messageRouter.PublishAsync(Fdc3Topic.RaiseIntentResolution(intent, targetId), intentResolutionRequest);
        }
    }

    //It is used in semaphore blocks
    private async Task<IEnumerable<AppIntent>> GetAppIntentsByRequest(
        Func<Fdc3App, IEnumerable<IntentMetadata>?> selector,
        AppIdentifier? targetAppIdentifier,
        bool selected)
    {
        var appIntents = new Dictionary<string, AppIntent>();

        if (targetAppIdentifier?.InstanceId == null)
        {
            foreach (var app in await _appDirectory.GetApps())
            {
                if (targetAppIdentifier != null && targetAppIdentifier.AppId != app.AppId) continue;
                var intentMetadataCollection = selector(app);
                if (intentMetadataCollection == null) continue;

                foreach (var intentMetadata in intentMetadataCollection)
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

                    if (appIntents.ContainsKey(intentMetadata!.Name))
                    {
                        appIntents[intentMetadata.Name] = new AppIntent(intentMetadata, appIntents[intentMetadata.Name].Apps.Append(appMetadata));
                    }
                    else
                    {
                        appIntents.Add(intentMetadata.Name, new AppIntent(intentMetadata, new List<AppMetadata>() { appMetadata }));
                    }
                }

                if (selected && appIntents.Count > 0) return appIntents.Values;
            }
        }

        foreach (var app in _runningApplications)
        {
            if (targetAppIdentifier?.InstanceId != null && new Guid(targetAppIdentifier.InstanceId) != app.Key) continue;
            var intentMetadataCollection = selector(app.Value);
            if (intentMetadataCollection == null) continue;

            foreach (var intentMetadata in intentMetadataCollection)
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

                if (appIntents.ContainsKey(intentMetadata!.Name))
                {
                    appIntents[intentMetadata.Name] = new AppIntent(intentMetadata, appIntents[intentMetadata.Name].Apps.Append(appMetadata));
                }
                else
                {
                    appIntents.Add(intentMetadata.Name, new AppIntent(intentMetadata, new List<AppMetadata>() { appMetadata }));
                }

                if (selected && appIntents.Count > 0) return appIntents.Values;
            }
        }

        return appIntents.Values;
    }

    private Task RemoveApplication(IModuleInstance instance)
    {
        lock (_runningApplicationsLock)
        {
            var fdc3InstanceId = GetFdc3InstanceId(instance);
            if (!_runningApplications.TryRemove(new(fdc3InstanceId), out _))
                _logger.LogError($"Could not remove the closed window with instanceId: {fdc3InstanceId}.");

            return Task.CompletedTask;
        }
    }

    private async Task AddOrUpdateApplicationAsync(IModuleInstance instance)
    {
        var fdc3App = await _appDirectory.GetApp(instance.Manifest.Id);
        if (fdc3App == null)
        {
            _logger.LogError($"Could not retrieve app: {instance.Manifest.Id} from AppDirectory.");
            return;
        }

        var fdc3InstanceId = GetFdc3InstanceId(instance);

        lock (_runningApplicationsLock)
        {
            //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
            _runningApplications.GetOrAdd(
                    new(fdc3InstanceId),
                    _ => fdc3App);
        }
    }

    private string GetFdc3InstanceId(IModuleInstance instance)
    {
        var fdc3InstanceId = instance.StartRequest.Parameters.FirstOrDefault(parameter => parameter.Key == Fdc3StartupProperties.Fdc3InstanceId);
        if (string.IsNullOrEmpty(fdc3InstanceId.Value))
        {
            if (instance.GetProperties().FirstOrDefault(property => property is Fdc3StartupProperties) is not Fdc3StartupProperties fdc3StartupProperties)
                throw ThrowHelper.MissingFdc3InstanceIdException(instance.Manifest.Id);

            return fdc3StartupProperties.InstanceId;
        }
        return fdc3InstanceId.Value;
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

    private async void UnsubscribeSafe()
    {
        try 
        {
            await _subscriptionLock.LockAsync();
            var subscriptions = _subscriptions.AsEnumerable().Reverse().ToArray();
            _subscriptions.Clear();

            foreach (var subscription in subscriptions)
            {
                try
                {
                    await subscription.DisposeAsync();
                }
                catch (Exception exception)
                {
                    _logger.LogWarning($"Could not dispose task: {subscription}. Exception: {exception}");
                }
            }
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            _subscriptionLock.Release();
        }
    }

    private class RaiseIntentResolutionInvocation
    {
        public RaiseIntentResolutionInvocation(
            int raiseIntentMessageId,
            string intent,
            string originFdc3InstanceId,
            Context contextToHandle,
            Context? resultContext = null,
            string? resultChannelId = null,
            ChannelType? resultChannelType = null)
        {
            RaiseIntentMessageId = $"{raiseIntentMessageId}-{Guid.NewGuid()}";
            Intent = intent;
            OriginFdc3InstanceId = originFdc3InstanceId;
            Context = contextToHandle;
            ResultContext = resultContext;
            ResultChannelId = resultChannelId;
            ResultChannelType = resultChannelType;
        }

        public string RaiseIntentMessageId { get; }
        public string Intent { get; }
        public string OriginFdc3InstanceId { get; }
        public Context Context { get; }
        public Context? ResultContext { get; set; }
        public string? ResultChannelId { get; set; }
        public ChannelType? ResultChannelType { get; set; }
        public string? ErrorResult { get; set; }
    }

    private class RaisedIntentResolver
    {
        private readonly object _intentListenersLock = new();
        private readonly object _raiseIntentInvocationsLock = new();
        private readonly List<string> _registeredIntentListeners = new();
        private readonly List<RaiseIntentResolutionInvocation> _raiseIntentResolutions = new();
        public IEnumerable<string> IntentListeners => _registeredIntentListeners;
        public IEnumerable<RaiseIntentResolutionInvocation> RaiseIntentResolutions => _raiseIntentResolutions;

        public RaisedIntentResolver AddIntentListener(string intent)
        {
            lock (_intentListenersLock)
            {
                _registeredIntentListeners.Add(intent);
                return this;
            }
        }

        public RaisedIntentResolver RemoveIntentListener(string intent)
        {
            lock (_intentListenersLock)
            {
                _registeredIntentListeners.Remove(intent);
                return this;
            }
        }

        public bool IsIntentListenerRegistered(string intent)
        {
            lock (_intentListenersLock)
            {
                return _registeredIntentListeners.Contains(intent);
            }
        }

        public RaisedIntentResolver AddRaiseIntentToHandle(RaiseIntentResolutionInvocation raiseIntentInvocation)
        {
            lock (_raiseIntentInvocationsLock)
            {
                _raiseIntentResolutions.Add(raiseIntentInvocation);
                return this;
            }
        }

        public RaisedIntentResolver RemoveRaisedIntent(RaiseIntentResolutionInvocation raiseIntentInvocation)
        {
            lock (_raiseIntentInvocationsLock)
            {
                _raiseIntentResolutions.Remove(raiseIntentInvocation);
                return this;
            }
        }

        public RaisedIntentResolver AddIntentResult(
            string intent,
            string? channelId = null,
            ChannelType? channelType = null,
            Context? context = null,
            string? errorResult = null)
        {
            lock (_raiseIntentInvocationsLock)
            {
                var raisedIntentInvocations = _raiseIntentResolutions.Where(raisedIntentToHandle => raisedIntentToHandle.Intent == intent);
                if (raisedIntentInvocations.Count() == 1)
                {
                    var raisedIntentInvocation = raisedIntentInvocations.First();
                    raisedIntentInvocation.ResultChannelId = channelId;
                    raisedIntentInvocation.ResultChannelType = channelType;
                    raisedIntentInvocation.ResultContext = context;
                    raisedIntentInvocation.ErrorResult = errorResult;
                }
                else if (raisedIntentInvocations.Count() > 1) throw ThrowHelper.MultipleIntentRegisteredToAnAppInstance(intent);

                return this;
            }
        }

        public bool TryGetRaisedIntentResult(string intent, out RaiseIntentResolutionInvocation raiseIntentInvocation)
        {
            lock (_raiseIntentInvocationsLock)
            {
                var raiseIntentInvocations = _raiseIntentResolutions.Where(raiseIntentInvocation => raiseIntentInvocation.Intent == intent);
                if (raiseIntentInvocations.Any())
                {
                    if (raiseIntentInvocations.Count() > 1) throw ThrowHelper.MultipleIntentRegisteredToAnAppInstance(intent);
                    raiseIntentInvocation = raiseIntentInvocations.First();
                    return true;
                }

                raiseIntentInvocation = null;
                return false;
            }
        }
    }
}