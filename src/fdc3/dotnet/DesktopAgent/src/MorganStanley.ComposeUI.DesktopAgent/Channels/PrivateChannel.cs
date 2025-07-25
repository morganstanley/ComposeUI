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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MorganStanley.ComposeUI.Messaging.Abstractions;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Channels;

internal class PrivateChannel : Channel
{
    public PrivateChannel(string id, IMessaging messagingService, JsonSerializerOptions jsonSerializerOptions, ILogger<PrivateChannel>? logger, string instanceId)
        : base(id, messagingService, jsonSerializerOptions, (ILogger?) logger ?? NullLogger.Instance, Fdc3Topic.PrivateChannel(id))
    {
        InstanceId = instanceId;
    }

    public string InstanceId { get; }
    protected override string ChannelTypeName => "PrivateChannel";

    public async Task Close(CancellationToken cancellationToken = default)
    {
        var topic = Fdc3Topic.PrivateChannel(Id).Events;
        var payload = "{\"event\": \"disconnected\"}";
        var tcs = new TaskCompletionSource<bool>();

        var subscription = await MessagingService.SubscribeAsync(topic, async buffer =>
        {
            if (buffer == null || !buffer.Contains("disconnected"))
            {
                LogUnexpectedMessage(buffer ?? string.Empty);
            }

            tcs.TrySetResult(true);
            await Task.CompletedTask;
        }, cancellationToken);

        await MessagingService.PublishAsync(topic, payload, cancellationToken: cancellationToken);

        using (cancellationToken.Register(() => tcs.TrySetCanceled()))
        {
            await tcs.Task;
        }

        await subscription.DisposeAsync();
    }
}