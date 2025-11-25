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

using System.Text.Json;
using Finos.Fdc3;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Contracts;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Exceptions;
using MorganStanley.ComposeUI.Messaging.Abstractions;
using AppIdentifier = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.AppIdentifier;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Infrastructure.Internal;

internal class MetadataClient : IMetadataClient
{
    private readonly string _appId;
    private readonly string _instanceId;
    private readonly IMessaging _messaging;
    private readonly ILogger<MetadataClient> _logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions = SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization;

    public MetadataClient(
        string appId,
        string instanceId,
        IMessaging messaging,
        ILogger<MetadataClient>? logger = null)
    {
        _appId = appId;
        _instanceId = instanceId;
        _messaging = messaging;
        _logger = logger ?? NullLogger<MetadataClient>.Instance;
    }

    public async ValueTask<IEnumerable<IAppIdentifier>> FindInstancesAsync(IAppIdentifier appIdentifier)
    {
        var request = new FindInstancesRequest
        {
            Fdc3InstanceId = _instanceId,
            AppIdentifier = new AppIdentifier
            {
                AppId = appIdentifier.AppId,
                InstanceId = appIdentifier.InstanceId,
            }
        };

        var response = await _messaging.InvokeJsonServiceAsync<FindInstancesRequest, FindInstancesResponse>(
            Fdc3Topic.FindInstances,
            request,
            _jsonSerializerOptions);

        if (response == null)
        {
            throw ThrowHelper.MissingResponse();
        }

        if (!string.IsNullOrEmpty(response.Error))
        {
            throw ThrowHelper.ErrorResponseReceived(response.Error);
        }

        if (response.Instances == null)
        {
            throw ThrowHelper.DesktopAgentBackendDidNotResolveRequest(nameof(FindInstancesRequest), nameof(response.Instances), Fdc3DesktopAgentErrors.NoInstanceFound);
        }

        return response.Instances;
    }

    public async ValueTask<IAppMetadata> GetAppMetadataAsync(IAppIdentifier appIdentifier)
    {
        var request = new GetAppMetadataRequest
        {
            Fdc3InstanceId = _instanceId,
            AppIdentifier = new Shared.Protocol.AppIdentifier
            {
                AppId = appIdentifier.AppId,
                InstanceId = appIdentifier.InstanceId,
            }
        };

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Sending request to retrieve metadata for app: {AppId}, instanceId: {InstanceId}...", appIdentifier.AppId, appIdentifier.InstanceId);
        }

        var response = await _messaging.InvokeJsonServiceAsync<GetAppMetadataRequest, GetAppMetadataResponse>(
            Fdc3Topic.GetAppMetadata,
            request,
            _jsonSerializerOptions);

        if (response == null)
        {
            _logger.LogError("{Method} response is null returned by the server...", nameof(GetAppMetadataAsync));
            throw ThrowHelper.MissingResponse();
        }

        if (response.Error != null)
        {
            _logger.LogError("{AppId} cannot return the {AppMetadata} for {TargetAppId} due to: {Error}.", _appId, nameof(AppMetadata), appIdentifier.AppId, response.Error);
            throw ThrowHelper.ErrorResponseReceived(_appId, appIdentifier.AppId, nameof(AppMetadata), response.Error);
        }

        return response.AppMetadata!;
    }

    public async ValueTask<IImplementationMetadata> GetInfoAsync()
    {
        var request = new GetInfoRequest
        {
            AppIdentifier = new Shared.Protocol.AppIdentifier
            {
                AppId = _appId,
                InstanceId = _instanceId
            }
        };

        var response = await _messaging.InvokeJsonServiceAsync<GetInfoRequest, GetInfoResponse>(
            Fdc3Topic.GetInfo,
            request,
            _jsonSerializerOptions).ConfigureAwait(false);

        if (response == null)
        {
            _logger.LogError("{Method} response is null returned by the server...", nameof(GetInfoAsync));
            throw ThrowHelper.MissingResponse();
        }

        if (response.Error != null)
        {
            _logger.LogError("{AppId} cannot return the {ImplementationMetadata} due to: {Error}.", _appId, nameof(ImplementationMetadata), response.Error);
            throw ThrowHelper.ErrorResponseReceived(_appId, _appId, nameof(ImplementationMetadata), response.Error);
        }

        if (response.ImplementationMetadata == null)
        {
            throw ThrowHelper.InvalidResponseRecevied(_instanceId, _appId, nameof(GetInfoAsync));
        }

        return response.ImplementationMetadata;
    }
}
