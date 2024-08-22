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

        var result = await GetAppIntentsByRequest(selector, null);

        return !result.AppIntents.TryGetValue(request.Intent, out var appIntent)
            ? FindIntentResponse.Failure(ResolveError.NoAppsFound)
            : FindIntentResponse.Success(appIntent);
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

        var result = await GetAppIntentsByRequest(selector, null);

        return !result.AppIntents.Any()
            ? FindIntentsByContextResponse.Failure(ResolveError.NoAppsFound)
            : FindIntentsByContextResponse.Success(result.AppIntents.Values);
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

            if (intentResolution == null)
            {
                return GetIntentResultResponse.Failure(ResolveError.ResolverUnavailable);
            }

            if (intentResolution.ResultError != null)
            {
                return GetIntentResultResponse.Failure(intentResolution.ResultError);
            }

            if (!intentResolution.IsResolved
                && ((_raisedIntentResolutions.TryGetValue(
                         new Guid(request.TargetAppIdentifier.InstanceId!),
                         out var handler)
                        && !handler.IsIntentListenerRegistered(request.Intent))
                    || intentResolution.ResultError == null))
            {
                return GetIntentResultResponse.Failure(ResolveError.IntentDeliveryFailed);
            }

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
        try
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
        catch (Exception exception)
        {
            _logger.LogError(exception, $"Exception was thrown while executing {nameof(StoreIntentResult)} call.");
            return ValueTask.FromResult(StoreIntentResultResponse.Failure(Fdc3DesktopAgentErrors.MissingId));
        }
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
                        if (!_runningModules.TryGetValue(new(request.Fdc3InstanceId), out var app))
                        {
                            continue;
                        }

                        var specification = new RaiseIntentSpecification
                        {
                            Intent = raisedIntent.Intent,
                            Context = raisedIntent.Context,
                            SourceAppInstanceId = new(raisedIntent.OriginFdc3InstanceId),
                            TargetAppMetadata = GetAppMetadata(app, request.Fdc3InstanceId, null),
                        };

                        var resolution = await GetRaiseIntentResolutionMessage(
                            raisedIntent.RaiseIntentMessageId,
                            specification);

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

    public async ValueTask<FindInstancesResponse> FindInstances(FindInstancesRequest? request)
    {
        if (request == null)
        {
            return FindInstancesResponse.Failure(Fdc3DesktopAgentErrors.PayloadNull);
        }

        if (!Guid.TryParse(request.Fdc3InstanceId, out var instanceId) || !_runningModules.TryGetValue(instanceId, out _))
        {
            return FindInstancesResponse.Failure(Fdc3DesktopAgentErrors.MissingId); //AccessDenied?
        }

        try
        {
            await _appDirectory.GetApp(request.AppIdentifier.AppId!);
        }
        catch (AppNotFoundException)
        {
            return FindInstancesResponse.Failure(ResolveError.NoAppsFound);
        }

        var apps = _runningModules
            .Where(app => app.Value.AppId == request.AppIdentifier.AppId)
            .Select(x => new AppIdentifier() { AppId = x.Value.AppId, InstanceId = x.Key.ToString() });

        if (apps.Any())
        {
            return FindInstancesResponse.Success(apps);
        }
        else
        {
            return FindInstancesResponse.Success(Enumerable.Empty<AppIdentifier>());
        }
    }

    public async ValueTask<GetAppMetadataResponse> GetAppMetadata(GetAppMetadataRequest? request)
    {
        if (request == null)
        {
            return GetAppMetadataResponse.Failure(Fdc3DesktopAgentErrors.PayloadNull);
        }

        if (!Guid.TryParse(request.Fdc3InstanceId, out var instanceId) || !_runningModules.TryGetValue(instanceId, out _))
        {
            return GetAppMetadataResponse.Failure(Fdc3DesktopAgentErrors.MissingId); //AccessDenied?
        }

        if (request.AppIdentifier.InstanceId != null)
        {
            if (!Guid.TryParse(request.AppIdentifier.InstanceId, out var fdc3InstanceId))
            {
                return GetAppMetadataResponse.Failure(Fdc3DesktopAgentErrors.MissingId); //AccessDenied?
            }

            if (!_runningModules.TryGetValue(fdc3InstanceId, out var app))
            {
                return GetAppMetadataResponse.Failure(ResolveError.TargetInstanceUnavailable);
            }

            var appMetadata = GetAppMetadata(app, request.AppIdentifier.InstanceId, null);
            return GetAppMetadataResponse.Success(appMetadata);
        }

        try
        {
            var app = await _appDirectory.GetApp(request.AppIdentifier.AppId);
            var appMetadata = GetAppMetadata(app, null, null);
            return GetAppMetadataResponse.Success(appMetadata);
        }
        catch (AppNotFoundException)
        {
            return GetAppMetadataResponse.Failure(ResolveError.TargetAppUnavailable);
        }
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
                && request.Context.Type != ContextTypes.Nothing)
            {
                return null;
            }

            if (request.TargetAppIdentifier != null && (fdc3App.AppId != request.TargetAppIdentifier.AppId))
            {
                return null;
            }

            return new Dictionary<string, IntentMetadata> { { request.Intent, intentMetadata } };
        };

        var intentQueryResult = await GetAppIntentsByRequest(selector, request.TargetAppIdentifier);

        if (intentQueryResult.Error != null)
        {
            return new()
            {
                Response = RaiseIntentResponse.Failure(intentQueryResult.Error)
            };
        }

        //No intents were found which would have the right information to handle the raised intent
        if (!intentQueryResult.AppIntents.TryGetValue(request.Intent, out var appIntent) || !appIntent.Apps.Any())
        {
            return new()
            {
                Response = RaiseIntentResponse.Failure(ResolveError.NoAppsFound)
            };
        }

        RaiseIntentSpecification raiseIntentSpecification = new()
        {
            Context = request.Context,
            Intent = request.Intent,
            RaisedIntentMessageId = request.MessageId,
            SourceAppInstanceId = new(request.Fdc3InstanceId)
        };

        if (appIntent.Apps.Count() == 1)
        {
            raiseIntentSpecification.TargetAppMetadata = appIntent.Apps.ElementAt(0);

            return await RaiseIntentToApplication(raiseIntentSpecification);
        }

        //Resolve to one app via ResolverUI.
        var result = await WaitForResolverUIAsync(appIntent.Apps);

        if (result != null && result.Error == null)
        {
            raiseIntentSpecification.TargetAppMetadata = (AppMetadata) result.AppMetadata!;
            return await RaiseIntentToApplication(raiseIntentSpecification);
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
    private async ValueTask<RaiseIntentResult<RaiseIntentResponse>> RaiseIntentToApplication(RaiseIntentSpecification raiseIntentSpecification)
    {
        async Task<RaiseIntentResult<RaiseIntentResponse>?> GetRaiseIntentResponse(RaiseIntentSpecification raiseIntentSpecification, string messageId)
        {
            RaiseIntentResult<RaiseIntentResponse>? response = null;

            while (response == null && response?.Response.AppMetadata == null)
            {
                if (!_raisedIntentResolutions.TryGetValue(new(raiseIntentSpecification.TargetAppMetadata.InstanceId!), out var registeredFdc3App))
                {
                    response = new()
                    {
                        Response = RaiseIntentResponse.Failure(ResolveError.TargetInstanceUnavailable)
                    };

                    await Task.Delay(1);
                    continue;
                }

                if (!registeredFdc3App.IsIntentListenerRegistered(raiseIntentSpecification.Intent))
                {
                    response = null;
                    await Task.Delay(1);
                    continue;
                }

                var resolution = await GetRaiseIntentResolutionMessage(messageId, raiseIntentSpecification);
                response = new()
                {
                    Response = RaiseIntentResponse.Success(messageId, raiseIntentSpecification.Intent, raiseIntentSpecification.TargetAppMetadata),
                    RaiseIntentResolutionMessages = resolution == null
                        ? Enumerable.Empty<RaiseIntentResolutionMessage>()
                        : [resolution]
                };

                return response;
            }

            return response;
        }

        try
        {
            if (!string.IsNullOrEmpty(raiseIntentSpecification.TargetAppMetadata.InstanceId))
            {
                var raisedIntentMessageId = StoreRaisedIntentForTarget(raiseIntentSpecification);

                var response = await GetRaiseIntentResponse(raiseIntentSpecification, raisedIntentMessageId).WaitAsync(_options.ListenerRegistrationTimeout);
                if (response != null)
                {
                    return response;
                }

                return new()
                {
                    Response = RaiseIntentResponse.Failure(ResolveError.IntentDeliveryFailed)
                };
            }

            try
            {
                var module = await StartModule(raiseIntentSpecification.TargetAppMetadata);

                raiseIntentSpecification.TargetAppMetadata = module;

                var raisedIntentMessageId = StoreRaisedIntentForTarget(raiseIntentSpecification);

                if (!_raisedIntentResolutions.TryGetValue(new Guid(module.InstanceId!), out _))
                {
                    return new()
                    {
                        Response = RaiseIntentResponse.Failure(ResolveError.TargetInstanceUnavailable)
                    };
                }

                var response = await GetRaiseIntentResponse(raiseIntentSpecification, raisedIntentMessageId).WaitAsync(_options.ListenerRegistrationTimeout);

                if (response != null)
                {
                    return response;
                }

                return new()
                {
                    Response = RaiseIntentResponse.Failure(ResolveError.IntentDeliveryFailed)
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
        catch (TimeoutException)
        {
            return new()
            {
                Response = RaiseIntentResponse.Failure(ResolveError.IntentDeliveryFailed)
            };
        }
    }

    private async ValueTask<AppMetadata> StartModule(AppMetadata targetAppMetadata, IEnumerable<KeyValuePair<string, string>>? additionalStartupParameters = null)
    {
        var startupParameters = additionalStartupParameters?.ToDictionary(x => x.Key, y => y.Value) ?? new Dictionary<string, string>();

        var fdc3InstanceId = Guid.NewGuid().ToString();
        startupParameters.Add(Fdc3StartupParameters.Fdc3InstanceId, fdc3InstanceId);

        var startRequest = new StartRequest(
            targetAppMetadata.AppId, //TODO: possible remove some identifier like @"fdc3."
            startupParameters);

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

        return new AppMetadata
        {
            AppId = targetAppMetadata.AppId,
            InstanceId = fdc3InstanceId,
            Name = targetAppMetadata.Name,
            Version = targetAppMetadata.Version,
            Title = targetAppMetadata.Title,
            Tooltip = targetAppMetadata.Tooltip,
            Description = targetAppMetadata.Description,
            Icons = targetAppMetadata.Icons.Select(Icon.GetIcon),
            Screenshots = targetAppMetadata.Screenshots.Select(Screenshot.GetScreenshot),
            ResultType = targetAppMetadata.ResultType
        };
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

    private string StoreRaisedIntentForTarget(RaiseIntentSpecification raiseIntentSpecification)
    {
        var invocation = new RaiseIntentResolutionInvocation(
            raiseIntentMessageId: raiseIntentSpecification.RaisedIntentMessageId,
            intent: raiseIntentSpecification.Intent,
            originFdc3InstanceId: raiseIntentSpecification.SourceAppInstanceId.ToString(),
            contextToHandle: raiseIntentSpecification.Context);

        //At this point the InstanceId should be not null.
        _raisedIntentResolutions.AddOrUpdate(
            new(raiseIntentSpecification.TargetAppMetadata.InstanceId!),
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
        RaiseIntentSpecification raiseIntentSpecification)
    {
        if (_runningModules.TryGetValue(raiseIntentSpecification.SourceAppInstanceId, out var sourceApp))
        {
            var sourceAppIdentifier = new AppIdentifier
            {
                AppId = sourceApp.AppId,
                InstanceId = raiseIntentSpecification.SourceAppInstanceId.ToString()
            };

            return Task.FromResult<RaiseIntentResolutionMessage?>(
                new()
                {
                    Intent = raiseIntentSpecification.Intent,
                    TargetModuleInstanceId = raiseIntentSpecification.TargetAppMetadata.InstanceId!,
                    Request = new RaiseIntentResolutionRequest
                    {
                        MessageId = raisedIntentMessageId,
                        Context = raiseIntentSpecification.Context,
                        ContextMetadata = new ContextMetadata
                        {
                            Source = sourceAppIdentifier
                        }
                    }
                });
        }

        return Task.FromResult<RaiseIntentResolutionMessage?>(null);
    }

    private async Task<IntentQueryResult> GetAppIntentsByRequest(
        Func<Fdc3App, Dictionary<string, AppIntent>, IEnumerable<KeyValuePair<string, IntentMetadata>>?> selector,
        IAppIdentifier? targetAppIdentifier)
    {
        var result = new IntentQueryResult();

        if (targetAppIdentifier?.InstanceId == null)
        {
            result = await GetAppIntentsFromAppDirectory(selector, targetAppIdentifier, result);
        }

        result = await GetAppIntentsFromRunningModules(selector, targetAppIdentifier, result);

        return result;
    }

    private async Task<IntentQueryResult> GetAppIntentsFromRunningModules(
        Func<Fdc3App, Dictionary<string, AppIntent>, IEnumerable<KeyValuePair<string, IntentMetadata>>?> selector,
        IAppIdentifier? targetAppIdentifier,
        IntentQueryResult result)
    {
        void FilterAppIntents(Fdc3App app, string id)
        {
            var intentMetadataCollection = selector(app, result.AppIntents);

            if (intentMetadataCollection == null)
            {
                return;
            }

            result.AppIntents = GetAppIntentsFromIntentMetadataCollection(
                app,
                id,
                intentMetadataCollection,
                result.AppIntents);
        }

        var validInstanceId = Guid.TryParse(targetAppIdentifier?.InstanceId, out var id);

        if (validInstanceId)
        {
            if (_runningModules.TryGetValue(id, out var fdc3App))
            {
                FilterAppIntents(fdc3App, targetAppIdentifier!.InstanceId!);
                return result;
            }

            try
            {
                //If the app exists, but the specified instance id does not.
                await _appDirectory.GetApp(targetAppIdentifier!.AppId);
                result.Error = ResolveError.TargetInstanceUnavailable;
                return result;
            }
            catch (AppNotFoundException)
            {
                result.Error = ResolveError.IntentDeliveryFailed;
                return result;
            }
        }

        if (targetAppIdentifier?.InstanceId != null && !validInstanceId)
        {
            result.Error = ResolveError.TargetInstanceUnavailable;
            return result;
        }

        //foreach is safe to do on a concurrent dictionary, it might involve the later added items, but would not throw an exception
        foreach (var app in _runningModules)
        {
            if (targetAppIdentifier?.InstanceId != null
                && Guid.TryParse(targetAppIdentifier.InstanceId, out var instanceId)
                && instanceId != app.Key)
            {
                continue;
            }

            FilterAppIntents(app.Value, app.Key.ToString());
        }

        return result;
    }

    private async Task<IntentQueryResult> GetAppIntentsFromAppDirectory(
        Func<Fdc3App, Dictionary<string, AppIntent>, IEnumerable<KeyValuePair<string, IntentMetadata>>?> selector,
        IAppIdentifier? targetAppIdentifier,
        IntentQueryResult result)
    {
        var apps = (await _appDirectory.GetApps()).ToArray();

        void FilterAppIntents(Fdc3App app)
        {
            var intentMetadataCollection = selector(app, result.AppIntents);

            if (intentMetadataCollection == null)
            {
                return;
            }

            result.AppIntents = GetAppIntentsFromIntentMetadataCollection(app, null, intentMetadataCollection, result.AppIntents);
        }

        if (targetAppIdentifier != null)
        {
            var fdc3App = apps.FirstOrDefault(x => x.AppId == targetAppIdentifier.AppId);
            if (fdc3App == default)
            {
                result.Error = ResolveError.TargetAppUnavailable;
                return result;
            }

            FilterAppIntents(fdc3App);
            return result;
        }

        foreach (var app in apps)
        {
            if (targetAppIdentifier != null && targetAppIdentifier.AppId != app.AppId)
            {
                continue;
            }

            FilterAppIntents(app);
        }

        return result;
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