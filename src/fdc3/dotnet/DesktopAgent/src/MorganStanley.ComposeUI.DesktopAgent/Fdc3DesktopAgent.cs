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
using Finos.Fdc3;
using Finos.Fdc3.AppDirectory;
using Finos.Fdc3.Context;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Channels;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.DependencyInjection;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Exceptions;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure.Internal;
using MorganStanley.ComposeUI.ModuleLoader;
using AppIdentifier = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.AppIdentifier;
using AppIntent = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.AppIntent;
using AppMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.AppMetadata;
using ContextMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.ContextMetadata;
using Icon = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.Icon;
using IntentMetadata = Finos.Fdc3.AppDirectory.IntentMetadata;
using Screenshot = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.Screenshot;
using AppChannel = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Channels.AppChannel;
using MorganStanley.ComposeUI.Messaging;
using System.Text.Json;
using System.IO.Abstractions;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Converters;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol;
using System.Text.Json.Serialization;
using DisplayMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.DisplayMetadata;
using ImplementationMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.ImplementationMetadata;
using Microsoft.VisualBasic;
using Constants = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure.Internal.Constants;
using FileSystem = System.IO.Abstractions.FileSystem;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent;

internal class Fdc3DesktopAgent : IFdc3DesktopAgentBridge
{
    private readonly ILogger<Fdc3DesktopAgent> _logger;
    private readonly IResolverUICommunicator _resolverUI;
    private readonly ConcurrentDictionary<string, UserChannel> _userChannels = new();
    private readonly ConcurrentDictionary<string, PrivateChannel> _privateChannels = new();
    private readonly ConcurrentDictionary<string, AppChannel> _appChannels = new();
    private readonly ILoggerFactory _loggerFactory;
    private readonly Fdc3DesktopAgentOptions _options;
    private readonly IAppDirectory _appDirectory;
    private readonly IModuleLoader _moduleLoader;
    private readonly ConcurrentDictionary<Guid, Fdc3App> _runningModules = new();
    private readonly ConcurrentDictionary<Guid, RaisedIntentRequestHandler> _raisedIntentResolutions = new();
    private readonly ConcurrentDictionary<StartRequest, TaskCompletionSource<IModuleInstance>> _pendingStartRequests = new();
    private IAsyncDisposable? _subscription;
    private readonly IFileSystem _fileSystem;
    private readonly HttpClient _httpClient = new();
    private ConcurrentDictionary<string, ChannelItem>? _userChannelSet;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new DisplayMetadataJsonConverter(), new JsonStringEnumConverter() }
    };

    public Fdc3DesktopAgent(
        IAppDirectory appDirectory,
        IModuleLoader moduleLoader,
        IOptions<Fdc3DesktopAgentOptions> options,
        IResolverUICommunicator resolverUI,
        IFileSystem? fileSystem = null,
        ILoggerFactory? loggerFactory = null)
    {
        _appDirectory = appDirectory;
        _moduleLoader = moduleLoader;
        _options = options.Value;
        _resolverUI = resolverUI;
        _fileSystem = fileSystem ?? new FileSystem();
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _logger = _loggerFactory.CreateLogger<Fdc3DesktopAgent>() ?? NullLogger<Fdc3DesktopAgent>.Instance;
    }

    public async ValueTask AddUserChannel(UserChannel userChannel)
    {
        //TODO: Decide if we need to check from the existing userchannel set if the id is contained
        //if (_userChannelSet != null && !_userChannelSet.TryGetValue(userChannel.Id, out _))
        //{
        //    return;
        //}

        if (!_userChannels.TryAdd(userChannel.Id, userChannel))
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug($"User channel with id: {userChannel.Id} cannot be added.");
            }

            return;
        }

        try
        {
            await userChannel.Connect();
        }
        catch (MessageRouterException exception)
        {
            if (!exception.Message.Contains("Duplicate endpoint"))
            {
                throw;
            }

            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(exception, $"Endpoint is already registered {userChannel.Id}.");
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, $"Exception thrown while executing {nameof(AddUserChannel)}.");
            _userChannels.TryRemove(userChannel.Id, out _);
            throw;
        }
    }

    public async ValueTask AddPrivateChannel(PrivateChannel privateChannel)
    {
        if (!_privateChannels.TryAdd(privateChannel.Id, privateChannel))
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug($"Private channel with id: {privateChannel.Id} cannot be added.");
            }

            return;
        }

        try
        {
            await privateChannel.Connect();
        }
        catch (MessageRouterException exception)
        {
            if (!exception.Message.Contains("Duplicate endpoint"))
            {
                throw;
            }

            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(exception, $"Endpoint is already registered {privateChannel.Id}.");
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, $"Exception thrown while executing {nameof(AddPrivateChannel)}.");
            _privateChannels.TryRemove(privateChannel.Id, out _);
            throw;
        }
    }

    public async ValueTask<CreateAppChannelResponse> AddAppChannel(
        AppChannel appChannel,
        string instanceId)
    {
        if (!_runningModules.TryGetValue(new Guid(instanceId), out _))
        {
            return CreateAppChannelResponse.Failed(ChannelError.CreationFailed);
        }

        if (!_appChannels.TryAdd(appChannel.Id, appChannel))
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug($"App channel with id: {appChannel.Id} cannot be added.");
            }

            return CreateAppChannelResponse.Created();
        }

        try
        {
            await appChannel.Connect();
            return CreateAppChannelResponse.Created();
        }
        catch (MessageRouterException exception)
        {
            if (!exception.Message.Contains("Duplicate endpoint"))
            {
                throw;
            }

            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(exception, $"Endpoint is already registered {appChannel.Id}.");
            }

            return CreateAppChannelResponse.Created();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, $"An exception was thrown while executing {nameof(AddAppChannel)}.");
            _appChannels.TryRemove(appChannel.Id, out _);
            return CreateAppChannelResponse.Failed(ChannelError.CreationFailed);
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var observable = _moduleLoader.LifetimeEvents.ToAsyncObservable();
        var subscription = await observable.SubscribeAsync(async lifetimeEvent =>
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
        _userChannelSet = await ReadUserChannelSet(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var userChannelDisposeTasks = _userChannels.Select(x => x.Value.DisposeAsync()).ToArray();
        var privateChannelDisposeTasks = _privateChannels.Select(x => x.Value.DisposeAsync()).ToArray();
        var appChannelDisposeTasks = _appChannels.Select(x => x.Value.DisposeAsync()).ToArray();

        await SafeWaitAsync(privateChannelDisposeTasks);
        await SafeWaitAsync(userChannelDisposeTasks);
        await SafeWaitAsync(appChannelDisposeTasks);

        if (_subscription != null)
        {
            _runningModules.Clear();
            await _subscription.DisposeAsync();
        }

        foreach (var pendingStartRequest in _pendingStartRequests)
        {
            pendingStartRequest.Value.TrySetCanceled();
        }

        _pendingStartRequests.Clear();
        _userChannels.Clear();
        _privateChannels.Clear();
        _appChannels.Clear();
        _httpClient.Dispose();
    }

    public bool FindChannel(string channelId, ChannelType channelType)
    {
        return channelType switch
        {
            ChannelType.User => _userChannels.TryGetValue(channelId, out _),
            ChannelType.Private => _privateChannels.TryGetValue(channelId, out _),
            ChannelType.App => _appChannels.TryGetValue(channelId, out _),
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
        Func<Fdc3App, Dictionary<string, AppIntent>, IEnumerable<KeyValuePair<string, IntentMetadata>>?> selector = (fdc3App, _) =>
        {
            if (fdc3App.Interop?.Intents?.ListensFor == null
                || !fdc3App.Interop.Intents.ListensFor.TryGetValue(request.Intent!, out var intentMetadata))
            {
                return null;
            }

            if (request.Context != null
                && (intentMetadata.Contexts == null || !intentMetadata.Contexts.Contains(request.Context.Type))
                && request.Context?.Type != ContextTypes.Nothing)
            {
                return null;
            }

            if (request.ResultType != null
                && (intentMetadata.ResultType == null || !intentMetadata.ResultType.Contains(request.ResultType)))
            {
                return null;
            }

            return new Dictionary<string, IntentMetadata> { { request.Intent, intentMetadata } };
        };

        var appIntents = await GetAppIntentsByRequest(selector, null);

        if (!appIntents.TryGetValue(request.Intent, out var appIntent))
        {
            return FindIntentResponse.Failure(ResolveError.NoAppsFound);
        }

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
        Func<Fdc3App, Dictionary<string, AppIntent>, IEnumerable<KeyValuePair<string, IntentMetadata>>?> selector = (fdc3App, _) =>
        {
            var intentMetadataCollection = new Dictionary<string, IntentMetadata>();
            if (fdc3App.Interop?.Intents?.ListensFor?.Values != null)
            {
                foreach (var intentMetadata in fdc3App.Interop.Intents.ListensFor)
                {
                    if (intentMetadata.Value.Contexts == null
                        || !intentMetadata.Value.Contexts.Contains(request.Context?.Type)
                        && request.Context?.Type != ContextTypes.Nothing)
                    {
                        continue;
                    }
                    if (request.ResultType != null
                        && (intentMetadata.Value.ResultType == null
                            || !intentMetadata.Value.ResultType.Contains(request.ResultType)))
                    {
                        continue;
                    }
                    intentMetadataCollection.Add(intentMetadata.Key, intentMetadata.Value);
                }
            }

            if (intentMetadataCollection.Any())
            {
                return intentMetadataCollection;
            }

            return null;
        };

        var appIntents = await GetAppIntentsByRequest(selector, null);

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

        if (request.TargetAppIdentifier?.InstanceId == null)
        {
            return GetIntentResultResponse.Failure(ResolveError.IntentDeliveryFailed);
        }

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
                        invocation => invocation.Intent == request.Intent && !invocation.IsResolved))
                    {
                        var resolution = await GetRaiseIntentResolutionMessage(
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

                var createdResolver = _raisedIntentResolutions.GetOrAdd(
                    new(request.Fdc3InstanceId),
                    new RaisedIntentRequestHandler(_loggerFactory.CreateLogger<RaisedIntentRequestHandler>()));

                createdResolver.AddIntentListener(request.Intent);

                return new()
                {
                    Response = IntentListenerResponse.SubscribeSuccess(),
                };

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

    public ValueTask<GetUserChannelsResponse> GetUserChannels(GetUserChannelsRequest? request)
    {
        if (request == null)
        {
            return ValueTask.FromResult(GetUserChannelsResponse.Failure(Fdc3DesktopAgentErrors.PayloadNull));
        }

        if (!Guid.TryParse(request.InstanceId, out var instanceId))
        {
            return ValueTask.FromResult(GetUserChannelsResponse.Failure(Fdc3DesktopAgentErrors.MissingId));
        }

        if (!_runningModules.TryGetValue(instanceId, out _))
        {
            return ValueTask.FromResult(GetUserChannelsResponse.Failure(ChannelError.AccessDenied));
        }

        var result = _userChannelSet?.Values;
        if (result == null)
        {
            return ValueTask.FromResult(GetUserChannelsResponse.Failure(Fdc3DesktopAgentErrors.NoUserChannelSetFound));
        }

        return ValueTask.FromResult(GetUserChannelsResponse.Success(result));
    }

    public async ValueTask<JoinUserChannelResponse?> JoinUserChannel(UserChannel channel, string instanceId)
    {
        if (!Guid.TryParse(instanceId, out var id) || !_runningModules.TryGetValue(id, out _))
        {
            return JoinUserChannelResponse.Failed(ChannelError.AccessDenied);
        }

        //TODO remove if regarding this
        ChannelItem? channelItem = null;
        if (_userChannelSet != null && !_userChannelSet.TryGetValue(channel.Id, out channelItem))
        {
            //TODO delete this if statement
            if (!_userChannels.TryGetValue(channel.Id, out _))
            {
                return JoinUserChannelResponse.Failed(ChannelError.CreationFailed);
            }
        }

        try
        {
            await AddUserChannel(channel);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, $"Exception is thrown while executing {nameof(JoinUserChannel)}.");
            return JoinUserChannelResponse.Failed(ChannelError.CreationFailed);
        }

        if (channelItem != null)
        {
            return JoinUserChannelResponse.Joined((DisplayMetadata)channelItem.DisplayMetadata);
        }

        return JoinUserChannelResponse.Joined();
    }

    public async ValueTask<GetInfoResponse> GetInfo(GetInfoRequest? request)
    {
        if (request == null)
        {
            return GetInfoResponse.Failure(Fdc3DesktopAgentErrors.PayloadNull);
        }

        if (request.AppIdentifier.InstanceId == null)
        {
            return GetInfoResponse.Failure(Fdc3DesktopAgentErrors.MissingId);
        }

        var result = await GetAppInfo(request.AppIdentifier);

        return result;
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

        //This function returns null, if the app could not be accepted based on the intent (required), context (optional in request), appIdentifier (optional in request)
        //else for consistency it will return a single element array containing the intentMetadata which is allowed by the request.
        Func<Fdc3App, Dictionary<string, AppIntent>, IEnumerable<KeyValuePair<string, IntentMetadata>>?> selector = (fdc3App, appIntents) =>
        {
            //If the user selects an application from the AppDirectory instead of the its running instance
            if (request.Selected && appIntents.TryGetValue(request.Intent, out var result) && result.Apps.Any())
            {
                return null;
            }

            if (fdc3App.Interop?.Intents?.ListensFor == null
                || !fdc3App.Interop.Intents.ListensFor.TryGetValue(request.Intent!, out var intentMetadata))
            {
                return null;
            }

            if (request.Context != null
                && (intentMetadata.Contexts == null || !intentMetadata.Contexts.Contains(request.Context.Type))
                && request.Context?.Type != ContextTypes.Nothing)
            {
                return null;
            }

            if (request.TargetAppIdentifier != null && (fdc3App.AppId != request.TargetAppIdentifier.AppId))
            {
                return null;
            }

            return new Dictionary<string, IntentMetadata> { { request.Intent, intentMetadata } };
        };

        var appIntents = await GetAppIntentsByRequest(selector, request.TargetAppIdentifier);

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
        var result = await WaitForResolverUIAsync(appIntent.Apps);

        if (result != null && result.Error == null)
        {
            return await RaiseIntentToApplication(
                request.MessageId,
                result.AppMetadata!,
                request.Intent,
                request.Context,
                request.Fdc3InstanceId);
        }

        if (result?.Error != null)
        {
            return new()
            {
                Response = RaiseIntentResponse.Failure(result.Error)
            };
        }

        return new()
        {
            Response = RaiseIntentResponse.Failure(ResolveError.UserCancelledResolution)
        };
    }

    private async Task<ResolverUIResponse?> WaitForResolverUIAsync(IEnumerable<AppMetadata> apps)
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));

        try
        {
            return await _resolverUI.SendResolverUIRequest(apps, cancellationTokenSource.Token);
        }
        catch (TimeoutException exception)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(exception, "MessageRouter didn't receive response from the ResolverUI.");
            }

            return new ResolverUIResponse()
            {
                Error = ResolveError.ResolverTimeout
            };
        }
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
            var raisedIntentMessageId = StoreRaisedIntentForTarget(messageId, targetAppMetadata.InstanceId, intent, context, sourceFdc3InstanceId);

            if (!_raisedIntentResolutions.TryGetValue(new(targetAppMetadata.InstanceId), out var registeredFdc3App))
            {
                return new()
                {
                    Response = RaiseIntentResponse.Failure(ResolveError.TargetInstanceUnavailable)
                };
            }

            if (registeredFdc3App.IsIntentListenerRegistered(intent))
            {
                resolution = await GetRaiseIntentResolutionMessage(raisedIntentMessageId, intent, context, targetAppMetadata.InstanceId, sourceFdc3InstanceId);
            }

            return new()
            {
                Response = RaiseIntentResponse.Success(raisedIntentMessageId, intent, targetAppMetadata),
                RaiseIntentResolutionMessages = resolution != null
                    ? [resolution]
                    : Enumerable.Empty<RaiseIntentResolutionMessage>()
            };
        }

        try
        {
            var fdc3InstanceId = Guid.NewGuid();
            var startRequest = new StartRequest(
                targetAppMetadata.AppId, //TODO: possible remove some identifier like @"fdc3."
                [
                    new(Fdc3StartupParameters.Fdc3InstanceId, fdc3InstanceId.ToString())
                ]);

            var taskCompletionSource = new TaskCompletionSource<IModuleInstance>();

            if (_pendingStartRequests.TryAdd(startRequest, taskCompletionSource))
            {
                var moduleInstance = await _moduleLoader.StartModule(startRequest);

                if (moduleInstance == null)
                {
                    var exception = ThrowHelper.TargetInstanceUnavailable();

                    if (!_pendingStartRequests.TryRemove(startRequest, out _))
                    {
                        _logger.LogWarning($"Could not remove {nameof(StartRequest)} from the pending requests. ModuleId: {startRequest.ModuleId}.");
                    }

                    taskCompletionSource.TrySetException(exception);
                }

                await taskCompletionSource.Task;
            }

            var raisedIntentMessageId = StoreRaisedIntentForTarget(messageId, fdc3InstanceId.ToString(), intent, context, sourceFdc3InstanceId);

            var target = new AppMetadata
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
                Response = RaiseIntentResponse.Failure(exception.ToString()),
            };
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
                var resolver = new RaisedIntentRequestHandler(_loggerFactory.CreateLogger<RaisedIntentRequestHandler>());
                resolver.AddRaiseIntentToHandle(invocation);
                return resolver;
            },
            (key, oldValue) => oldValue.AddRaiseIntentToHandle(invocation));

        return invocation.RaiseIntentMessageId;
    }

    //Publishing intent resolution request to the fdc3 clients, they will receive the message and start their IntentHandler appropriately, and send a store request back to the backend.
    private Task<RaiseIntentResolutionMessage?> GetRaiseIntentResolutionMessage(
        string raisedIntentMessageId,
        string intent,
        Context context,
        string targetId,
        string sourceFdc3InstanceId)
    {
        if (_runningModules.TryGetValue(new(sourceFdc3InstanceId), out var sourceApp))
        {
            var sourceAppIdentifier = new AppIdentifier
            {
                AppId = sourceApp.AppId,
                InstanceId = sourceFdc3InstanceId
            };

            return Task.FromResult<RaiseIntentResolutionMessage?>(
                new()
                {
                    Intent = intent,
                    TargetModuleInstanceId = targetId,
                    Request = new RaiseIntentResolutionRequest
                    {
                        MessageId = raisedIntentMessageId,
                        Context = context,
                        ContextMetadata = new ContextMetadata
                        {
                            Source = sourceAppIdentifier
                        }
                    }
                });
        }

        return Task.FromResult<RaiseIntentResolutionMessage?>(null);
    }

    private async Task<Dictionary<string, AppIntent>> GetAppIntentsByRequest(
        Func<Fdc3App, Dictionary<string, AppIntent>, IEnumerable<KeyValuePair<string, IntentMetadata>>?> selector,
        IAppIdentifier? targetAppIdentifier)
    {
        var appIntents = new Dictionary<string, AppIntent>();

        if (targetAppIdentifier?.InstanceId == null)
        {
            appIntents = await GetAppIntentsFromAppDirectory(selector, targetAppIdentifier, appIntents);
        }

        appIntents = GetAppIntentsFromRunningModules(selector, targetAppIdentifier, appIntents);

        return appIntents;
    }

    private async Task GetAppIntentsByRequest(
        Action<Fdc3App, string?> selector,
        IAppIdentifier? targetAppIdentifier)
    {
        if (targetAppIdentifier?.InstanceId == null)
        {
            await GetAppIntentsFromAppDirectory(selector, targetAppIdentifier);
        }

        GetAppIntentsFromRunningModules(selector, targetAppIdentifier);
    }

    private Dictionary<string, AppIntent> GetAppIntentsFromRunningModules(
        Func<Fdc3App, Dictionary<string, AppIntent>, IEnumerable<KeyValuePair<string, IntentMetadata>>?> selector,
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

            var intentMetadataCollection = selector(app.Value, appIntents);

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
        Func<Fdc3App, Dictionary<string, AppIntent>, IEnumerable<KeyValuePair<string, IntentMetadata>>?> selector,
        IAppIdentifier? targetAppIdentifier,
        Dictionary<string, AppIntent> appIntents)
    {
        foreach (var app in await _appDirectory.GetApps())
        {
            if (targetAppIdentifier != null && targetAppIdentifier.AppId != app.AppId)
            {
                continue;
            }

            var intentMetadataCollection = selector(app, appIntents);

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
        IEnumerable<KeyValuePair<string, IntentMetadata>> intentMetadataCollection,
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
                    ResultType = intentMetadata.Value.ResultType
                };

            if (!appIntents.TryGetValue(intentMetadata.Key, out var appIntent)) //Name is null
            {
                appIntent = new AppIntent
                {
                    Intent = new Protocol.IntentMetadata
                    { Name = intentMetadata.Key, DisplayName = intentMetadata.Value.DisplayName },
                    Apps = Enumerable.Empty<AppMetadata>()
                };

                appIntents.Add(intentMetadata.Key, appIntent);
            }

            appIntent.Apps = appIntent.Apps.Append(appMetadata);
        }

        return appIntents;
    }

    private async Task GetAppIntentsFromAppDirectory(
        Action<Fdc3App, string?> selector,
        IAppIdentifier? targetAppIdentifier)
    {
        foreach (var app in await _appDirectory.GetApps())
        {
            if (targetAppIdentifier != null && targetAppIdentifier.AppId != app.AppId)
            {
                continue;
            }

            selector(app, null);
        }
    }

    private void GetAppIntentsFromRunningModules(
        Action<Fdc3App, string?> selector,
        IAppIdentifier? targetAppIdentifier)
    {
        foreach (var app in _runningModules)
        {
            if (targetAppIdentifier?.InstanceId != null
                && Guid.TryParse(targetAppIdentifier.InstanceId, out var instanceId)
                && instanceId != app.Key)
            {
                continue;
            }

            selector(app.Value, app.Key.ToString());
        }
    }

    private ValueTask<GetInfoResponse> GetAppInfo(IAppIdentifier appIdentifier)
    {
        if (!Guid.TryParse(appIdentifier.InstanceId!, out var instanceId))
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug($"Instance id: {appIdentifier.InstanceId} cannot be parsed to {typeof(Guid)}.");
            }

            return ValueTask.FromResult<GetInfoResponse>(GetInfoResponse.Failure(Fdc3DesktopAgentErrors.MissingId));
        }

        if (!_runningModules.TryGetValue(instanceId, out var app))
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug($"Instance id: {instanceId} is missing from the tracked modules of {Constants.DesktopAgentProvider}.");
            }

            return ValueTask.FromResult<GetInfoResponse>(GetInfoResponse.Failure(Fdc3DesktopAgentErrors.MissingId));
        }

        var implementationMetadata = new ImplementationMetadata
        {
            AppMetadata = GetAppMetadata(app, appIdentifier.InstanceId, null),
            Fdc3Version = Constants.SupportedFdc3Version,
            OptionalFeatures = new OptionalDesktopAgentFeatures
            {
                OriginatingAppMetadata = false, //TODO
                UserChannelMembershipAPIs = Constants.SupportUserChannelMembershipAPI
            },
            Provider = Constants.DesktopAgentProvider,
            ProviderVersion = Constants.ComposeUIVersion ?? "0.0.0"
        };

        return ValueTask.FromResult<GetInfoResponse>(GetInfoResponse.Success(implementationMetadata));
    }

    private AppMetadata GetAppMetadata(Fdc3App app, string? instanceId, IntentMetadata? intentMetadata)
    {
        return new AppMetadata
        {
            AppId = app.AppId,
            InstanceId = instanceId,
            Description = app.Description,
            Icons = app.Icons == null ? Enumerable.Empty<Icon>() : app.Icons.Select(Icon.GetIcon),
            Name = app.Name,
            ResultType = intentMetadata?.ResultType,
            Screenshots = app.Screenshots == null
                ? Enumerable.Empty<Screenshot>()
                : app.Screenshots.Select(Screenshot.GetScreenshot),
            Title = app.Title,
            Tooltip = app.ToolTip,
            Version = app.Version,
        };
    }

    private async Task<ConcurrentDictionary<string, ChannelItem>?> ReadUserChannelSet(CancellationToken cancellationToken = default)
    {
        var uri = _options.UserChannelConfigFile ?? new Uri($"file://{Directory.GetCurrentDirectory()}/userChannelSet.json");
        IEnumerable<ChannelItem>? userChannelSet = null;

        if (uri.IsFile)
        {
            var path = uri.IsAbsoluteUri ? uri.AbsolutePath : Path.GetFullPath(uri.ToString());

            if (!_fileSystem.File.Exists(path))
            {
                _logger.LogError($"{Fdc3DesktopAgentErrors.NoUserChannelSetFound}, no user channel set was configured.");
                return null;
            }

            await using var stream = _fileSystem.File.OpenRead(path);
            userChannelSet = JsonSerializer.Deserialize<ChannelItem[]>(stream, _jsonSerializerOptions);
        }
        else if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
        {
            var response = await _httpClient.GetAsync(uri, cancellationToken);
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            userChannelSet = JsonSerializer.Deserialize<ChannelItem[]>(stream, _jsonSerializerOptions);
        }

        return new((userChannelSet ?? Array.Empty<ChannelItem>()).ToDictionary(x => x.Id, y => y));
    }

    private Task RemoveModuleAsync(IModuleInstance instance)
    {
        if (!IsFdc3StartedModule(instance, out var fdc3InstanceId))
        {
            return Task.CompletedTask;
        }

        if (!_runningModules.TryRemove(new(fdc3InstanceId!), out _)) //At this point the fdc3InstanceId shouldn't be null
        {
            _logger.LogError($"Could not remove the closed window with instanceId: {fdc3InstanceId}.");
        }

        if (_pendingStartRequests.TryRemove(instance.StartRequest, out var taskCompletionSource))
        {
            taskCompletionSource.SetException(ThrowHelper.TargetInstanceUnavailable());
        }

        return Task.CompletedTask;
    }

    private async Task AddOrUpdateModuleAsync(IModuleInstance instance)
    {

        if (!IsFdc3StartedModule(instance, out var fdc3InstanceId))
        {
            return;
        }

        Fdc3App fdc3App;
        try
        {
            fdc3App = await _appDirectory.GetApp(instance.Manifest.Id);

            if (_pendingStartRequests.TryRemove(instance.StartRequest, out var taskCompletionSource))
            {
                taskCompletionSource.SetResult(instance);
            }
        }
        catch (AppNotFoundException exception)
        {
            _logger.LogError(exception, $"Could not retrieve app: {instance.Manifest.Id} from AppDirectory.");
            return;
        }

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        _runningModules.GetOrAdd(
            new(fdc3InstanceId!), //At this point the fdc3InstanceId shouldn't be null
            _ => fdc3App);
    }

    private bool IsFdc3StartedModule(IModuleInstance instance, out string? instanceId)
    {
        instanceId = string.Empty;
        var fdc3InstanceId = instance.StartRequest.Parameters.FirstOrDefault(parameter => parameter.Key == Fdc3StartupParameters.Fdc3InstanceId);

        if (string.IsNullOrEmpty(fdc3InstanceId.Value))
        {
            var startupProperties = instance.GetProperties().FirstOrDefault(property => property is Fdc3StartupProperties);

            instanceId = startupProperties == null ? null : ((Fdc3StartupProperties) startupProperties).InstanceId;

            return startupProperties == null
                ? false
                : true;
        }

        instanceId = fdc3InstanceId.Value;
        return true;
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