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

namespace MorganStanley.ComposeUI.Messaging;

/// <summary>
/// Predefined error types.
/// </summary>
public static class MessageRouterErrors
{
    /// <summary>
    /// The requested endpoint or service registration failed because
    /// an endpoint with that name is already registered.
    /// </summary>
    public const string DuplicateEndpoint = "DuplicateEndpoint";

    /// <summary>
    /// A request ID is being used in multiple parallel requests.
    /// </summary>
    public const string DuplicateRequestId = "DuplicateRequestId";

    /// <summary>
    /// The endpoint name has an invalid format.
    /// </summary>
    public const string InvalidEndpoint = "InvalidEndpoint";

    /// <summary>
    /// The topic name has an invalid format.
    /// </summary>
    public const string InvalidTopic = "InvalidTopic";

    /// <summary>
    /// The requested invocation failed because the endpoint is not registered.
    /// </summary>
    public const string UnknownEndpoint = "UnknownEndpoint";

    /// <summary>
    /// The scope of the request contains an unknown client ID.
    /// </summary>
    public const string UnknownClient = "UnknownClient";

    /// <summary>
    /// Client side error that occurs when user code tries to call a method after closing the connection.
    /// </summary>
    public const string ConnectionClosed = "ConnectionClosed";

    /// <summary>
    /// Client side error that occurs when the connection drops unexpectedly.
    /// </summary>
    public const string ConnectionAborted = "ConnectionAborted";

    /// <summary>
    /// Sent by the server if the access token provided by the client is invalid.
    /// </summary>
    public const string InvalidAccessToken = "InvalidAccessToken";

    /// <summary>
    /// Client side error that occurs when connecting to the Message Router server fails.
    /// </summary>
    public const string ConnectionFailed = "ConnectionFailed";
}
