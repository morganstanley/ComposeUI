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
using Microsoft.Extensions.Options;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.DependencyInjection;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Contracts;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Converters;
using MorganStanley.ComposeUI.Messaging.Abstractions;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure.Internal;

internal class ResolverUICommunicator : IResolverUICommunicator
{
    private readonly ILogger<ResolverUICommunicator> _logger;
    private readonly IMessaging _messaging;
    private readonly TimeSpan _defaultTimeout = TimeSpan.FromMinutes(2);
    private readonly bool _allowLegacyResolverUI;

    private readonly JsonSerializerOptions _jsonSerializerOptions = SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization;
    
    public ResolverUICommunicator(
        IMessaging messaging,
        ILogger<ResolverUICommunicator>? logger = null,
        IOptions<Fdc3DesktopAgentOptions>? options = null)
    {
        _messaging = messaging;
        _logger = logger ?? NullLogger<ResolverUICommunicator>.Instance;
        _allowLegacyResolverUI = options?.Value.AllowLegacyResolverUI ?? false;
    }

    public async Task<ResolverUIResponse?> SendResolverUIRequestAsync(IEnumerable<IAppMetadata> appMetadata, CancellationToken cancellationToken = default)
    {
        try
        {
            return await SendResolverUIRequestCore(appMetadata, cancellationToken).ConfigureAwait(false);
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
        catch (JsonException ex)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(ex, "The response of the ResolverUI could not be read.");
            }

            return new ResolverUIResponse()
            {
                Error = ResolveError.ResolverUnavailable
            };
        }
    }

    private async Task<ResolverUIResponse?> SendResolverUIRequestCore(IEnumerable<IAppMetadata> appMetadata, CancellationToken cancellationToken = default)
    {
        if (_allowLegacyResolverUI)
        {
            return await SendLegacyResolverUIRequestCore(appMetadata, cancellationToken).ConfigureAwait(false);
        }

        // The ids are generated per request, so a response can only select an app that was offered in this very request.
        var candidates = appMetadata
            .Select(app => new ResolverUICandidate { Id = Guid.NewGuid().ToString("N"), AppMetadata = app })
            .ToArray();

        var request = new ResolverUIRequest
        {
            Candidates = candidates
        };

        var response = await _messaging.InvokeJsonServiceAsync<ResolverUIRequest, ResolverUIResponse>(
            Fdc3Topic.ResolverUI,
            request,
            _jsonSerializerOptions,
            cancellationToken).ConfigureAwait(false);

        if (response == null)
        {
            return null;
        }

        if (response.Error != null)
        {
            return new ResolverUIResponse { Error = response.Error };
        }

        // Only the id is used from the response: the app is always the one that the Desktop Agent offered.
        var selected = string.IsNullOrEmpty(response.SelectedCandidateId)
            ? null
            : candidates.FirstOrDefault(candidate => string.Equals(candidate.Id, response.SelectedCandidateId, StringComparison.Ordinal));

        if (selected == null)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning("The ResolverUI did not select any of the apps that were offered, so the raised intent is not delivered.");
            }

            return new ResolverUIResponse { Error = ResolveError.ResolverUnavailable };
        }

        return new ResolverUIResponse { AppMetadata = selected.AppMetadata };
    }

    private async Task<ResolverUIResponse?> SendLegacyResolverUIRequestCore(IEnumerable<IAppMetadata> appMetadata, CancellationToken cancellationToken)
    {
        if (_logger.IsEnabled(LogLevel.Warning))
        {
            _logger.LogWarning("The legacy ResolverUI contract is used, in which the Desktop Agent trusts the app that the ResolverUI names. It is deprecated and is going to be removed.");
        }

        var request = new ResolverUIRequest
        {
            AppMetadata = appMetadata
        };

        return await _messaging.InvokeJsonServiceAsync<ResolverUIRequest, ResolverUIResponse>(
            Fdc3Topic.LegacyResolverUI,
            request,
            _jsonSerializerOptions,
            cancellationToken).ConfigureAwait(false);
    }


    public async Task<ResolverUIIntentResponse?> SendResolverUIIntentRequestAsync(IEnumerable<string> intents, CancellationToken cancellationToken = default)
    {
        //TODO: use the same ResolverUI
        try
        {
            return await SendResolverUIIntentRequestCore(intents, cancellationToken).ConfigureAwait(false);
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
            request, 
            _jsonSerializerOptions,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return response;
    }

}