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
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Converters;
using MorganStanley.ComposeUI.Messaging.Abstractions;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure.Internal;

internal class ResolverUICommunicator : IResolverUICommunicator
{
    private readonly ILogger<ResolverUICommunicator> _logger;
    private readonly IMessaging _messaging;
    private readonly TimeSpan _defaultTimeout = TimeSpan.FromMinutes(2);

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        Converters = { new AppMetadataJsonConverter() }
    };

    public ResolverUICommunicator(
        IMessaging messaging,
        ILogger<ResolverUICommunicator>? logger = null)
    {
        _messaging = messaging;
        _logger = logger ?? NullLogger<ResolverUICommunicator>.Instance;
    }

    public async Task<ResolverUIResponse?> SendResolverUIRequest(IEnumerable<IAppMetadata> appMetadata, CancellationToken cancellationToken = default)
    {
        try
        {
            return await SendResolverUIRequestCore(appMetadata, cancellationToken);
        }
        catch (TimeoutException ex)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(ex, "No answer was received using the communication layer.");
            }

            return new ResolverUIResponse()
            {
                Error = ResolveError.ResolverTimeout
            };
        }
    }

    private async Task<ResolverUIResponse?> SendResolverUIRequestCore(IEnumerable<IAppMetadata> appMetadata, CancellationToken cancellationToken = default)
    {
        var request = new ResolverUIRequest
        {
            AppMetadata = appMetadata
        };

        var response = await _messaging.InvokeJsonServiceAsync<ResolverUIRequest, ResolverUIResponse>(
            Fdc3Topic.ResolverUI,
            request,
            _jsonSerializerOptions,
            cancellationToken);

        return response;
    }


    public async Task<ResolverUIIntentResponse?> SendResolverUIIntentRequest(IEnumerable<string> intents, CancellationToken cancellationToken = default)
    {
        //TODO: use the same ResolverUI
        try
        {
            return await SendResolverUIIntentRequestCore(intents, cancellationToken);
        }
        catch (TimeoutException ex)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(ex, "No answer was received using the communication layer.");
            }

            return new ResolverUIIntentResponse
            {
                Error = ResolveError.ResolverTimeout
            };
        }
    }

    private async Task<ResolverUIIntentResponse?> SendResolverUIIntentRequestCore(IEnumerable<string> intents, CancellationToken cancellationToken = default)
    {
        var request = new ResolverUIIntentRequest
        {
            Intents = intents
        };

        var response = await _messaging.InvokeJsonServiceAsync<ResolverUIIntentRequest, ResolverUIIntentResponse>(
            Fdc3Topic.ResolverUIIntent,
            request, _jsonSerializerOptions,
            cancellationToken: cancellationToken);

        return response;
    }

}