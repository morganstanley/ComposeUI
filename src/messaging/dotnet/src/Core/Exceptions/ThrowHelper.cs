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

#pragma warning disable CS1591
namespace MorganStanley.ComposeUI.Messaging.Exceptions;

/// <summary>
/// Contains helper methods for creating <see cref="MessageRouterException"/> instances.
/// This type is used by infrastructure components and is not intended for use in application code.
/// </summary>
public static class ThrowHelper
{
    public static MessageRouterException DuplicateEndpoint(string endpoint) =>
        new(MessageRouterErrors.DuplicateEndpoint, $"Duplicate endpoint registration: '{endpoint}'");

    public static MessageRouterException DuplicateRequestId() =>
        new(MessageRouterErrors.DuplicateRequestId, "Duplicate request ID");

    public static MessageRouterException InvalidEndpoint(string endpoint) =>
        new(MessageRouterErrors.InvalidEndpoint, $"Invalid endpoint: '{endpoint}'");

    public static MessageRouterException InvalidTopic(string topic) =>
        new(MessageRouterErrors.InvalidTopic, $"Invalid topic: '{topic}'");

    public static MessageRouterException UnknownEndpoint(string endpoint) =>
        new(MessageRouterErrors.UnknownEndpoint, $"Unknown endpoint: {endpoint}");

    public static MessageRouterException ConnectionClosed() =>
        new(MessageRouterErrors.ConnectionClosed, "The connection has been closed");

    public static MessageRouterException ConnectionFailed() =>
        new(MessageRouterErrors.ConnectionFailed, "Connection failed");

    public static MessageRouterException ConnectionFailed(Exception innerException) =>
        new(MessageRouterErrors.ConnectionFailed, "Connection failed.\n\r{innerException.Message}'", innerException);

    public static MessageRouterException ConnectionAborted() =>
        new(MessageRouterErrors.ConnectionAborted, "The connection dropped unexpectedly");

    public static MessageRouterException ConnectionAborted(Exception innerException) =>
        new(MessageRouterErrors.ConnectionAborted, $"The connection dropped unexpectedly.\n\r{innerException.Message}", innerException);

    public static MessageRouterException MessageOrPayloadNull() =>
        new(MessageRouterErrors.ConnectionAborted, "The TopicMessage or its payload is null.");

    public static MessageRouterException ErrorInObserver(Exception innerException) =>
        new(MessageRouterErrors.ConnectionAborted, $"Error in Observer.\n\r{innerException.Message}", innerException);
}
