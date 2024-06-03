﻿/*
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
using MorganStanley.ComposeUI.Messaging;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure.Internal;

internal class ResolverUiMessageRouterCommunicator : IResolverUiCommunicator
{
    private readonly IMessageRouter _messageRouter;
    private readonly JsonSerializerOptions _jsonMessageSerializerOptions = new()
    {
        Converters = { new AppMetadataJsonConverter() }
    };
    private readonly ILogger<ResolverUiMessageRouterCommunicator> _logger;

    public ResolverUiMessageRouterCommunicator(
        IMessageRouter messageRouter,
        ILogger<ResolverUiMessageRouterCommunicator>? logger = null)
    {
        _messageRouter = messageRouter;
        _logger = logger ?? NullLogger<ResolverUiMessageRouterCommunicator>.Instance;
    }

    public async Task<ResolverUiResponse?> SendResolverUiRequest(IEnumerable<IAppMetadata> appMetadata, CancellationToken cancellationToken = default)
    {
        var request = new ResolverUiRequest
        {
            AppMetadata = appMetadata
        };

        var responseBuffer = await _messageRouter.InvokeAsync(
            Fdc3Topic.ResolverUi,
            MessageBuffer.Factory.CreateJson(request, _jsonMessageSerializerOptions), 
            cancellationToken: cancellationToken);

        if (responseBuffer == null)
        {
            return null;
        }

        var response = responseBuffer.ReadJson<ResolverUiResponse>(_jsonMessageSerializerOptions);

        return response;
    }
}