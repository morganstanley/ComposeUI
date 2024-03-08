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

namespace MorganStanley.ComposeUI.Messaging.Protocol.Messages;

public enum MessageType : int
{
    /// <summary>
    ///     Client wants to connect.
    /// </summary>
    Connect,

    /// <summary>
    ///     Server accepts the connection.
    /// </summary>
    ConnectResponse,

    /// <summary>
    ///     Client subscribes to a topic.
    /// </summary>
    Subscribe,

    /// <summary>
    ///     Server confirms that the client subscribed.
    /// </summary>
    SubscribeResponse,

    /// <summary>
    ///     Client unsubscribes from a topic.
    /// </summary>
    Unsubscribe,

    /// <summary>
    ///     Server confirms that the client unsubscribed.
    /// </summary>
    UnsubscribeResponse,

    /// <summary>
    ///     Client publishes a message to a topic.
    /// </summary>
    Publish,

    /// <summary>
    ///     Server confirms that the message was published to a topic by the client.
    /// </summary>
    PublishResponse,

    /// <summary>
    ///     Server notifies client of a message from a subscribed topic.
    /// </summary>
    Topic,

    /// <summary>
    ///     Client registers an invokable service
    /// </summary>
    RegisterService,

    /// <summary>
    ///     Server confirms that the service is registered (or responds with error).
    /// </summary>
    RegisterServiceResponse,

    /// <summary>
    ///     Client invokes a service, or the server notifies the registered service of an invocation.
    /// </summary>
    Invoke,

    /// <summary>
    ///     Service sends the response of an invocation to server, or server sends the result of a service invocation to the
    ///     caller
    /// </summary>
    InvokeResponse,

    /// <summary>
    ///     Client unregisters itself from a previously registered service name.
    /// </summary>
    UnregisterService,

    /// <summary>
    ///     Server acknowledges that the service is unregistered.
    /// </summary>
    UnregisterServiceResponse,
}