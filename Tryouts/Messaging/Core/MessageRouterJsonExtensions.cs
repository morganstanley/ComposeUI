// Morgan Stanley makes this available to you under the Apache License,
// Version 2.0 (the "License"). You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0.
// 
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership. Unless required by applicable law or agreed
// to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
// or implied. See the License for the specific language governing permissions
// and limitations under the License.

using System.Text.Json;

namespace MorganStanley.ComposeUI.Messaging;

/// <summary>
/// Contains extension methods for working with JSON payloads.
/// </summary>
public static class MessageRouterJsonExtensions
{
    /// <summary>
    /// Publishes a message to a topic, with the payload serialized to JSON.
    /// </summary>
    /// <param name="messageRouter"></param>
    /// <param name="topic"></param>
    /// <param name="payload"></param>
    /// <param name="publishOptions"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="jsonSerializerOptions"></param>
    /// <typeparam name="TPayload"></typeparam>
    /// <returns></returns>
    public static ValueTask PublishJsonAsync<TPayload>(
        this IMessageRouter messageRouter,
        string topic,
        TPayload payload,
        JsonSerializerOptions? jsonSerializerOptions = null,
        PublishOptions publishOptions = default,
        CancellationToken cancellationToken = default)
    {
        return messageRouter.PublishAsync(
            topic,
            MessageBufferJsonExtensions.CreateJson(payload, jsonSerializerOptions),
            publishOptions,
            cancellationToken);
    }
}
