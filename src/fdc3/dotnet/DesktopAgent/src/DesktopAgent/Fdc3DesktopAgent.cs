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
    private readonly SemaphoreSlim _runningApplicationsSemaphoreLock = new(1);
    private readonly ConcurrentDictionary<Guid, Fdc3App> _runningApplications = new();
    private readonly AsyncLock _mutex = new();
    private readonly ConcurrentDictionary<Guid, RaisedIntentResolver> _raisedIntentResolutions = new();
    private readonly ConcurrentBag<IModuleInstance> _fdc3ModuleInstances = new();
    private readonly AsyncLock _fdc3ModuleInstancesMutex = new();
    private IAsyncDisposable? _subscription;

    private readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new IAppMetadataJsonConverter(),
            new IIntentMetadataJsonConverter(),
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
        return ValueTask.FromResult<MessageBuffer?>(MessageBuffer.Factory.CreateJson(_userChannels.Any(x => x.Id == request.ChannelId)
            ? FindChannelResponse.Success
            : FindChannelResponse.Failure(ChannelError.NoChannelFound)));
    }

    internal async ValueTask<MessageBuffer?> HandleFindIntent(string endpoint, MessageBuffer? payload, MessageContext context)
    {
        var request = payload?.ReadJson<FindIntentRequest>(_jsonSerializerOptions);
        if (request == null) return MessageBuffer.Factory.CreateJson(FindIntentResponse.Failure(ResolveError.IntentDeliveryFailed), _jsonSerializerOptions);

        await _runningApplicationsSemaphoreLock.LockAsync();

        try
        {
            var response = await FindIntent(request);
            return MessageBuffer.Factory.CreateJson(response, _jsonSerializerOptions);
        }
        finally
        {
            _runningApplicationsSemaphoreLock.Release();
        }
    }

    internal async ValueTask<MessageBuffer?> HandleFindIntentsByContext(string endpoint, MessageBuffer? payload, MessageContext context)
    {
        var request = payload?.ReadJson<FindIntentsByContextRequest>(_jsonSerializerOptions);
        if (request == null) return MessageBuffer.Factory.CreateJson(FindIntentResponse.Failure(ResolveError.IntentDeliveryFailed), _jsonSerializerOptions);

        await _runningApplicationsSemaphoreLock.LockAsync();

        try
        {
            var response = await FindIntentsByContext(request);
            return MessageBuffer.Factory.CreateJson(response, _jsonSerializerOptions);
        }
        finally
        {
            _runningApplicationsSemaphoreLock.Release();
        }
    }

    internal async ValueTask<MessageBuffer?> HandleRaiseIntent(string endpoint, MessageBuffer? payload, MessageContext context)
    {
        var request = payload?.ReadJson<RaiseIntentRequest>(_jsonSerializerOptions);
        if (request == null) return MessageBuffer.Factory.CreateJson(RaiseIntentResponse.Failure(ResolveError.IntentDeliveryFailed), _jsonSerializerOptions);
        if (!string.IsNullOrEmpty(request.Error)) return MessageBuffer.Factory.CreateJson(RaiseIntentResponse.Failure(request.Error), _jsonSerializerOptions);

        await _runningApplicationsSemaphoreLock.LockAsync();

        try
        {
            var response = await RaiseIntent(request);
            return MessageBuffer.Factory.CreateJson(response, _jsonSerializerOptions);
        }
        catch (Fdc3DesktopAgentException)
        {
            throw;
        }
        finally
        {
            _runningApplicationsSemaphoreLock.Release();

            using (await _fdc3ModuleInstancesMutex.LockAsync())
            {
                if (_fdc3ModuleInstances.TryTake(out var module))
                {
                    await AddAppplication(module);
                }
            }
        }
    }

    internal async ValueTask<MessageBuffer?> HandleAddIntentListener(string endpoint, MessageBuffer? payload, MessageContext context)
    {
        var request = payload?.ReadJson<AddIntentListenerRequest>(_jsonSerializerOptions);
        if (request == null) return MessageBuffer.Factory.CreateJson(AddIntentListenerResponse.Failure(Fdc3DesktopAgentErrors.PayloadNull), _jsonSerializerOptions);

        using (await _mutex.LockAsync())
        {
            var response = await AddIntentListener(request);
            return MessageBuffer.Factory.CreateJson(response, _jsonSerializerOptions);
        }
    }

    internal async ValueTask<MessageBuffer?> HandleStoreIntentResult(string endpoint, MessageBuffer? payload, MessageContext context)
    {
        var request = payload?.ReadJson<StoreIntentResultRequest>(_jsonSerializerOptions);
        if (request == null) return MessageBuffer.Factory.CreateJson(StoreIntentResultResponse.Failure(ResolveError.IntentDeliveryFailed), _jsonSerializerOptions);
        if (request.TargetFdc3InstanceId == null || request.Intent == null) return MessageBuffer.Factory.CreateJson(StoreIntentResultResponse.Failure(ResolveError.IntentDeliveryFailed), _jsonSerializerOptions);

        using (await _mutex.LockAsync())
        {
            var response = await StoreIntentResult(request);
            return MessageBuffer.Factory.CreateJson(response, _jsonSerializerOptions);
        }
    }

    internal async ValueTask<MessageBuffer?> HandleGetIntentResult(string endpoint, MessageBuffer? payload, MessageContext context)
    {
        var request = payload?.ReadJson<GetIntentResultRequest>(_jsonSerializerOptions);
        if (request == null) return MessageBuffer.Factory.CreateJson(GetIntentResultResponse.Failure(ResolveError.IntentDeliveryFailed), _jsonSerializerOptions);
        if (request.TargetAppIdentifier?.InstanceId == null || request.Intent == null || request.MessageId == null) return MessageBuffer.Factory.CreateJson(GetIntentResultResponse.Failure(ResolveError.IntentDeliveryFailed), _jsonSerializerOptions);

        var response = await GetIntentResult(request);
        return MessageBuffer.Factory.CreateJson(response, _jsonSerializerOptions);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _messageRouter.RegisterServiceAsync(Fdc3Topic.FindChannel, FindChannel, cancellationToken: cancellationToken);
        await _messageRouter.RegisterServiceAsync(Fdc3Topic.FindIntent, HandleFindIntent, cancellationToken: cancellationToken);
        await _messageRouter.RegisterServiceAsync(Fdc3Topic.FindIntentsByContext, HandleFindIntentsByContext, cancellationToken: cancellationToken);
        await _messageRouter.RegisterServiceAsync(Fdc3Topic.RaiseIntent, HandleRaiseIntent, cancellationToken: cancellationToken);
        await _messageRouter.RegisterServiceAsync(Fdc3Topic.GetIntentResult, HandleGetIntentResult, cancellationToken: cancellationToken);
        await _messageRouter.RegisterServiceAsync(Fdc3Topic.SendIntentResult, HandleStoreIntentResult, cancellationToken: cancellationToken);
        await _messageRouter.RegisterServiceAsync(Fdc3Topic.AddIntentListener, HandleAddIntentListener, cancellationToken: cancellationToken);

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
        if (_subscription != null) await _subscription.DisposeAsync();
    }

    internal async Task SubscribeAsync()
    {
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

        _subscription = subscription;
    }

    private async ValueTask<FindIntentResponse> FindIntent(FindIntentRequest request)
    {
        Func<Fdc3App, IEnumerable<IntentMetadata>?> selector = (fdc3App) =>
        {
            if (fdc3App.Interop?.Intents?.ListensFor == null || !fdc3App.Interop.Intents.ListensFor.TryGetValue(request.Intent!, out var intentMetadata)) return null;
            if (request.Context != null && (intentMetadata.Contexts == null || !intentMetadata.Contexts.Contains(request.Context.Type)) && request.Context?.Type != "fdc3.nothing") return null;
            if (request.ResultType != null && (intentMetadata.ResultType == null || !intentMetadata.ResultType.Contains(request.ResultType))) return null;
            return new[] { intentMetadata };
        };

        var appIntents = await GetAppIntentsByRequest(selector, null, false);

        if (!appIntents.Any()) return FindIntentResponse.Failure(ResolveError.NoAppsFound);

        return appIntents.Count() > 1
            ? FindIntentResponse.Failure(ResolveError.IntentDeliveryFailed)
            : FindIntentResponse.Success(appIntents.ElementAt(0));
    }

    private async ValueTask<FindIntentsByContextResponse> FindIntentsByContext(FindIntentsByContextRequest request)
    {
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
            ? FindIntentsByContextResponse.Failure(ResolveError.NoAppsFound)
            : FindIntentsByContextResponse.Success(appIntents);
    }

    private async ValueTask<GetIntentResultResponse> GetIntentResult(GetIntentResultRequest request)
    {
        var cancellationTokenSource = new CancellationTokenSource();
        try
        {
            var task = Task.Delay(_options.Timeout, cancellationTokenSource.Token);

            if (await Task.WhenAny(
                IsIntentResolved(request), 
                task) != task)
            {
                cancellationTokenSource.Cancel();
                var resolvedIntent = await IsIntentResolved(request);
                return GetIntentResultResponse.Success(
                            resolvedIntent!.ResultChannelId,
                            resolvedIntent!.ResultChannelType,
                            resolvedIntent!.ResultContext,
                            resolvedIntent!.ResultVoid);
            }
        }
        catch (Fdc3DesktopAgentException exception)
        {
            _logger.LogError(exception, "Error occurred while getResult() was called from the client.");
        }

        return GetIntentResultResponse.Failure(ResolveError.IntentDeliveryFailed);
    }

    private async Task<RaiseIntentResolutionInvocation?> IsIntentResolved(GetIntentResultRequest request)
    {
        RaiseIntentResolutionInvocation? resolution = null;
        while (
                !_raisedIntentResolutions.TryGetValue(new Guid(request.TargetAppIdentifier.InstanceId!), out var resolver)
                || !resolver.TryGetRaisedIntentResult(request.MessageId, request.Intent, out resolution)
                || (resolution.ResultChannelId == null && resolution.ResultChannelType == null && resolution.ResultContext == null && resolution.ResultVoid == null))
        {
            await Task.Delay(1);
        }

        return resolution;
    }

    private ValueTask<StoreIntentResultResponse> StoreIntentResult(StoreIntentResultRequest request)
    {
        _raisedIntentResolutions.AddOrUpdate(
                new Guid(request.OriginFdc3InstanceId),
                _ => throw ThrowHelper.MissingAppFromRaisedIntentInvocationsException(request.OriginFdc3InstanceId),
                (key, oldValue) => oldValue.AddIntentResult(
                    messageId: request.MessageId,
                    intent: request.Intent,
                    channelId: request.ChannelId,
                    channelType: request.ChannelType,
                    context: request.Context,
                    voidResult: request.VoidResult));

        return ValueTask.FromResult(StoreIntentResultResponse.Success());
    }

    private async ValueTask<AddIntentListenerResponse> AddIntentListener(AddIntentListenerRequest request)
    {
        switch (request.State)
        {
            case SubscribeState.Subscribe:
                if (_raisedIntentResolutions.TryGetValue(new(request.Fdc3InstanceId), out var resolver))
                {
                    resolver.AddIntentListener(request.Intent);

                    foreach (var raisedIntent in resolver.RaiseIntentResolutions)
                    {
                        await RaiseIntentResolution(
                            raisedIntent.RaiseIntentMessageId,
                            raisedIntent.Intent,
                            raisedIntent.Context,
                            request.Fdc3InstanceId,
                            raisedIntent.OriginFdc3InstanceId);
                    }

                    return AddIntentListenerResponse.SubscribeSuccess();
                }
                else
                {
                    var createdResolver = _raisedIntentResolutions.GetOrAdd(
                        new(request.Fdc3InstanceId),
                        new RaisedIntentResolver());

                    createdResolver.AddIntentListener(request.Intent);

                    return AddIntentListenerResponse.SubscribeSuccess();
                }

            case SubscribeState.Unsubscribe:

                if (_raisedIntentResolutions.TryGetValue(new(request.Fdc3InstanceId), out var resolverToRemove))
                {
                    resolverToRemove.RemoveIntentListener(request.Intent);
                    return AddIntentListenerResponse.UnsubscribeSuccess();
                }

                break;
        }

        return AddIntentListenerResponse.Failure(Fdc3DesktopAgentErrors.MissingId);
    }

    private async ValueTask<RaiseIntentResponse> RaiseIntent(RaiseIntentRequest request)
    {
        Func<Fdc3App, IEnumerable<IntentMetadata>?> selector = (fdc3App) =>
        {
            if (fdc3App.Interop?.Intents?.ListensFor == null || !fdc3App.Interop.Intents.ListensFor.TryGetValue(request.Intent!, out var intentMetadata)) return null;
            if (request.Context != null && (intentMetadata.Contexts == null || !intentMetadata.Contexts.Contains(request.Context.Type)) && request.Context?.Type != "fdc3.nothing") return null;
            if (request.TargetAppIdentifier != null && (fdc3App.AppId != request.TargetAppIdentifier.AppId)) return null;
            return new[] { intentMetadata };
        };

        var appIntents = await GetAppIntentsByRequest(selector, request.TargetAppIdentifier, request.Selected);

        //No intents were found which would have the right information to handle the raised intent
        if (!appIntents.Any()) return RaiseIntentResponse.Failure(ResolveError.NoAppsFound);

        //Here we have used a method, which could return multiple IAppIntents to multiple intents, this is for abstracting method to findIntent, findIntentsByContext, etc.
        //Here we should get just one IAppIntent, as the intent field is required (at least fdc3.nothing)
        if (appIntents.Count() > 1) return RaiseIntentResponse.Failure(ResolveError.IntentDeliveryFailed);

        var appIntent = appIntents.First();

        if (appIntent.Apps.Count() == 1)
        {
            var response = await RaiseIntentToApplication(
                request.MessageId,
                appIntent.Apps.ElementAt(0),
                request.Intent,
                request.Context,
                request.Fdc3InstanceId);

            return response;
        }

        //Multiple app inside one AppIntent, we pass back the messageId as string.
        return RaiseIntentResponse.Success(request.MessageId.ToString(), appIntent);
    }

    //Here we have a specific application which should either start or we should send a intent resolution request
    private async ValueTask<RaiseIntentResponse> RaiseIntentToApplication(
        int messageId,
        IAppMetadata targetAppMetadata,
        string intent,
        Context context,
        string sourceFdc3InstanceId)
    {
        using (await _mutex.LockAsync())
        {
            if (!string.IsNullOrEmpty(targetAppMetadata.InstanceId))
            {
                var raisedIntentMessageId = StoreRaisedIntentForTarget(messageId, targetAppMetadata.InstanceId, intent, context, sourceFdc3InstanceId);

                if (!_raisedIntentResolutions.TryGetValue(new(targetAppMetadata.InstanceId), out var registeredFdc3App))
                    return RaiseIntentResponse.Failure(ResolveError.TargetInstanceUnavailable);

                if (registeredFdc3App.IsIntentListenerRegistered(intent))
                {
                    await RaiseIntentResolution(raisedIntentMessageId, intent, context, targetAppMetadata.InstanceId, sourceFdc3InstanceId);
                }

                return RaiseIntentResponse.Success(raisedIntentMessageId, intent, targetAppMetadata);
            }
            else
            {
                try
                {
                    var fdc3InstanceId = Guid.NewGuid();
                    var moduleInstance = await _moduleLoader.StartModule(
                        new StartRequest(
                            targetAppMetadata.AppId, //TODO: possible remove some identifier like @"fdc3."
                            new List<KeyValuePair<string, string>>()
                            {
                            { new(Fdc3StartupParameters.Fdc3InstanceId, fdc3InstanceId.ToString()) }
                            }));

                    if (moduleInstance == null) throw ThrowHelper.TargetInstanceUnavailable();

                    var raisedIntentMessageId = StoreRaisedIntentForTarget(messageId, fdc3InstanceId.ToString(), intent, context, sourceFdc3InstanceId);

                    var target = new AppMetadata(
                        targetAppMetadata.AppId,
                        fdc3InstanceId.ToString(),
                        targetAppMetadata.Name,
                        targetAppMetadata.Version,
                        targetAppMetadata.Title,
                        targetAppMetadata.Tooltip,
                        targetAppMetadata.Description,
                        targetAppMetadata.Icons,
                        targetAppMetadata.Screenshots,
                        targetAppMetadata.ResultType);

                    return RaiseIntentResponse.Success(raisedIntentMessageId, intent, target);
                }
                catch (Fdc3DesktopAgentException exception)
                {
                    _logger.LogError(exception, "Error while starting module.");
                    throw;
                }
            }
        }
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
                            var resolver = new RaisedIntentResolver();
                            resolver.AddRaiseIntentToHandle(invocation);
                            return resolver;
                        },
                        (key, oldValue) => oldValue.AddRaiseIntentToHandle(invocation));

        return invocation.RaiseIntentMessageId;
    }

    //Publishing intent resolution request to the fdc3 clients, they will receive the message and start their intenthandler appropriately, and send a store request back to the backend.
    private async Task RaiseIntentResolution(string raisedIntentMessageId, string intent, Context context, string targetId, string sourceFdc3InstanceId)
    {
        if (_runningApplications.TryGetValue(new(sourceFdc3InstanceId), out var sourceApp))
        {
            var sourceAppIdentifier = new AppIdentifier(sourceApp.AppId, sourceFdc3InstanceId);
            var intentResolutionRequest = MessageBuffer.Factory.CreateJson(new RaiseIntentResolutionRequest(raisedIntentMessageId, context, new ContextMetadata(sourceAppIdentifier)), _jsonSerializerOptions);

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

    private async Task RemoveApplication(IModuleInstance instance)
    {
        await _runningApplicationsSemaphoreLock.LockAsync();
        try
        {
            var fdc3InstanceId = GetFdc3InstanceId(instance);
            if (!_runningApplications.TryRemove(new(fdc3InstanceId), out _))
                _logger.LogError($"Could not remove the closed window with instanceId: {fdc3InstanceId}.");
        }
        finally
        {
            _runningApplicationsSemaphoreLock.Release();
        }
    }

    private async Task AddOrUpdateApplicationAsync(IModuleInstance instance)
    {
        if (IsFdc3StartedModule(instance, out _))
        {
            using (await _fdc3ModuleInstancesMutex.LockAsync())
            {
                _fdc3ModuleInstances.Add(instance);
            }
        }
        else
        {
            await AddAppplication(instance);
        }
    }

    private async Task AddAppplication(IModuleInstance instance)
    {
        var fdc3App = await _appDirectory.GetApp(instance.Manifest.Id);
        if (fdc3App == null)
        {
            _logger.LogError($"Could not retrieve app: {instance.Manifest.Id} from AppDirectory.");
            return;
        }

        var fdc3InstanceId = GetFdc3InstanceId(instance);

        await _runningApplicationsSemaphoreLock.LockAsync();
        try
        {
            //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
            _runningApplications.GetOrAdd(
                    new(fdc3InstanceId),
                    _ => fdc3App);
        }
        finally
        {
            _runningApplicationsSemaphoreLock.Release();
        }
    }

    private bool IsFdc3StartedModule(IModuleInstance instance, out string instanceId)
    {
        instanceId = string.Empty;
        var fdc3InstanceId = instance.StartRequest.Parameters.FirstOrDefault(parameter => parameter.Key == Fdc3StartupParameters.Fdc3InstanceId);
        if (string.IsNullOrEmpty(fdc3InstanceId.Value)) return false;
        instanceId = fdc3InstanceId.Value;
        return true;
    }

    private string GetFdc3InstanceId(IModuleInstance instance)
    {
        if (!IsFdc3StartedModule(instance, out var fdc3InstanceId))
        {
            if (instance.GetProperties().FirstOrDefault(property => property is Fdc3StartupProperties) is not Fdc3StartupProperties fdc3StartupProperties)
                throw ThrowHelper.MissingFdc3InstanceIdException(instance.Manifest.Id);

            return fdc3StartupProperties.InstanceId;
        }
        return fdc3InstanceId;
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

    private class RaiseIntentResolutionInvocation
    {
        public RaiseIntentResolutionInvocation(
            int raiseIntentMessageId,
            string intent,
            string originFdc3InstanceId,
            Context contextToHandle,
            Context? resultContext = null,
            string? resultChannelId = null,
            ChannelType? resultChannelType = null,
            string? resultVoid = null)
        {
            RaiseIntentMessageId = $"{raiseIntentMessageId}-{Guid.NewGuid()}";
            Intent = intent;
            OriginFdc3InstanceId = originFdc3InstanceId;
            Context = contextToHandle;
            ResultContext = resultContext;
            ResultChannelId = resultChannelId;
            ResultChannelType = resultChannelType;
            ResultVoid = resultVoid;
        }

        public string RaiseIntentMessageId { get; }
        public string Intent { get; }
        public string OriginFdc3InstanceId { get; }
        public Context Context { get; }
        public Context? ResultContext { get; set; }
        public string? ResultChannelId { get; set; }
        public ChannelType? ResultChannelType { get; set; }
        public string? ResultVoid { get; set; }
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

        public RaisedIntentResolver AddIntentResult(
            string messageId,
            string intent,
            string? channelId = null,
            ChannelType? channelType = null,
            Context? context = null,
            string? voidResult = null)
        {
            lock (_raiseIntentInvocationsLock)
            {
                var raisedIntentInvocations = _raiseIntentResolutions.Where(
                    raisedIntentToHandle => raisedIntentToHandle.RaiseIntentMessageId == messageId && raisedIntentToHandle.Intent == intent);

                if (raisedIntentInvocations.Count() == 1)
                {
                    var raisedIntentInvocation = raisedIntentInvocations.First();
                    raisedIntentInvocation.ResultChannelId = channelId;
                    raisedIntentInvocation.ResultChannelType = channelType;
                    raisedIntentInvocation.ResultContext = context;
                    raisedIntentInvocation.ResultVoid = voidResult;
                }
                else if (raisedIntentInvocations.Count() > 1) throw ThrowHelper.MultipleIntentRegisteredToAnAppInstance(intent);

                return this;
            }
        }

        public bool TryGetRaisedIntentResult(string messageId, string intent, out RaiseIntentResolutionInvocation raiseIntentInvocation)
        {
            lock (_raiseIntentInvocationsLock)
            {
                var raiseIntentInvocations = _raiseIntentResolutions.Where(raiseIntentInvocation => raiseIntentInvocation.RaiseIntentMessageId == messageId && raiseIntentInvocation.Intent == intent);
                if (raiseIntentInvocations.Any())
                {
                    if (raiseIntentInvocations.Count() > 1) throw ThrowHelper.MultipleIntentRegisteredToAnAppInstance(intent);
                    raiseIntentInvocation = raiseIntentInvocations.First();
                    return true;
                }

                throw ThrowHelper.MissingIntentForMessage(messageId, intent);
            }
        }
    }
}