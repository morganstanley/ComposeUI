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
using MorganStanley.ComposeUI.Messaging.Abstractions.Exceptions;

namespace MorganStanley.ComposeUI.Messaging.Abstractions;

/// <summary>
/// Extension methods to ease JSON communication via IMessaging
/// </summary>
public static class MessagingServiceJsonExtensions
{
    /// <summary>
    /// Serializes an object to JSON and calls <see cref="IMessaging.PublishAsync(string, string, CancellationToken)"/> with the resulting string
    /// </summary>
    /// <typeparam name="TPayload">The type of the object to serialize</typeparam>
    /// <param name="messaging"></param>
    /// <param name="topic">The name of the topic to publish to</param>
    /// <param name="payload">The object to serialize and send to the topic</param>
    /// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> to use for serialization</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static ValueTask PublishJsonAsync<TPayload>(this IMessaging messaging, string topic, TPayload payload, JsonSerializerOptions jsonSerializerOptions, CancellationToken cancellationToken = default)
    {
        var stringPayload = JsonSerializer.Serialize(payload, jsonSerializerOptions);
        return messaging.PublishAsync(topic, stringPayload, cancellationToken);
    }

    /// <summary>
    /// Serializes an object to JSON and calls <see cref="IMessaging.InvokeServiceAsync(string, string?, CancellationToken)"/> with the resulting string. Parses the response as JSON and returns it.
    /// </summary>
    /// <typeparam name="TPayload">The type of the request object</typeparam>
    /// <typeparam name="TResult">The type of the result object</typeparam>
    /// <param name="messaging"></param>
    /// <param name="serviceName">The name of the service to invoke</param>
    /// <param name="payload">The request object</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for serializing and deserializing the data</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The parsed response from the service</returns>
    public static async ValueTask<TResult?> InvokeJsonServiceAsync<TPayload, TResult>(
        this IMessaging messaging,
        string serviceName,
        TPayload payload,
        JsonSerializerOptions jsonSerializerOptions,
        CancellationToken cancellationToken = default)
    {
        var stringPayload = JsonSerializer.Serialize(payload, jsonSerializerOptions);
        var response = await messaging.InvokeServiceAsync(serviceName, stringPayload, cancellationToken);

        if (response == null) { return default; }

        return JsonSerializer.Deserialize<TResult>(response, jsonSerializerOptions);
    }

    /// <summary>
    /// Calls <see cref="IMessaging.InvokeServiceAsync(string, string?, CancellationToken)"/> with null request object and parses the resulting JSON string.
    /// </summary>
    /// <typeparam name="TResult">The type the result should be deserialized to</typeparam>
    /// <param name="messaging"></param>
    /// <param name="serviceName">The name of the service to invoke</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for serializing and deserializing the data</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The deserialized response</returns>
    public static async ValueTask<TResult?> InvokeJsonServiceAsync<TResult>(this IMessaging messaging, string serviceName, JsonSerializerOptions jsonSerializerOptions, CancellationToken cancellationToken = default)
    {
        var response = await messaging.InvokeServiceAsync(serviceName, cancellationToken: cancellationToken);

        return response == null ? default : JsonSerializer.Deserialize<TResult>(response, jsonSerializerOptions);
    }

    /// <summary>
    /// Registers a service with <see cref="IMessaging.RegisterServiceAsync(string, ServiceHandler, CancellationToken)". The service handler will parse the request into the request type from JSON and serialize the response object to JSON />
    /// </summary>
    /// <typeparam name="TRequest">The type to deserialize the request to. Must not be string.</typeparam>
    /// <typeparam name="TResult">The type to serialize the response to</typeparam>
    /// <param name="messaging"></param>
    /// <param name="serviceName">The name the service can be invoked by. Must be unique across all messaging instances</param>
    /// <param name="typedHandler">The typed service handler to process the request.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for deserializing requests and serializing responses</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static ValueTask<IAsyncDisposable> RegisterJsonServiceAsync<TRequest, TResult>(
        this IMessaging messaging,
        string serviceName,
        ServiceHandler<TRequest,TResult> typedHandler,
        JsonSerializerOptions jsonSerializerOptions,
        CancellationToken cancellationToken = default)
    {
        return messaging.RegisterServiceAsync(serviceName, CreateJsonServiceHandler(typedHandler, jsonSerializerOptions), cancellationToken);
    }

    /// <summary>
    /// Subscribes to a topic with <see cref="IMessaging.SubscribeAsync(string, TopicMessageHandler, CancellationToken)"/>. The topic handler will deserialize the incoming JSON string to the specified type.
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <param name="messaging"></param>
    /// <param name="topic"></param>
    /// <param name="typedHandler"></param>
    /// <param name="jsonSerializerOptions"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static ValueTask<IAsyncDisposable> SubscribeJsonAsync<TRequest>(
        this IMessaging messaging,
        string topic,
        TopicMessageHandler<TRequest> typedHandler,
        JsonSerializerOptions jsonSerializerOptions,
        CancellationToken cancellationToken = default)
    {
        return messaging.SubscribeAsync(topic, CreateJsonTopicMessageHandler(typedHandler, jsonSerializerOptions), cancellationToken);
    }

    private static TopicMessageHandler CreateJsonTopicMessageHandler<TRequest>(TopicMessageHandler<TRequest> typedHandler, JsonSerializerOptions jsonSerializerOptions)
    {
        if (typeof(TRequest) == typeof(string))
        {
            throw new MessagingException("NonJsonTopic", "The handler provided accepts a string as input. This extension does not support that use-case. Use SubscribeAsync directly to register such a topic.");
        }

        return async (payload) =>
        {
            var request = JsonSerializer.Deserialize<TRequest>(payload, jsonSerializerOptions);
            await typedHandler(request);
        };
    }

    private static ServiceHandler CreateJsonServiceHandler<TRequest, TResult>(ServiceHandler<TRequest, TResult> realHandler, JsonSerializerOptions jsonSerializerOptions)
    {
        if (typeof(TRequest) == typeof(string))
        {
            throw new MessagingException("NonJsonService", "The handler provided accepts a string as input. This extension does not support that use-case. Use CreateServiceHandler directly to register such a service.");
        }

        return async (payload) =>
        {
            var request = payload == null ? default : JsonSerializer.Deserialize<TRequest>(payload, jsonSerializerOptions);
            var result = await realHandler(request);

            if (result is string str)
            {
                return str;
            }

            return JsonSerializer.Serialize(result, jsonSerializerOptions);
        };
    }
}
