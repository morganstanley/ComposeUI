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
    ///     Client wants to connect
    /// </summary>
    Connect,

    /// <summary>
    ///     Server accepted the connection
    /// </summary>
    ConnectResponse,

    /// <summary>
    ///     Client subscribes to a topic
    /// </summary>
    Subscribe,

    // TODO: SubscribeResponse

    /// <summary>
    ///     Client unsubscribes from a topic
    /// </summary>
    Unsubscribe,

    // TODO: UnsubscribeResponse

    /// <summary>
    ///     Client publishes a message to a topic
    /// </summary>
    Publish,

    // TODO: PublishResponse

    // TODO: Rename to Topic?
    /// <summary>
    ///     Server notifies client of a message from a subscribed topic
    /// </summary>
    Update,

    /// <summary>
    ///     Client registers an invokable service
    /// </summary>
    RegisterService,

    /// <summary>
    ///     Server responds to a RegisterServiceRequest message
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

    // TODO: UnregisterServiceResponse
}