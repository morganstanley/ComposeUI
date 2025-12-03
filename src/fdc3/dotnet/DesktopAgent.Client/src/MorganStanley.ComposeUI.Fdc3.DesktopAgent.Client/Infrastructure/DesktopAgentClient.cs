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
using Finos.Fdc3;
using Finos.Fdc3.Context;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Infrastructure.Internal;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Exceptions;
using MorganStanley.ComposeUI.Messaging.Abstractions;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Infrastructure;

/// <summary>
/// Desktop Agent client implementation.
/// </summary>
public class DesktopAgentClient : IDesktopAgent
{
    private readonly IMessaging _messaging;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<DesktopAgentClient> _logger;
    private readonly string _appId;
    private readonly string _instanceId;
    private IChannelFactory _channelFactory;
    private IMetadataClient _metadataClient;
    private IIntentsClient _intentsClient;
    private IOpenClient _openClient;

    //This cache stores the top-level context listeners added through the `AddContextListener<T>(...)` API. It stores their actions to be able to resubscribe them when joining a new channel and handle the last context based on the FDC3 standard.
    private readonly Dictionary<IListener, Func<string, ChannelType, CancellationToken, ValueTask>> _contextListeners = new();
    private readonly ConcurrentDictionary<string, IListener> _intentListeners = new();

    private IChannel? _currentChannel;
    private IContext? _openedAppContext;

    private readonly SemaphoreSlim _currentChannelLock = new(1, 1);

    private readonly ConcurrentDictionary<string, IChannel> _userChannels = new();
    private readonly ConcurrentDictionary<string, IChannel> _appChannels = new();
    private readonly SemaphoreSlim _appChannelsLock = new(1, 1);

    private readonly TaskCompletionSource<string> _initializationTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private string? _openedAppContextId;

    public DesktopAgentClient(
        IMessaging messaging,
        ILoggerFactory? loggerFactory = null)
    {
        _messaging = messaging;
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _logger = _loggerFactory.CreateLogger<DesktopAgentClient>();

        _appId = Environment.GetEnvironmentVariable(nameof(AppIdentifier.AppId)) ?? throw ThrowHelper.MissingAppId(string.Empty);
        _instanceId = Environment.GetEnvironmentVariable(nameof(AppIdentifier.InstanceId)) ?? throw ThrowHelper.MissingInstanceId(_appId, string.Empty);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("AppID: {AppId}; InstanceId: {InstanceId} is registered for the FDC3 client app.", _appId, _instanceId);
        }

        _metadataClient = new MetadataClient(_appId, _instanceId, _messaging, _loggerFactory.CreateLogger<MetadataClient>());
        _openClient = new OpenClient(_instanceId, _messaging, this, _loggerFactory.CreateLogger<OpenClient>());

        _ = Task.Run(() => InitializeAsync());
    }

    /// <summary>
    /// Joins to a user channel if the channel id is initially defined for the app and requests to backend to return the context if the app was opened through fdc3.open call.
    /// </summary>
    /// <returns></returns>
    private async Task InitializeAsync()
    {
        try
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Initializing DesktopAgentClient...");
            }

            var channelId = Environment.GetEnvironmentVariable(Fdc3StartupParameters.Fdc3ChannelId);
            var openedAppContextId = Environment.GetEnvironmentVariable(Fdc3StartupParameters.OpenedAppContextId);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Retrieved startup parameters for channelId: {ChannelId}; openedAppContextId: {OpenedAppContext}.", channelId ?? "null", openedAppContextId ?? "null");
            }

            if (!string.IsNullOrEmpty(channelId))
            {
                await JoinUserChannel(channelId!).ConfigureAwait(false);
            }

            if (!string.IsNullOrEmpty(openedAppContextId))
            {
                _openedAppContextId = openedAppContextId;
                await GetOpenedAppContextAsync(openedAppContextId!).ConfigureAwait(false);
            }

            _channelFactory = new ChannelFactory(_messaging, _instanceId, _openedAppContext, _loggerFactory);
            _intentsClient = new IntentsClient(_messaging, _channelFactory, _instanceId, _loggerFactory);

            _initializationTaskCompletionSource.SetResult(_instanceId);
        }
        catch (Exception exception)
        {
            _initializationTaskCompletionSource.TrySetException(exception);
        }
    }

    /// <summary>
    /// Adds a context listener for the specified context type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="contextType"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    public async Task<IListener> AddContextListener<T>(string? contextType, ContextHandler<T> handler) where T : IContext
    {
        await _initializationTaskCompletionSource.Task.ConfigureAwait(false);

        ContextListener<T> listener = null;
        await _currentChannelLock.WaitAsync().ConfigureAwait(false);

        try
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(
                    "Checking if the app was opened via fdc3.open. OpenedAppContext exists: {IsNotNull}, current context listener's context type: {ContextType}, received app context's type via open call: {OpenedAppContextType}.", _openedAppContext != null, contextType, _openedAppContext?.Type);
            }

            listener = await _channelFactory.CreateContextListenerAsync<T>(handler, _currentChannel, contextType);

            _contextListeners.Add(
                listener,
                async (channelId, channelType, cancellationToken) =>
                {
                    await listener.SubscribeAsync(channelId, channelType, cancellationToken);
                    await HandleLastContextAsync(listener);
                });

            return listener;
        }
        finally
        {
            await HandleLastContextAsync(listener);

            _currentChannelLock.Release();
        }
    }

    /// <summary>
    /// Adds an intent listener for the specified intent.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="intent"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    public async Task<IListener> AddIntentListener<T>(string intent, IntentHandler<T> handler) where T : IContext
    {
        await _initializationTaskCompletionSource.Task.ConfigureAwait(false);

        if (_intentListeners.TryGetValue(intent, out var existingListener))
        {
            return existingListener;
        }

        var listener = await _intentsClient.AddIntentListenerAsync<T>(intent, handler);

        if (!_intentListeners.TryAdd(intent, listener))
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning("Failed to add intent listener to the internal collection: {Intent}.", intent);
            }
        }

        return listener;
    }

    /// <summary>
    /// Broadcasts the specified context to the current channel.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task Broadcast(IContext context)
    {
        await _initializationTaskCompletionSource.Task.ConfigureAwait(false);

        try
        {
            await _currentChannelLock.WaitAsync().ConfigureAwait(false);

            if (_currentChannel == null)
            {
                throw ThrowHelper.ClientNotConnectedToUserChannel();
            }

            await _currentChannel.Broadcast(context);
        }
        finally
        {
            _currentChannelLock.Release();
        }
    }

    /// <summary>
    /// Creates a new private channel.
    /// </summary>
    /// <returns></returns>
    public async Task<IPrivateChannel> CreatePrivateChannel()
    {
        await _initializationTaskCompletionSource.Task.ConfigureAwait(false);
        var privateChannel = await _channelFactory.CreatePrivateChannelAsync();

        return privateChannel;
    }

    /// <summary>
    /// Lists all instances of the specified app.
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public async Task<IEnumerable<IAppIdentifier>> FindInstances(IAppIdentifier app)
    {
        await _initializationTaskCompletionSource.Task.ConfigureAwait(false);

        var instances = await _metadataClient.FindInstancesAsync(app);
        return instances;
    }

    /// <summary>
    /// Finds an app intent matching the specified parameters from the AppDirectory.
    /// </summary>
    /// <param name="intent"></param>
    /// <param name="context"></param>
    /// <param name="resultType"></param>
    /// <returns></returns>
    public async Task<IAppIntent> FindIntent(string intent, IContext? context = null, string? resultType = null)
    {
        await _initializationTaskCompletionSource.Task.ConfigureAwait(false);

        var result = await _intentsClient.FindIntentAsync(intent, context, resultType);
        return result;
    }

    /// <summary>
    /// Lists all app intents matching the specified context and other parameters from the AppDirectory.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="resultType"></param>
    /// <returns></returns>
    public async Task<IEnumerable<IAppIntent>> FindIntentsByContext(IContext context, string? resultType = null)
    {
        await _initializationTaskCompletionSource.Task.ConfigureAwait(false);

        var appIntents = await _intentsClient.FindIntentsByContextAsync(context, resultType);
        return appIntents;
    }

    /// <summary>
    /// Retrieves metadata for the specified app.
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public async Task<IAppMetadata> GetAppMetadata(IAppIdentifier app)
    {
        await _initializationTaskCompletionSource.Task.ConfigureAwait(false);

        var appMetadata = await _metadataClient.GetAppMetadataAsync(app);
        return appMetadata;
    }

    /// <summary>
    /// Retrieves the current user channel the app is connected to.
    /// </summary>
    /// <returns></returns>
    public async Task<IChannel?> GetCurrentChannel()
    {
        await _initializationTaskCompletionSource.Task.ConfigureAwait(false);

        return _currentChannel;
    }

    /// <summary>
    /// Retrieves implementation metadata of the current application.
    /// </summary>
    /// <returns></returns>
    public async Task<IImplementationMetadata> GetInfo()
    {
        await _initializationTaskCompletionSource.Task.ConfigureAwait(false);

        var implementationMetadata = await _metadataClient.GetInfoAsync();
        return implementationMetadata;
    }

    /// <summary>
    /// Creates or retrieves an application channel with the specified id.
    /// </summary>
    /// <param name="channelId"></param>
    /// <returns></returns>
    public async Task<IChannel> GetOrCreateChannel(string channelId)
    {
        try
        {
            await _initializationTaskCompletionSource.Task.ConfigureAwait(false);
            await _appChannelsLock.WaitAsync().ConfigureAwait(false);

            if (_appChannels.TryGetValue(channelId, out var existingChannel))
            {
                return existingChannel;
            }

            var channel = await _channelFactory.CreateAppChannelAsync(channelId);

            if (!_appChannels.TryAdd(channelId, channel))
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                {
                    _logger.LogWarning("Failed to add app channel to the internal collection: {ChannelId}.", channelId);
                }
            }

            return channel;
        }
        finally
        {
            _appChannelsLock.Release();
        }
    }

    /// <summary>
    /// Lists all user channels available.
    /// </summary>
    /// <returns></returns>
    public async Task<IEnumerable<IChannel>> GetUserChannels()
    {
        await _initializationTaskCompletionSource.Task.ConfigureAwait(false);

        var channels = await _channelFactory.GetUserChannelsAsync();
        return channels;
    }

    /// <summary>
    /// Joins to a user channel with the specified id.
    /// </summary>
    /// <param name="channelId"></param>
    /// <returns></returns>
    public async Task JoinUserChannel(string channelId)
    {
        await _initializationTaskCompletionSource.Task.ConfigureAwait(false);

        try
        {
            await _currentChannelLock.WaitAsync().ConfigureAwait(false);

            if (_currentChannel != null)
            {
                _logger.LogInformation("Leaving current channel: {CurrentChannelId}...", _currentChannel.Id);
                await LeaveCurrentChannel().ConfigureAwait(false);
            }

            if (!_userChannels.TryGetValue(channelId, out var channel))
            {
                channel = await _channelFactory.JoinUserChannelAsync(channelId).ConfigureAwait(false);
                _userChannels[channelId] = channel;
            }

            _currentChannel = channel;
        }
        finally
        {
            var contextListeners = _contextListeners.Reverse();
            foreach (var contextListener in contextListeners)
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Subscribing context listener to channel: {CurrentChannelId}...", _currentChannel?.Id);
                }

                await contextListener.Value(_currentChannel!.Id, _currentChannel!.Type, CancellationToken.None);
            }

            _currentChannelLock.Release();
        }
    }

    /// <summary>
    /// Leaves the current user channel.
    /// </summary>
    /// <returns></returns>
    public async Task LeaveCurrentChannel()
    {
        await _initializationTaskCompletionSource.Task.ConfigureAwait(false);

        try
        {
            await _currentChannelLock.WaitAsync().ConfigureAwait(false);
            var contextListeners = _contextListeners.Reverse();

            //The context listeners, that have been added through the `fdc3.addContextListener()` should unsubscribe, but the context listeners should remain registered to the DesktopAgentClient instance.
            foreach (var contextListener in contextListeners)
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Unsubscribing context listener from channel: {CurrentChannelId}...", _currentChannel?.Id);
                }

                contextListener.Key.Unsubscribe();
            }

            _currentChannel = null;
        }
        finally
        {
            _currentChannelLock.Release();
        }
    }

    /// <summary>
    /// Opens the specified app.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task<IAppIdentifier> Open(IAppIdentifier app, IContext? context = null)
    {
        await _initializationTaskCompletionSource.Task.ConfigureAwait(false);

        var appIdentifier = await _openClient.OpenAsync(app, context);
        return appIdentifier;
    }

    /// <summary>
    /// Raises the specified intent with the given context and optionally targets specific app to handle it. When multiple apps can handle the intent, the user will be prompted to choose one.
    /// </summary>
    /// <param name="intent"></param>
    /// <param name="context"></param>
    /// <param name="app"></param>
    /// <returns></returns>
    public async Task<IIntentResolution> RaiseIntent(string intent, IContext context, IAppIdentifier? app = null)
    {
        await _initializationTaskCompletionSource.Task.ConfigureAwait(false);

        var intentResolution = await _intentsClient.RaiseIntentAsync(intent, context, app);
        return intentResolution;
    }

    /// <summary>
    /// Raises an intent for the given context and optionally targets specific app to handle it. When multiple apps can handle the intent, the user will be prompted to choose one.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="app"></param>
    /// <returns></returns>
    public async Task<IIntentResolution> RaiseIntentForContext(IContext context, IAppIdentifier? app = null)
    {
        await _initializationTaskCompletionSource.Task.ConfigureAwait(false);

        var intentResolution = await _intentsClient.RaiseIntentForContextAsync(context, app);
        return intentResolution;
    }

    /// <summary>
    /// Checks if the app was opened through fdc3.open and retrieves the context associated with the opened app.
    /// </summary>
    /// <param name="openedAppContextId"></param>
    /// <returns></returns>
    internal async ValueTask GetOpenedAppContextAsync(string openedAppContextId)
    {
        try
        {
            _openedAppContext = await _openClient.GetOpenAppContextAsync(openedAppContextId);
        }
        catch (Fdc3DesktopAgentException exception)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(exception, "No context was received for the opened app.");
            }
        }
    }

    /// <summary>
    /// Handles the last context in the current channel for the specified context listener.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="listener"></param>
    /// <returns></returns>
    private async Task HandleLastContextAsync<T>(
        ContextListener<T>? listener = null)
        where T : IContext
    {
        if (listener == null)
        {
            return;
        }

        if (_currentChannel == null)
        {
            return;
        }

        var lastContext = await _currentChannel.GetCurrentContext(listener.ContextType);

        if (lastContext == null)
        {
            _logger.LogDebug("No last context of type: {ContextType} found in channel: {CurrentChannelId}.", listener.ContextType ?? "null", _currentChannel.Id);
            return;
        }

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Invoking context handler for the last context of type: {ContextType}.", listener.ContextType ?? "null");
        }

        await listener.HandleContextAsync(lastContext);
    }
}