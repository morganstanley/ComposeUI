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
using Finos.Fdc3.Context;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Contracts;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Exceptions;
using MorganStanley.ComposeUI.Messaging.Abstractions;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Infrastructure.Internal;

internal class IntentsClient : IIntentsClient
{
    private readonly IMessaging _messaging;
    private readonly string _instanceId;
    private readonly ILogger<IntentsClient> _logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions = SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization;

    public IntentsClient(
        IMessaging messaging,
        string instanceId,
        ILogger<IntentsClient>? logger = null)
    {
        _messaging = messaging;
        _instanceId = instanceId;
        _logger = logger ?? NullLogger<IntentsClient>.Instance;
    }

    public async ValueTask<IAppIntent> FindIntentAsync(string intent, IContext? context = null, string? resultType = null)
    {
        var request = new FindIntentRequest
        {
            Fdc3InstanceId = _instanceId,
            Intent = intent,
            Context = context?.Type,
            ResultType = resultType
        };

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Finding intent. Intent: {Intent}, Context Type: {ContextType}, Result Type: {ResultType}", intent, context?.Type, resultType);
        }

        var response = await _messaging.InvokeJsonServiceAsync<FindIntentRequest, FindIntentResponse>(
            Fdc3Topic.FindIntent,
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

        if (response.AppIntent == null)
        {
            throw ThrowHelper.AppIntentIsNotDefined(intent, context?.Type, resultType);
        }

        return response.AppIntent;
    }
}
