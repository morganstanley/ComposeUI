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
using AppIdentifier = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.AppIdentifier;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Infrastructure.Internal.Protocol;

internal class IntentResolution : IIntentResolution
{
    private readonly string _messageId;
    private readonly IMessaging _messaging;
    private readonly IChannelFactory _channelFactory;
    private readonly JsonSerializerOptions _jsonSerializerOptions = SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization;

    public IntentResolution(
        string messageId,
        IMessaging messaging,
        IChannelFactory channelFactory,
        string intent,
        IAppIdentifier source,
        ILogger<IntentResolution>? logger = null)
    {
        _messageId = messageId;
        _messaging = messaging;
        _channelFactory = channelFactory;
        Intent = intent;
        Source = source;
        _logger = logger ?? NullLogger<IntentResolution>.Instance;
    }

    public IAppIdentifier Source { get; }

    private readonly ILogger<IntentResolution> _logger;

    public string Intent { get; }

    public string? Version { get; }

    public async Task<IIntentResult?> GetResult()
    {
        var request = new GetIntentResultRequest
        {
            MessageId = _messageId,
            TargetAppIdentifier = new AppIdentifier
            {
                AppId = Source.AppId,
                InstanceId = Source.InstanceId
            },
            Intent = Intent,
            Version = Version
        };

        var response = await _messaging.InvokeJsonServiceAsync<GetIntentResultRequest, GetIntentResultResponse>(
            Fdc3Topic.GetIntentResult,
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

        if (!string.IsNullOrEmpty(response.ChannelId)
            && response.ChannelType != null)
        {
            var channel = await _channelFactory.FindChannelAsync(response.ChannelId!, response.ChannelType.Value);
            return channel;
        }
        else if (!string.IsNullOrEmpty(response.Context))
        {
            var context = JsonSerializer.Deserialize<IContext>(response.Context!, _jsonSerializerOptions);
            return context;
        }
        else if (response.VoidResult != null 
            && response.VoidResult.Value)
        {
            _logger.LogDebug("The intent result is void for intent:{Intent} for message: {MessageId}.", Intent, _messageId);

            return null;
        }

        throw ThrowHelper.IntentResolutionFailed(Intent, _messageId, Source);
    }
}
